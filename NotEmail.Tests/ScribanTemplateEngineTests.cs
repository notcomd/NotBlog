using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class ScribanTemplateEngineTests
{
    private readonly ScribanTemplateEngine _engine = new();

    [Fact]
    public async Task Render_SimpleVariable_ShouldReplace()
    {
        var template = "<h1>Hello {{ Name }}!</h1>";
        var model = new { Name = "World" };

        var result = await _engine.RenderAsync(template, model);

        Assert.Equal("<h1>Hello World!</h1>", result);
    }

    [Fact]
    public async Task Render_ConditionalBlock_ShouldWork()
    {
        var template = "{{ if IsPremium }}VIP{{ else }}Basic{{ end }}";

        var vipResult = await _engine.RenderAsync(template, new { IsPremium = true });
        Assert.Equal("VIP", vipResult);

        var basicResult = await _engine.RenderAsync(template, new { IsPremium = false });
        Assert.Equal("Basic", basicResult);
    }

    [Fact]
    public async Task Render_LoopBlock_ShouldIterate()
    {
        var template = "{{ for item in Items }}{{ item }},{{ end }}";
        var model = new { Items = new[] { "A", "B", "C" } };

        var result = await _engine.RenderAsync(template, model);

        Assert.Equal("A,B,C,", result);
    }

    [Fact]
    public async Task Render_ComplexModel_ShouldWork()
    {
        var template = @"
<h1>Order #{{ OrderId }}</h1>
<p>Customer: {{ CustomerName }}</p>
<p>Total: ${{ Total | math.format ""F2"" }}</p>
<ul>
{{ for item in Items }}
  <li>{{ item.Product }} x{{ item.Quantity }}</li>
{{ end }}
</ul>";

        var model = new
        {
            OrderId = "ORD-001",
            CustomerName = "张三",
            Total = 99.50,
            Items = new[]
            {
                new { Product = "笔记本", Quantity = 1 },
                new { Product = "鼠标", Quantity = 2 }
            }
        };

        var result = await _engine.RenderAsync(template, model);

        Assert.Contains("Order #ORD-001", result);
        Assert.Contains("Customer: 张三", result);
        Assert.Contains("$99.50", result);
        Assert.Contains("笔记本 x1", result);
        Assert.Contains("鼠标 x2", result);
    }

    [Fact]
    public async Task Render_InvalidTemplate_ShouldThrow()
    {
        var template = "{{ if Name }}missing end";

        await Assert.ThrowsAsync<TemplateRenderException>(
            () => _engine.RenderAsync(template, new { Name = "test" }));
    }

    [Fact]
    public async Task Render_EmptyTemplate_ShouldReturnEmpty()
    {
        var result = await _engine.RenderAsync("", new { });

        Assert.Equal("", result);
    }
}
