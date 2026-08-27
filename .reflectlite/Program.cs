using System;
using System.Linq;
using System.Reflection;

var dllPath = @"c:\Users\notco\.nuget\packages\mohutianchi.lite\1.0.2\lib\net10.0\MohuTianchi.Lite.dll";
var asm = Assembly.LoadFrom(dllPath);

Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException ex)
{
    types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
}

static string Pretty(Type t)
{
    if (t.IsGenericType)
    {
        var name = t.Name[..t.Name.IndexOf('`')];
        return $"{name}<{string.Join(",", t.GetGenericArguments().Select(Pretty))}>";
    }
    if (t.IsByRef) return Pretty(t.GetElementType()!) + "&";
    if (t.IsArray) return Pretty(t.GetElementType()!) + "[]";
    return t.Name;
}

string Opt(ParameterInfo p)
{
    var s = p.ParameterType.Name;
    if (p.HasDefaultValue) s += " = " + (p.DefaultValue ?? "null");
    return s;
}

var targets = new[] { "VolumeRecord", "VolumeRegistry", "AccessContext", "AccessControlBase",
    "AccessRule", "RequestContext", "QueryFilter", "ObjectIndexEntry", "WriteOptions",
    "StorageOptions", "LiteStorage", "TenantVolumeIsolation", "StorageVolumePool", "DirectoryLimits" };

foreach (var tn in targets)
{
    var t = types.FirstOrDefault(x => x.Name == tn);
    if (t is null) { Console.WriteLine($"[MISSING] {tn}"); continue; }

    Console.WriteLine($"\n===== {t.FullName} ({(t.IsInterface ? "interface" : t.IsEnum ? "enum" : t.IsClass ? "class" : "")}) =====");

    if (t.IsEnum)
    {
        Console.WriteLine("  " + string.Join(", ", Enum.GetNames(t).Select(n => $"{n}={Convert.ToInt64(Enum.Parse(t, n))}")));
        continue;
    }

    // Properties
    foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var rw = prop.CanWrite ? (prop.CanRead ? "get;set" : "set") : "get";
        Console.WriteLine($"  prop {Pretty(prop.PropertyType)} {prop.Name} {{ {rw} }}");
    }

    // Public methods (incl inherited if non-interface)
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
    {
        var ps = string.Join(", ", m.GetParameters().Select(p => $"{Pretty(p.ParameterType)} {p.Name}"));
        Console.WriteLine($"  {Pretty(m.ReturnType)} {m.Name}({ps})");
    }

    // static factory methods
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
    {
        var ps = string.Join(", ", m.GetParameters().Select(p => $"{Pretty(p.ParameterType)} {p.Name}={Opt(p)}"));
        Console.WriteLine($"  static {Pretty(m.ReturnType)} {m.Name}({ps})");
    }
}

Console.WriteLine("\n=== IObjectStorage full params (with defaults) ===");
var ios = types.First(x => x.Name == "IObjectStorage");
foreach (var m in ios.GetMethods().OrderBy(m => m.Name))
{
    var ps = string.Join(", ", m.GetParameters().Select(p => $"{Pretty(p.ParameterType)} {p.Name}={Opt(p)}"));
    Console.WriteLine($"  {Pretty(m.ReturnType)} {m.Name}({ps})");
}