using Microsoft.Data.Sqlite;
using System.Data;

namespace PMTapHoa.Core.Data;

public class DatabaseContext
{
    private readonly string _connectionString;
    public string DatabaseFilePath { get; }

    public DatabaseContext(string databaseFilePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databaseFilePath)!);
        DatabaseFilePath = databaseFilePath;
        _connectionString = $"Data Source={databaseFilePath};";
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    public void InitializeDatabase(string sqlScriptPath)
    {
        if (!File.Exists(sqlScriptPath))
        {
            throw new FileNotFoundException("Không tìm thấy file database.sql.", sqlScriptPath);
        }

        var script = File.ReadAllText(sqlScriptPath);
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = script;
        command.ExecuteNonQuery();

        EnsureSchemaMigrated(connection);
    }

    private static void EnsureSchemaMigrated(IDbConnection connection)
    {
        try
        {
            using var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Products ADD COLUMN ImageUrl TEXT;";
            alterCmd.ExecuteNonQuery();
        }
        catch
        {
            // Ignore if column already exists
        }
    }
}
