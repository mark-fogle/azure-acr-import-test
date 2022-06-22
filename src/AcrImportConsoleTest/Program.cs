using System.Net;
using AcrImportConsoleTest.Services;
using AcrImportConsoleTest.Services.Models;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Initialize generic host
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(context.Configuration.GetSection(nameof(ContainerRegistryManagementConfig)).Get<ContainerRegistryManagementConfig>());
        services.AddSingleton(context.Configuration.GetSection(nameof(ContainerImageImportSource)).Get<ContainerImageImportSource>());
        services.AddHttpClient<IContainerRegistryManagementService, ContainerRegistryManagementService>();
    })
    .Build();

// Get token for Azure Management API using default credentials
// https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
var tokenResponse = await GetAzureAccessTokenAsync(new[] { "https://management.core.windows.net/.default" });

var containerRegistryManagementService = host.Services.GetRequiredService<IContainerRegistryManagementService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

var containerImportSource = host.Services.GetRequiredService<ContainerImageImportSource>();
containerImportSource.SourceImage = "myimage:latest";

var destinationImages = new[] { "myimage:latest" };

var importContainerResponse = await containerRegistryManagementService.ImportContainerImageAsync(new ContainerImageImportRequest
{
    Mode = ContainerImportMode.Force,
    Source = containerImportSource,
    TargetTags = destinationImages
}, tokenResponse.Token);

if (!importContainerResponse.IsSuccessStatusCode)
{
    logger.LogError($"Error: {importContainerResponse.StatusCode} - {importContainerResponse.ReasonPhrase}");
    return;
}

if (importContainerResponse.StatusCode == HttpStatusCode.OK)
{
    logger.LogInformation("Import completed synchronously");
    return;
}

if (importContainerResponse.StatusCode == HttpStatusCode.Accepted)
{
    var location = importContainerResponse.Headers.Location;

    if (location == null)
    {
        logger.LogError("No status endpoint in response");
        return;
    }

    var finalStatusReponse =
        await containerRegistryManagementService.WaitForImportCompletionAsync(location, tokenResponse.Token);

    if (!finalStatusReponse.IsSuccessStatusCode)
    {
        logger.LogError($"Error: {finalStatusReponse.StatusCode} - {finalStatusReponse.ReasonPhrase}");
    }

    logger.LogInformation("Import completed asynchronously");
}

static async Task<AccessToken> GetAzureAccessTokenAsync(string[] scopes)
{
    var credentials = new Azure.Identity.DefaultAzureCredential();

    var accessToken =
        await credentials.GetTokenAsync(
            new TokenRequestContext(scopes));
    return accessToken;
}
