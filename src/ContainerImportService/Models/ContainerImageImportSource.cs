using System.Text.Json.Serialization;

namespace ContainerImportService.Models;

public class ContainerImageImportSource
{
    /// <summary>
    /// The address of the source registry (e.g. 'mcr.microsoft.com').
    /// </summary>
    [JsonPropertyName("registryUri")] 
    public string? RegistryUri { get; set; }

    /// <summary>
    /// Repository name of the source image. Specify an image by repository ('hello-world'). This will use the 'latest' tag. Specify an image by tag ('hello-world:latest'). Specify an image by sha256-based manifest digest ('hello-world@sha256:abc123').
    /// </summary>
    [JsonPropertyName("sourceImage")]
    public string? SourceImage { get; set; }
    
    /// <summary>
    /// Credentials used when importing from a registry uri.
    /// </summary>
    [JsonPropertyName("credentials")]
    public ContainerRegistryCredentials? Credentials { get; set; }

}