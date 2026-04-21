using System.Text.Json.Serialization;

namespace db_switch
{
    public class DbItem
    {
        [JsonPropertyName("name")] public string DbName { get; set; } = string.Empty;
        [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
        [JsonPropertyName("post-restore-script")] public string PostRestoreScript { get; set; } = string.Empty;
    }
}
