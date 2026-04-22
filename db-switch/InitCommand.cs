using Spectre.Console.Cli;
using System.Text.Json;

namespace db_switch
{
    public class InitCommand : Command
    {
        protected override int Execute(CommandContext context, CancellationToken cancellationToken)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
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
            return 0;
        }
    }
}