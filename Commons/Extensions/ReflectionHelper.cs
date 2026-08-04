using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Commons.Extensions;

/// <summary>
/// 反射辅助工具
/// 提供程序集扫描、类型发现等反射相关操作。
/// </summary>
public static class ReflectionHelper
{
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
