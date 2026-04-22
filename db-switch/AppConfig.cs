using System.Text.Json.Serialization;

namespace db_switch
{
    public class AppConfig
    {
        [JsonPropertyName("connection-string")]
        public string ConnectionString { get; set; } = string.Empty;
        [JsonPropertyName("environments")]
        public List<DbEnv> Environments { get; set; } = new();
    }
}