using System.Text.Json;
using System.Text.Json.Serialization;

namespace widows_dev_interview2.Models
{
    public class MediaItem
    {
        [JsonPropertyName("date")]
        public DateOnly Date { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = "";

        [JsonPropertyName("media_type")]
        public string MediaType { get; set; } = "";

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("hdurl")]
        public string? HighResolutionUrl { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("copyright")]
        public string? Copyright { get; set; }

        [JsonPropertyName("credit")]
        public string? Credit { get; set; }

        [JsonIgnore]
        public string OriginalJson { get; set; } = "";

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalFields { get; set; } = new();
    }
}
