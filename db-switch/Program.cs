using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

public class AppConfigRoot { [JsonPropertyName("app_config")] public AppConfig AppConfig { get; set; } = default!; }
public class AppConfig
{
    [JsonPropertyName("connection-string")] public string ConnectionString { get; set; } = string.Empty;
    [JsonPropertyName("environments")] public List<DbEnv> Environments { get; set; } = new();
}
public class DbEnv
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("backup")] public List<DbItem> Backups { get; set; } = new();
}
public class DbItem
{
    [JsonPropertyName("name")] public string DbName { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("post-restore-script")] public string PostRestoreScript { get; set; } = string.Empty;
}

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "-env")
        {
            Console.WriteLine("Usage: db-switch -env <env-name>");
            return;
        }

        string targetEnv = args[1];
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Error: Config file missing at {configPath}");
            return;
        }

        var config = JsonSerializer.Deserialize<AppConfigRoot>(File.ReadAllText(configPath));
        var env = config?.AppConfig.Environments.FirstOrDefault(e => e.Name.Equals(targetEnv, StringComparison.OrdinalIgnoreCase));

        if (env == null)
        {
            Console.WriteLine($"Environment '{targetEnv}' not found.");
            return;
        }

        Console.WriteLine($"Switching to: {env.Name.ToUpper()}");

        using var conn = new SqlConnection(config!.AppConfig.ConnectionString);
        conn.Open();

        foreach (var db in env.Backups)
        {
            Console.WriteLine($"[1/3] Deleting database: {db.DbName}");
            string dropSql = $@"
                USE master;
                IF EXISTS (SELECT name FROM sys.databases WHERE name = '{db.DbName}')
                BEGIN
                    ALTER DATABASE [{db.DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{db.DbName}];
                END";
            ExecuteSql(conn, dropSql);

            Console.WriteLine($"[2/3] Restoring from: {db.Path}");
            string restoreSql = $@"
                RESTORE DATABASE [{db.DbName}] 
                FROM DISK = '{db.Path}' 
                WITH REPLACE, RECOVERY;";
            ExecuteSql(conn, restoreSql);

            if (!string.IsNullOrEmpty(db.PostRestoreScript) && File.Exists(db.PostRestoreScript))
            {
                Console.WriteLine($"[3/3] Running script: {db.PostRestoreScript}");
                string postSql = $"USE [{db.DbName}];\n" + File.ReadAllText(db.PostRestoreScript);
                ExecuteSql(conn, postSql);
            }
            else
            {
                Console.WriteLine("[3/3] No post-restore script to run");
            }
        }

        Console.WriteLine("\n DONE");
    }

    static void ExecuteSql(SqlConnection conn, string sql)
    {
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 0;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Environment.Exit(1);
        }
    }
}