using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

[ApiController]
[Route($"api/[controller]")]
public class NotFileController : ControllerBase
{
    [HttpGet("hello")]
    public string Hello()
    {
        return "hello";
    }
}