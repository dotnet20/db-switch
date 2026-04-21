using System.Text.Json.Serialization;

namespace db_switch
{
    public class AppConfigRoot
    {
        [JsonPropertyName("app_config")] public AppConfig AppConfig { get; set; } = default!;
    }
}