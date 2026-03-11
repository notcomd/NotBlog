using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace FileDev.Web.API.APIs;

public static class NotMapNotFileApis
{
    public static RouteGroupBuilder NotFileApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var route = routeGroupBuilder.MapGroup("notfile").WithHttpLogging(HttpLoggingFields.RequestBody,
            60, 60);


        route.MapGet("/hello", TestHello).WithOpenApi(operation =>
        {
            operation.Description = "hello world";
            operation.Summary = "hello world";
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "hello",
                In = ParameterLocation.Query,
                Description = "hello world",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Description = "hello world",
                    Example = new OpenApiString("hello world")
                }
            });
            operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "hello world",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "application/json",
                            new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "hello world",
                                    Example = new OpenApiString("hello world")
                                }
                            }
                        }
                    }
                }
            );
            return operation;
        });
        return routeGroupBuilder;
    }

    private static Task<string?> TestHello(this HttpContext httpContext)
    {
        return Task.FromResult(httpContext.Request.Path.Value);
    }
}