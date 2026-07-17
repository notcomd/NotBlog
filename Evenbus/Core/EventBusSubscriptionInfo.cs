
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Notcomd.Evenbus.Core;

public class EventBusSubscriptionInfo
{
    public Dictionary<string, Type> EventTypes{ get; } = new();

    public JsonSerializerOptions JsonSerializerOptions { get; } = new();

    internal static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        TypeInfoResolver=JsonSerializer.IsReflectionEnabledByDefault ? CreateDefaultTypeInfoResolver() : JsonTypeInfoResolver.Combine()
    };

    private static IJsonTypeInfoResolver CreateDefaultTypeInfoResolver()
    {
        return new DefaultJsonTypeInfoResolver();
    }
    
}
        
    

