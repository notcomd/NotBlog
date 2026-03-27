using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DomainInfrastructure;

/// <summary>
/// 反射辅助工具
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// 根据产品名称获取程序集
    /// </summary>
    public static IEnumerable<Assembly> GetAssembliesByProductName(string productName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            var productAttr = asm.GetCustomAttribute<AssemblyProductAttribute>();
            if (productAttr != null && productAttr.Product == productName)
                yield return asm;
        }
    }

    /// <summary>
    /// 获取所有引用的程序集（排除系统程序集）
    /// </summary>
    public static IEnumerable<Assembly> GetAllReferencedAssemblies(bool skipSystemAssemblies = true)
    {
        var rootAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var resultAssemblies = new HashSet<Assembly>(new AssemblyNameComparer());
        var loadedAssemblies = new HashSet<string>();
        var assembliesToCheck = new Queue<Assembly>();

        assembliesToCheck.Enqueue(rootAssembly);

        if (!skipSystemAssemblies || !IsSystemAssembly(rootAssembly))
        {
            if (IsValidAssembly(rootAssembly))
                resultAssemblies.Add(rootAssembly);
        }

        while (assembliesToCheck.Count > 0)
        {
            var assembly = assembliesToCheck.Dequeue();
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!loadedAssemblies.Contains(reference.FullName))
                {
                    var referencedAssembly = Assembly.Load(reference);
                    if (skipSystemAssemblies && IsSystemAssembly(referencedAssembly))
                        continue;

                    assembliesToCheck.Enqueue(referencedAssembly);
                    loadedAssemblies.Add(reference.FullName);

                    if (IsValidAssembly(referencedAssembly))
                        resultAssemblies.Add(referencedAssembly);
                }
            }
        }

        // 扫描基目录中的 DLL 文件
        var dllFiles = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll",
            new EnumerationOptions { RecurseSubdirectories = true });

        foreach (var dllPath in dllFiles)
        {
            if (!IsManagedAssembly(dllPath)) continue;

            try
            {
                var asmName = AssemblyName.GetAssemblyName(dllPath);
                if (resultAssemblies.Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), asmName)))
                    continue;

                if (skipSystemAssemblies && IsSystemAssemblyByPath(dllPath))
                    continue;

                var asm = TryLoadAssembly(dllPath);
                if (asm == null) continue;
                if (!IsValidAssembly(asm)) continue;

                resultAssemblies.Add(asm);
            }
            catch
            {
                // 忽略无法加载的程序集
            }
        }

        return resultAssemblies;
    }

    private static bool IsSystemAssembly(Assembly asm)
    {
        var companyAttr = asm.GetCustomAttribute<AssemblyCompanyAttribute>();
        return companyAttr?.Company?.Contains("Microsoft") == true;
    }

    private static bool IsSystemAssemblyByPath(string path)
    {
        try
        {
            var name = AssemblyName.GetAssemblyName(path);
            var asm = Assembly.Load(name);
            return IsSystemAssembly(asm);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsManagedAssembly(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            using var peReader = new PEReader(fs);
            return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
        }
        catch
        {
            return false;
        }
    }

    private static Assembly? TryLoadAssembly(string path)
    {
        try
        {
            var name = AssemblyName.GetAssemblyName(path);
            return Assembly.Load(name);
        }
        catch
        {
            try
            {
                return Assembly.LoadFile(path);
            }
            catch
            {
                return null;
            }
        }
    }

    private static bool IsValidAssembly(Assembly asm)
    {
        try
        {
            asm.GetTypes();
            _ = asm.DefinedTypes.Count();
            return true;
        }
        catch (ReflectionTypeLoadException)
        {
            return false;
        }
    }

    private class AssemblyNameComparer : EqualityComparer<Assembly>
    {
        public override bool Equals(Assembly? x, Assembly? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return AssemblyName.ReferenceMatchesDefinition(x.GetName(), y.GetName());
        }

        public override int GetHashCode(Assembly obj) => obj.GetName().FullName.GetHashCode();
    }
}