using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AcrImportConsoleTest.Services.Models;

namespace AcrImportConsoleTest.Services;

public class ContainerRegistryManagementService : IContainerRegistryManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ContainerRegistryManagementConfig _config;
    private readonly ILogger<ContainerRegistryManagementService> _logger;

    public ContainerRegistryManagementService(HttpClient httpClient, ContainerRegistryManagementConfig config, ILogger<ContainerRegistryManagementService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient.BaseAddress = new Uri($"https://management.azure.com");
    }

    /// <summary>
    /// Calls Azure Management API for container registry to import a container image from another registry
    /// https://docs.microsoft.com/en-us/rest/api/containerregistry/registries/import-image#importsource
    /// </summary>
    /// <param name="imageImportRequest">Container image import request to send to endpoint</param>
    /// <param name="token">Access token to access API</param>
    /// <returns></returns>
    public Task<HttpResponseMessage> ImportContainerImageAsync(ContainerImageImportRequest imageImportRequest, string token)
    {
        var requestUri = $@"/subscriptions/{_config.SubscriptionId}/resourceGroups/{_config.ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{_config.ContainerRegistryName}/importImage?api-version=2019-05-01";

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(JsonSerializer.Serialize(imageImportRequest));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = content;

        return _httpClient.SendAsync(requestMessage);
        
    }

    /// <summary>
    /// Polls import status endpoint for completion. Status changes from Accepted to OK.
    /// </summary>
    /// <param name="statusEndpoint"></param>
    /// <param name="token"></param>
    /// <param name="pollingDelaySeconds"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> WaitForImportCompletionAsync(Uri statusEndpoint, string token, int pollingDelaySeconds = 1)
    {
        var statusResponse = new HttpResponseMessage(HttpStatusCode.Accepted);

        while (statusResponse.StatusCode == HttpStatusCode.Accepted)
        {

            var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusEndpoint);
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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