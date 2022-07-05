using System.Text.Json.Serialization;

namespace ContainerImportService.Models;

public class ContainerRegistryCredentials
{
    /// <summary>
    /// The username to authenticate with the source registry.
    /// </summary>
    [JsonPropertyName("username")]
    public string? UserName { get; set; }

    /// <summary>
    /// The password used to authenticate with the source registry.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}