using Npgsql;

// 凭据外置（S-01）：口令从环境变量读取，不再硬编码。
var connStr = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connStr))
{
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException(
            "未配置数据库口令：请设置环境变量 POSTGRES_PASSWORD，或直接提供完整连接串 POSTGRES_CONNECTION_STRING。");

    connStr = $"Host=localhost;Database=postgres;Username=notblog;Password={password}";
}

using var conn = new NpgsqlConnection(connStr);
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "CREATE DATABASE \"BlogFileDev\"";
try {
    cmd.ExecuteNonQuery();
    Console.WriteLine("Database BlogFileDev created successfully!");
} catch (Exception ex) {
    Console.WriteLine("Error: " + ex.Message);
}
