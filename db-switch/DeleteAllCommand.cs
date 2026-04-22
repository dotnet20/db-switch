using Microsoft.Data.SqlClient;
using Spectre.Console.Cli;
using System.Text.Json;

namespace db_switch
{
    public class DeleteAllCommand : Command
    {
        protected override int Execute(CommandContext context, CancellationToken cancellationToken)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found.");
                return 1;
            }

            var config = JsonSerializer.Deserialize<AppConfigRoot>(File.ReadAllText(configPath));
            using var conn = new SqlConnection(config!.AppConfig.ConnectionString);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Conn Error: {ex.Message}"); return 1;
            }

            Program.ExecuteSql(conn, @"USE master; DECLARE @sql NVARCHAR(MAX) = N''; 
                SELECT @sql += N'ALTER DATABASE [' + name + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + name + ']; ' 
                FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb'); EXEC sp_executesql @sql;", "Dropping all user databases");

            Console.WriteLine("All user databases deleted.");
            return 0;
        }
    }
}