using Npgsql;

var connStr = "Host=localhost;Database=postgres;Username=notblog;Password=NotBlog@2026!";
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
