using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DomainInfrastructure;

/// <summary>
/// 反射辅助工具
/// 提供程序集扫描、类型发现等反射相关操作。
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// 根据 AssemblyProductAttribute 产品名称获取匹配的程序集
    /// </summary>
    /// <param name="productName">产品名称</param>
    /// <returns>匹配的程序集集合</returns>
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
    /// 获取所有引用的程序集（可选排除系统程序集）。
    /// 通过递归遍历引用关系 + 扫描 BaseDirectory 中的 DLL 文件实现。
    /// </summary>
    /// <param name="skipSystemAssemblies">是否跳过系统程序集，默认 true</param>
    /// <returns>发现的程序集集合</returns>
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

        // 扫描基目录中的 DLL 文件，发现可能因延迟加载而未在引用链中的程序集
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

    /// <summary>
    /// 判断程序集是否为系统程序集。
    /// 检查 AssemblyCompanyAttribute 和 AssemblyProductAttribute 中是否包含 "Microsoft"。
    /// 此为启发式判断，不保证 100% 精确。
    /// </summary>
    private static bool IsSystemAssembly(Assembly asm)
    {
        var companyAttr = asm.GetCustomAttribute<AssemblyCompanyAttribute>();
        if (companyAttr?.Company?.Contains("Microsoft") == true)
            return true;

        var productAttr = asm.GetCustomAttribute<AssemblyProductAttribute>();
        return productAttr?.Product?.Contains("Microsoft") == true;
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

    /// <summary>
    /// 判断文件是否为托管（.NET）程序集
    /// </summary>
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

    /// <summary>
    /// 尝试加载程序集，先尝试按名称加载，失败则按文件路径加载
    /// </summary>
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

    /// <summary>
    /// 验证程序集是否可正常加载类型（无 ReflectionTypeLoadException）
    /// </summary>
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

    /// <summary>
    /// 基于程序集名称的比较器，用于 HashSet 去重
    /// </summary>
    private class AssemblyNameComparer : EqualityComparer<Assembly>
    {
        public override bool Equals(Assembly? x, Assembly? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return AssemblyName.ReferenceMatchesDefinition(x.GetName(), y.GetName());
        }

        public override int GetHashCode(Assembly obj)
        {
            var name = obj?.GetName();
            return name?.FullName?.GetHashCode() ?? 0;
        }
    }
}