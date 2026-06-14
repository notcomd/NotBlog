using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Notcomd.NotEmail;

/// <summary>
/// IMAP 邮件接收器
/// 
/// 特性:
/// - 获取收件箱邮件列表
/// - 按 UID 搜索和获取单封邮件
/// - 主题关键词搜索
/// - 软删除 + 永久清除
/// - 已读标记
/// </summary>
public class ImapEmailReceiver : IEmailReceiver, IAsyncDisposable
{
    private readonly ILogger<ImapEmailReceiver>? _logger;
    private readonly IOptionsSnapshot<EmailOptions> _options;
    private ImapClient? _client;
    private bool _connected;

    public ImapEmailReceiver(IOptionsSnapshot<EmailOptions> options, ILogger<ImapEmailReceiver>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            if (_client.IsConnected)
                await _client.DisconnectAsync(true);
            _client.Dispose();
            _connected = false;
        }
    }

    public async Task<IReadOnlyList<ReceivedEmail>> GetInboxAsync(int limit = 50, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var inbox = _client!.Inbox;
        var count = Math.Min(limit == 0 ? int.MaxValue : limit, inbox.Count);

        if (count == 0) return Array.Empty<ReceivedEmail>();

        // 获取最近的 N 条邮件
        var start = Math.Max(0, inbox.Count - count);
        var end = inbox.Count - 1;

        var uids = await inbox.FetchAsync(start, end,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags);
        var results = new List<ReceivedEmail>(uids.Count);

        foreach (var summary in uids.OrderByDescending(s => s.Index))
        {
            results.Add(MapSummary(summary));
        }

        return results;
    }

    public async Task<ReceivedEmail?> GetByUidAsync(uint uid, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var uids = new[] { new UniqueId(uid) };
        var summaries = await _client!.Inbox.FetchAsync(uids,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope |
            MessageSummaryItems.Flags | MessageSummaryItems.BodyStructure);

        var first = summaries.FirstOrDefault();
        if (first == null) return null;

        var email = MapSummary(first);

        // 获取邮件正文
        try
        {
            var message = await _client.Inbox.GetMessageAsync(first.UniqueId, ct);
            email = FillBody(email, message);
        }
        catch
        {
            // 如果获取正文失败，返回基本信息
        }

        return email;
    }

    public async Task<IReadOnlyList<ReceivedEmail>> SearchAsync(string keyword, int limit = 50,
        CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var query = SearchQuery.SubjectContains(keyword).Or(SearchQuery.BodyContains(keyword));
        var uids = await _client!.Inbox.SearchAsync(query, ct);

        var fetchUids = uids.Take(limit == 0 ? int.MaxValue : limit).ToList();
        if (fetchUids.Count == 0) return Array.Empty<ReceivedEmail>();

        var summaries = await _client.Inbox.FetchAsync(fetchUids,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags);

        return summaries.Select(MapSummary).ToList();
    }

    public async Task<bool> DeleteAsync(uint uid, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var uniqueId = new UniqueId(uid);
        await _client!.Inbox.AddFlagsAsync(uniqueId, MessageFlags.Deleted, true, ct);

        _logger?.LogInformation("[NotEmail] 邮件已标记删除: UID={Uid}", uid);
        return true;
    }

    public async Task<int> DeleteBatchAsync(IEnumerable<uint> uids, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);

        var uniqueIds = uids.Select(id => new UniqueId(id)).ToList();
        await _client!.Inbox.AddFlagsAsync(uniqueIds, MessageFlags.Deleted, true, ct);

        _logger?.LogInformation("[NotEmail] 批量标记删除: Count={Count}", uniqueIds.Count);
        return uniqueIds.Count;
    }

    public async Task ExpungeAsync(CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);
        await _client!.Inbox.ExpungeAsync(ct);
        _logger?.LogInformation("[NotEmail] 已永久清除标记删除的邮件");
    }

    public async Task MarkAsReadAsync(uint uid, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);
        await _client!.Inbox.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connected && _client?.IsConnected == true) return;

        var opt = _options.Value;
        _client?.Dispose();
        _client = new ImapClient();
        _client.Timeout = opt.ReceiveTimeoutMs;

        await _client.ConnectAsync(opt.ImapHost, opt.ImapPort, SecureSocketOptions.SslOnConnect, ct);

        // 认证: OAuth 2.0 优先，否则密码认证
        if (opt.IsOAuth2Configured)
        {
            var accessToken = await opt.AccessTokenCallback!(ct);
            var oauth2 = new SaslMechanismOAuth2(opt.FromEmail, accessToken);
            await _client.AuthenticateAsync(oauth2, ct);
            _logger?.LogDebug("[NotEmail] IMAP OAuth2 认证成功");
        }
        else
        {
            if (!opt.IsPasswordConfigured)
                throw new InvalidOperationException(
                    "[NotEmail] 未配置认证凭据。请设置 Password 或启用 OAuth 2.0（UseOAuth2 + AccessTokenCallback）。");

            _client.AuthenticationMechanisms.Remove("XOAUTH2");
            await _client.AuthenticateAsync(opt.FromEmail, opt.Password!, ct);
        }

        // 打开收件箱（读写模式）
        await _client.Inbox.OpenAsync(FolderAccess.ReadWrite, ct);
        _connected = true;

        _logger?.LogDebug("[NotEmail] IMAP 已连接到 {Host}", opt.ImapHost);
    }

    private static ReceivedEmail MapSummary(IMessageSummary summary)
    {
        IReadOnlyList<string> to = Array.Empty<string>();
        if (summary.Envelope?.To?.Mailboxes is { } mailboxes)
            to = mailboxes.Select(m => m.Address).ToList();
        else if (summary.Envelope?.To is { } toList)
            to = toList.ToString()!.Split(',').Select(s => s.Trim()).ToList();

        return new ReceivedEmail
        {
            Uid = summary.UniqueId.Id,
            Subject = summary.Envelope?.Subject?.ToString() ?? "(无主题)",
            From = summary.Envelope?.From?.Mailboxes?.FirstOrDefault()?.Address ?? "unknown",
            To = to,
            ReceivedAt = summary.Envelope?.Date ?? DateTimeOffset.MinValue,
            IsRead = summary.Flags?.HasFlag(MessageFlags.Seen) ?? false
        };
    }

    private static ReceivedEmail FillBody(ReceivedEmail email, MimeMessage message)
    {
        return new ReceivedEmail
        {
            Uid = email.Uid,
            Subject = email.Subject,
            From = email.From,
            To = email.To,
            ReceivedAt = email.ReceivedAt,
            IsRead = email.IsRead,
            HtmlBody = message.HtmlBody,
            PlainTextBody = message.TextBody,
            Attachments = message.Attachments
                .Where(a => a.IsAttachment)
                .Select(a =>
                {
                    using var ms = new MemoryStream();
                    (a as MimePart)?.Content.DecodeTo(ms);
                    return new EmailAttachment(
                        a.ContentDisposition?.FileName ?? a.ContentType.Name ?? "attachment",
                        ms.ToArray(),
                        a.ContentType.MimeType);
                }).ToList()
        };
    }
}