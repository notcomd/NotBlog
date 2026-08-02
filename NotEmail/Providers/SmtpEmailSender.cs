﻿using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notcomd.NotEmail.Core;

namespace Notcomd.NotEmail.Providers;

/// <summary>
/// SMTP 邮件发送器
/// 
/// 特性:
/// - 支持 Gmail / Outlook / QQ / 163 等所有 SMTP 服务器
/// - 自动重试 + 线性退避
/// - 附件支持
/// - CC / BCC 支持
/// - 结构化返回 SendResult
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender>? _logger;
    private readonly IOptionsSnapshot<EmailOptions> _options;

    public SmtpEmailSender(IOptionsSnapshot<EmailOptions> options, ILogger<SmtpEmailSender>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task<SendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;

        // FromEmail 缺失时直接返回清晰错误，避免后续 NRE
        // （OAuth2/密码认证的登录账号与 MimeMessage 发件人均依赖 FromEmail）
        if (string.IsNullOrWhiteSpace(opt.FromEmail))
        {
            var error = "[NotEmail] 未配置发件人邮箱（EmailOptions.FromEmail 为空），邮件发送已中止。";
            _logger?.LogError("{Error}", error);
            return SendResult.Fail(error);
        }

        var sw = Stopwatch.StartNew();
        Exception? lastException = null;

        for (var attempt = 0; attempt <= opt.MaxRetryCount; attempt++)
        {
            try
            {
                using var client = new SmtpClient();
                client.Timeout = opt.SendTimeoutMs;

                var socketType = ResolveSocketOptions(opt);
                await client.ConnectAsync(opt.SmtpHost, opt.SmtpPort, socketType, cancellationToken);

                // 认证: OAuth 2.0 优先，否则密码认证
                if (opt.IsOAuth2Configured)
                {
                    var accessToken = await opt.AccessTokenCallback!(cancellationToken);
                    var oauth2 = new SaslMechanismOAuth2(opt.FromEmail, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);
                    _logger?.LogDebug("[NotEmail] SMTP OAuth2 认证成功");
                }
                else
                {
                    if (!opt.IsPasswordConfigured)
                        throw new InvalidOperationException(
                            "[NotEmail] 未配置认证凭据。请设置 Password 或启用 OAuth 2.0（UseOAuth2 + AccessTokenCallback）。");

                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    await client.AuthenticateAsync(opt.FromEmail, opt.Password!, cancellationToken);
                }

                var mimeMessage = BuildMimeMessage(message, opt);
                var response = await client.SendAsync(mimeMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                sw.Stop();

                _logger?.LogInformation(
                    "[NotEmail] 邮件发送成功: To={To}, Subject={Subject}, " +
                    "尝试次数={Attempt}, 耗时={ElapsedMs}ms",
                    message.To, message.Subject, attempt + 1, sw.ElapsedMilliseconds);

                return new SendResult
                {
                    Success = true,
                    MessageId = response,
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < opt.MaxRetryCount)
                {
                    var delayMs = (attempt + 1) * opt.RetryIntervalMs;
                    _logger?.LogWarning(ex,
                        "[NotEmail] 邮件发送失败，将在 {DelayMs}ms 后重试（第 {Attempt}/{MaxRetry} 次）",
                        delayMs, attempt + 1, opt.MaxRetryCount);
                    await Task.Delay(delayMs, cancellationToken);
                }
                else
                {
                    _logger?.LogError(ex,
                        "[NotEmail] 邮件发送最终失败（已重试 {MaxRetry} 次）: To={To}, Subject={Subject}",
                        opt.MaxRetryCount, message.To, message.Subject);
                }
            }
        }

        sw.Stop();
        return new SendResult
        {
            Success = false,
            ErrorMessage = lastException?.Message ?? "未知错误",
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    public async Task<IReadOnlyList<SendResult>> SendBatchAsync(
        IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<SendResult>();
        foreach (var message in messages)
        {
            results.Add(await SendAsync(message, cancellationToken));
        }

        return results;
    }

    /// <summary>
    /// 按端口自动选择安全模式（支持 UseSsl 配置覆盖）：
    ///   - UseSsl=false → 不加密（SecureSocketOptions.None）
    ///   - UseSsl=true（默认）→ 按端口：465 隐式 SSL（SslOnConnect），587/25 显式 TLS（StartTls）
    /// </summary>
    private static SecureSocketOptions ResolveSocketOptions(EmailOptions opt)
    {
        if (!opt.UseSsl)
            return SecureSocketOptions.None;

        return opt.SmtpPort switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            25 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.StartTls
        };
    }

    private MimeMessage BuildMimeMessage(EmailMessage message, EmailOptions opt)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(message.FromName ?? opt.FromName ?? opt.FromEmail, opt.FromEmail));
        mime.To.Add(MailboxAddress.Parse(message.To));

        foreach (var cc in message.Cc)
            mime.Cc.Add(MailboxAddress.Parse(cc));
        foreach (var bcc in message.Bcc)
            mime.Bcc.Add(MailboxAddress.Parse(bcc));

        mime.Subject = message.Subject;

        var builder = new BodyBuilder();

        if (!string.IsNullOrEmpty(message.HtmlBody))
            builder.HtmlBody = message.HtmlBody;
        if (!string.IsNullOrEmpty(message.PlainTextBody))
            builder.TextBody = message.PlainTextBody;

        foreach (var att in message.Attachments)
        {
            builder.Attachments.Add(att.FileName, att.Data,
                ContentType.Parse(att.MimeType ?? "application/octet-stream"));
        }

        mime.Body = builder.ToMessageBody();

        mime.Priority = message.Priority switch
        {
            EmailPriority.High => MessagePriority.Urgent,
            EmailPriority.Low => MessagePriority.NonUrgent,
            _ => MessagePriority.Normal
        };

        return mime;
    }
}