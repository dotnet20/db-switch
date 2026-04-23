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

            var jsonString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppConfigRoot>(jsonString);

            if (config?.AppConfig?.Environments == null)
            {
                Console.WriteLine("No defined environments in the configuration.");
                return 1;
            }

            var databasesToDelete = config.AppConfig.Environments
                .SelectMany(e => e.Backups)
                .Select(b => b.DbName)
                .Distinct()
                .ToList();

            if (!databasesToDelete.Any())
            {
                Console.WriteLine("No databases to delete.");
                return 0;
            }

            using var conn = new SqlConnection(config.AppConfig.ConnectionString);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Conn Error: {ex.Message}");
                return 1;
            }

            foreach (var dbName in databasesToDelete)
            {
                string sql = $@"
                USE master; 
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName.Replace("'", "''")}')
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; 
                    DROP DATABASE [{dbName}]; 
                END";

                Program.ExecuteSql(conn, sql, $"Dropping database {dbName}");
            }

            Console.WriteLine("All user databases deleted.");
            return 0;
        }
    }
}