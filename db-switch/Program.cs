using db_switch;
using Microsoft.Data.SqlClient;
using System.Text.Json;

string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

if (args.Length > 0 && args[0] == "--init")
{
    var defaultConfig = new AppConfigRoot
    {
        AppConfig = new AppConfig
        {
            ConnectionString = "Server=localhost;Database=master;Trusted_Connection=True;Encrypt=False;",
            Environments = new List<DbEnv> {
                new DbEnv {
                    Name = "dev",
                    Backups = new List<DbItem>
                    {
                        new DbItem
                        {
                            DbName = "DbName",
                            Path = "C:\\Windows\\Users\\file.bak"
                        }
                    }
                }
            }
        }
    };
    File.WriteAllText(configPath, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Created default config at: {configPath}");
    return;
}

if (args.Length < 2 || args[0] != "-env")
{
    return;
}

if (!File.Exists(configPath))
{
    return;
}

string targetEnv = args[1];
var config = JsonSerializer.Deserialize<AppConfigRoot>(File.ReadAllText(configPath));
var env = config?.AppConfig.Environments.FirstOrDefault(e => e.Name.Equals(targetEnv, StringComparison.OrdinalIgnoreCase));

if (env == null)
{
    return;
}

Console.WriteLine($"Switching to: {env.Name.ToUpper()}");

using var conn = new SqlConnection(config!.AppConfig.ConnectionString);
try
{
    conn.Open();
}
catch (Exception ex)
{
    Console.WriteLine($"Conn Error: {ex.Message}");
    return;
}

foreach (var db in env.Backups)
{
    Console.WriteLine($"[1/3] Deleting: {db.DbName}");
    ExecuteSql(conn, $@"USE master; IF EXISTS(SELECT * FROM sys.databases WHERE name='{db.DbName}') 
        BEGIN ALTER DATABASE [{db.DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db.DbName}]; END");

    Console.WriteLine($"[2/3] Restoring: {db.Path}");
    ExecuteSql(conn, $"RESTORE DATABASE [{db.DbName}] FROM DISK='{db.Path}' WITH REPLACE, RECOVERY;");

    if (!string.IsNullOrEmpty(db.PostRestoreScript) && File.Exists(db.PostRestoreScript))
    {
        Console.WriteLine($"[3/3] Script: {db.PostRestoreScript}");
        ExecuteSql(conn, $"USE [{db.DbName}];\n" + File.ReadAllText(db.PostRestoreScript));
    }
}

Console.WriteLine("\n DONE");

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