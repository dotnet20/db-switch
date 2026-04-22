using System.Text.Json.Serialization;

namespace db_switch
{
    public class DbEnv
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("backup")]
        public List<DbItem> Backups { get; set; } = new();
    }
}