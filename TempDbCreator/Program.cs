using Npgsql;

var connStr = "Host=localhost;Database=postgres;Username=notblog;Password=NotBlog@2026!";
using var conn = new NpgsqlConnection(connStr);
conn.Open();
Console.WriteLine("Connected to PostgreSQL as notblog superuser.");

using var checkCmd = conn.CreateCommand();
checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'BlogFileDev'";
var exists = checkCmd.ExecuteScalar() != null;

if (!exists)
{
    using var createCmd = conn.CreateCommand();
    createCmd.CommandText = "CREATE DATABASE \"BlogFileDev\" OWNER notcomd";
    createCmd.ExecuteNonQuery();
    Console.WriteLine("Database BlogFileDev created successfully.");
}
else
{
    Console.WriteLine("Database BlogFileDev already exists.");
}
