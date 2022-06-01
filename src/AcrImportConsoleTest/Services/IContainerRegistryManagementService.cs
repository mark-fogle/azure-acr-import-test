using AcrImportConsoleTest.Services.Models;

namespace AcrImportConsoleTest.Services;

public interface IContainerRegistryManagementService
{
    Task<HttpResponseMessage> ImportContainerImageAsync(ContainerImageImportRequest imageImportRequest, string token);
    Task<HttpResponseMessage> WaitForImportCompletionAsync(Uri statusEndpoint, string token, int pollingDelaySeconds = 1);
}