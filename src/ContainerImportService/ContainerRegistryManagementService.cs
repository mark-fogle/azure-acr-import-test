using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using ContainerImportService.Models;
using Microsoft.Extensions.Logging;

namespace ContainerImportService;

public class ContainerRegistryManagementService : IContainerRegistryManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ContainerRegistryManagementConfig _config;
    private readonly TokenCredential _credentials;
    private readonly ILogger<ContainerRegistryManagementService> _logger;
    private AccessToken? _accessToken;

    public ContainerRegistryManagementService(HttpClient httpClient, 
        ContainerRegistryManagementConfig config,
        TokenCredential credentials,
        ILogger<ContainerRegistryManagementService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient.BaseAddress = new Uri("https://management.azure.com");
    }

    private async Task<AccessToken> GetTokenAsync()
    {
        if (_accessToken == null || _accessToken.Value.ExpiresOn - DateTimeOffset.UtcNow < TimeSpan.FromMinutes(15)) 
        {

            _accessToken =
            await _credentials.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.core.windows.net/.default"}), CancellationToken.None);
        }
        return _accessToken.Value;
    }

    /// <summary>
    /// Calls Azure Management API for container registry to import a container image from another registry
    /// https://docs.microsoft.com/en-us/rest/api/containerregistry/registries/import-image#importsource
    /// </summary>
    /// <param name="imageImportRequest">Container image import request to send to endpoint</param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> ImportContainerImageAsync(ContainerImageImportRequest imageImportRequest)
    {
        var accessToken = await GetTokenAsync();

        var requestUri = $@"/subscriptions/{_config.SubscriptionId}/resourceGroups/{_config.ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{_config.ContainerRegistryName}/importImage?api-version=2019-05-01";

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        var content = new StringContent(JsonSerializer.Serialize(imageImportRequest));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = content;

        return await _httpClient.SendAsync(requestMessage);
        
    }

    /// <summary>
    /// Polls import status endpoint for completion. Status changes from Accepted to OK.
    /// </summary>
    /// <param name="statusEndpoint"></param>
    /// <param name="pollingDelaySeconds"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> WaitForImportCompletionAsync(Uri statusEndpoint, int pollingDelaySeconds = 1)
    {
        var accessToken = await GetTokenAsync();

        var statusResponse = new HttpResponseMessage(HttpStatusCode.Accepted);

        while (statusResponse.StatusCode == HttpStatusCode.Accepted)
        {

            var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusEndpoint);
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

            statusResponse = await _httpClient.SendAsync(statusRequest);
            var responseContent = await statusResponse.Content.ReadAsStringAsync();
            
            if (!statusResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Error: {statusResponse.StatusCode} - {statusResponse.ReasonPhrase}: {responseContent}");
                return statusResponse;
            }

            _logger.LogInformation($"{statusResponse.StatusCode} - {statusResponse.ReasonPhrase}: {responseContent}");

            await Task.Delay(TimeSpan.FromSeconds(pollingDelaySeconds));
        }

        return statusResponse;
    }
}