using ContainerImportService.Models;

namespace ContainerImportService;

public interface IContainerRegistryManagementService
{
    Task<HttpResponseMessage> ImportContainerImageAsync(ContainerImageImportRequest imageImportRequest);
    Task<HttpResponseMessage> WaitForImportCompletionAsync(Uri statusEndpoint, int pollingDelaySeconds = 1);
}