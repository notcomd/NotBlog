namespace Commons.EntityFramework;

/// <summary>
/// 数据库连接字符串解析器（凭据外置 S-01）：
/// 若连接串已含 Password 则原样返回；否则从环境变量 <paramref name="envPasswordName"/> 读取口令并追加；
/// 连接串缺失或口令缺失时抛出清晰错误。
/// </summary>
public static class DbConnectionStringResolver
{
    public static string Resolve(string? connectionString, string envPasswordName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "未配置数据库连接字符串（配置节 DbContextConnect / DbContextOption:DbContextConnect）。");

        if (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var password = Environment.GetEnvironmentVariable(envPasswordName);
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                $"数据库连接字符串未包含 Password，且环境变量 {envPasswordName} 未设置，无法启动。");

        return connectionString.TrimEnd(';') + $";Password={password};";
    }
}
