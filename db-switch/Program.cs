using Microsoft.Data.SqlClient;
using Spectre.Console.Cli;
using System.Collections.Generic;

namespace db_switch
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.SetApplicationName("db-switch");
                config.ConfigureConsole(Spectre.Console.AnsiConsole.Console);

                config.AddCommand<InitCommand>("init").WithDescription("Creates a configuration file").WithExample(new[] { "--init"});
                config.AddCommand<DeleteAllCommand>("delall").WithDescription("Remove all databases from config file")
                .WithExample(new[] { "--delall" });
                config.AddCommand<SwitchCommand>("env")
                .WithExample(new[] { "--env", "dev" })
                .WithExample(new[] { "--env", "prod", "--db", "DbName" })
                .WithExample(new[] { "--env", "test", "--skip-post" });
            });

            var sanitizedArgs = args.Select(arg =>
                (arg == "--init") ? "init" :
                (arg == "--delall") ? "delall" :
                (arg == "--env") ? "env" : arg).ToArray();

            if (sanitizedArgs.Length > 0 && sanitizedArgs[0].StartsWith("-") && !new[] { "env", "init", "delall" }.Contains(sanitizedArgs[0]))
            {
                sanitizedArgs = sanitizedArgs.Prepend("env").ToArray();
            }

            return app.Run(sanitizedArgs);
        }

        public static void ExecuteSql(SqlConnection conn, string sql, string? context = null)
        {
            try
            {
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(context))
                {
                    Console.WriteLine($"Error during: {context}");
                }
                Console.WriteLine($"SQL Error: {ex.Message}");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}