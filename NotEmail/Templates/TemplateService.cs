using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Notcomd.NotEmail;

/// <summary>
/// 模板注册与渲染服务
/// </summary>
public class TemplateService
{
    private readonly ITemplateEngine _engine;
    private readonly ILogger<TemplateService>? _logger;
    private readonly ConcurrentDictionary<string, EmailTemplate> _templates = new();

    public TemplateService(ITemplateEngine engine, ILogger<TemplateService>? logger = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger;
    }

    /// <summary>
    /// 获取所有模板名称
    /// </summary>
    public IReadOnlyCollection<string> TemplateNames => _templates.Keys.ToList().AsReadOnly();

    /// <summary>
    /// 注册一个模板
    /// </summary>
    public TemplateService Register(EmailTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (!_templates.TryAdd(template.Name, template))
            throw new InvalidOperationException($"模板 '{template.Name}' 已存在");
        _logger?.LogDebug("[NotEmail] 模板已注册: {TemplateName}", template.Name);
        return this;
    }

    /// <summary>
    /// 批量注册模板
    /// </summary>
    public TemplateService RegisterAll(IEnumerable<EmailTemplate> templates)
    {
        foreach (var t in templates) Register(t);
        return this;
    }

    /// <summary>
    /// 获取已注册的模板
    /// </summary>
    public EmailTemplate? Get(string name) =>
        _templates.TryGetValue(name, out var template) ? template : null;

    /// <summary>
    /// 渲染模板并返回 EmailMessage
    /// </summary>
    public async Task<EmailMessage> RenderAsync(string templateName, object model, string to, string? subject = null)
    {
        var template = Get(templateName)
                       ?? throw new KeyNotFoundException(
                           $"模板 '{templateName}' 未注册。可用模板: {string.Join(", ", _templates.Keys)}");

        var htmlBody = await _engine.RenderAsync(template.HtmlTemplate, model);
        var renderedSubject = subject ?? await _engine.RenderAsync(template.Subject, model);

        var message = new EmailMessage(to, renderedSubject, htmlBody);

        if (template.PlainTextTemplate != null)
        {
            message.PlainTextBody = await _engine.RenderAsync(template.PlainTextTemplate, model);
        }

        return message;
    }
}