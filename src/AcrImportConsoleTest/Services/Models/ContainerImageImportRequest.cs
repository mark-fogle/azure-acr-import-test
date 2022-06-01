using System.Text.Json.Serialization;

namespace AcrImportConsoleTest.Services.Models
{
    public class ContainerImageImportRequest
    {
        /// <summary>
        /// The source of the image.
        /// </summary>
        [JsonPropertyName("source")]
        public ContainerImageImportSource? Source { get; set; }
        
        /// <summary>
        /// When Force, any existing target tags will be overwritten. When NoForce, any existing target tags will fail the operation before any copying begins.
        /// </summary>
        [JsonPropertyName("mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContainerImportMode Mode { get; set; }

        /// <summary>
        /// List of strings of the form repo[:tag]. When tag is omitted the source will be used (or 'latest' if source tag is also omitted).
        /// </summary>
        [JsonPropertyName("targetTags")] 
        public string[]? TargetTags { get; set; }
    }
}
