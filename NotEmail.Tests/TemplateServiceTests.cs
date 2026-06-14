using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class TemplateServiceTests
{
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        _service = new TemplateService(new ScribanTemplateEngine());
    }

    [Fact]
    public void Register_ShouldAddTemplate()
    {
        var template = new EmailTemplate("Welcome", "Hello {{ model.Name }}", "<h1>Hello {{ model.Name }}</h1>");

        _service.Register(template);

        Assert.Contains("Welcome", _service.TemplateNames);
        Assert.NotNull(_service.Get("Welcome"));
    }

    [Fact]
    public void Register_DuplicateName_ShouldThrow()
    {
        _service.Register(new EmailTemplate("Test", "Subject", "Body"));

        Assert.Throws<InvalidOperationException>(
            () => _service.Register(new EmailTemplate("Test", "Subject2", "Body2")));
    }

    [Fact]
    public void RegisterAll_ShouldAddAllTemplates()
    {
        var templates = new[]
        {
            new EmailTemplate("A", "Sub A", "Body A"),
            new EmailTemplate("B", "Sub B", "Body B"),
            new EmailTemplate("C", "Sub C", "Body C")
        };

        _service.RegisterAll(templates);

        Assert.Equal(3, _service.TemplateNames.Count);
    }

    [Fact]
    public async Task RenderAsync_ShouldProduceCorrectEmailMessage()
    {
        _service.Register(new EmailTemplate(
            "Welcome",
            "Welcome {{ Name }}!",
            "<h1>Hello {{ Name }}</h1>",
            "Hello {{ Name }}"));

        var message = await _service.RenderAsync("Welcome", new { Name = "张三" }, "user@test.com");

        Assert.Equal("Welcome 张三!", message.Subject);
        Assert.Equal("<h1>Hello 张三</h1>", message.HtmlBody);
        Assert.Equal("Hello 张三", message.PlainTextBody);
        Assert.Equal("user@test.com", message.To);
    }

    [Fact]
    public async Task RenderAsync_NonExistentTemplate_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.RenderAsync("NonExistent", new { }, "to@test.com"));
    }

    [Fact]
    public async Task RenderAsync_CustomSubject_ShouldOverride()
    {
        _service.Register(new EmailTemplate("Order", "Default", "<p>{{ Id }}</p>"));

        var message = await _service.RenderAsync("Order", new { Id = 123 }, "to@test.com", subject: "Custom Subject");

        Assert.Equal("Custom Subject", message.Subject);
    }

    [Fact]
    public void Get_NonExistent_ShouldReturnNull()
    {
        Assert.Null(_service.Get("NotExist"));
    }
}
