using Microsoft.Data.SqlClient;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace db_switch
{
    public class SwitchCommand : Command<SwitchSettings>
    {
        protected override int Execute(CommandContext context, SwitchSettings settings, CancellationToken cancellationToken)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found.");
                return 1;
            }

            var config = JsonSerializer.Deserialize<AppConfigRoot>(File.ReadAllText(configPath));
            var env = config?.AppConfig.Environments.FirstOrDefault(e => e.Name.Equals(settings.EnvironmentName, StringComparison.OrdinalIgnoreCase));

            if (env == null)
            {
                Console.WriteLine($"Environment '{settings.EnvironmentName}' not found in config.");
                return 1;
            }

            Console.WriteLine($"Switching to: {env.Name.ToUpper()}");

            List<DbItem> backupsToRestore = env.Backups;
            if (!string.IsNullOrEmpty(settings.DatabaseName))
            {
                var dbItem = env.Backups.FirstOrDefault(b => b.DbName.Equals(settings.DatabaseName, StringComparison.OrdinalIgnoreCase));
                if (dbItem == null)
                {
                    Console.WriteLine($"Database '{settings.DatabaseName}' not found in environment '{env.Name}'.");
                    return 1;
                }
                backupsToRestore = new List<DbItem> { dbItem };
            }

            using var conn = new SqlConnection(config!.AppConfig.ConnectionString);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Conn Error: {ex.Message}");
                return 1;
            }

            int total = backupsToRestore.Count;
            for (int idx = 0; idx < total; idx++)
            {
                var db = backupsToRestore[idx];
                Console.WriteLine($"[{idx + 1}/{total}] Deleting: {db.DbName}");
                Program.ExecuteSql(conn, $@"USE master; IF EXISTS(SELECT * FROM sys.databases WHERE name='{db.DbName}') 
                    BEGIN ALTER DATABASE [{db.DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db.DbName}]; END");

                Console.WriteLine($"[{idx + 1}/{total}] Restoring: {db.Path}");
                Program.ExecuteSql(conn, $"RESTORE DATABASE [{db.DbName}] FROM DISK='{db.Path}' WITH REPLACE, RECOVERY;");

                if (!settings.SkipPost)
                {
                    if (!string.IsNullOrEmpty(db.PostRestoreScript) && File.Exists(db.PostRestoreScript))
                    {
                        Console.WriteLine($"[{idx + 1}/{total}] Script: {db.PostRestoreScript}");
                        Program.ExecuteSql(conn, $"USE [{db.DbName}];\n" + File.ReadAllText(db.PostRestoreScript));
                    }
                }
                else if (!string.IsNullOrEmpty(db.PostRestoreScript))
                {
                    Console.WriteLine($"[{idx + 1}/{total}] Skipped post-restore script for {db.DbName} (switch enabled).");
                }
            }

            Console.WriteLine("\n DONE");
            return 0;
        }
    }
}
