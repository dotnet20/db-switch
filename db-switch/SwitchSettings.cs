using Microsoft.Data.SqlClient;
using Spectre.Console.Cli;
using System.Text.Json;

namespace db_switch
{
    public class SwitchSettings : CommandSettings
    {
        [CommandArgument(0, "<ENVIRONMENT>")]
        public string EnvironmentName { get; set; } = string.Empty;

        [CommandOption("-d|--db <DATABASE>")]
        public string? DatabaseName { get; set; }

        [CommandOption("--skip-post|--no-post")]
        public bool SkipPost { get; set; }
    }
}