using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ContainerImportService;
using ContainerImportService.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AcrImportFunctionTest
{
    public class ImportFunction
    {
        private readonly IContainerRegistryManagementService _containerRegistryManagementService;
        private readonly ContainerImageImportSource _containerImageImportSource;

        public ImportFunction(IContainerRegistryManagementService containerRegistryManagementService, 
            ContainerImageImportSource containerImageImportSource)
        {
            _containerRegistryManagementService = containerRegistryManagementService ?? throw new ArgumentNullException(nameof(containerRegistryManagementService));
            _containerImageImportSource = containerImageImportSource ?? throw new ArgumentNullException(nameof(containerImageImportSource));
        }

        [FunctionName(nameof(ImportFunction))]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function,  "post", Route = null)] HttpRequest req, ILogger logger)
        {
            var request = await JsonSerializer.DeserializeAsync<ImportRequest>(req.Body);
            if (request == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var importContainerResponse = await _containerRegistryManagementService.ImportContainerImageAsync(new ContainerImageImportRequest
            {
                Mode = ContainerImportMode.Force,
                Source = new ContainerImageImportSource
                {
                    RegistryUri = _containerImageImportSource.RegistryUri,
                    SourceImage = request.SourceImageTag,
                    Credentials = _containerImageImportSource.Credentials
                },
                TargetTags = request.DestinationImageTags
            });

            if (!importContainerResponse.IsSuccessStatusCode)
            {
                logger.LogError($"Error: {importContainerResponse.StatusCode} - {importContainerResponse.ReasonPhrase}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (importContainerResponse.StatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Import completed synchronously");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (importContainerResponse.StatusCode == HttpStatusCode.Accepted)
            {
                var location = importContainerResponse.Headers.Location;

                if (location == null)
                {
                    logger.LogError("No status endpoint in response");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                var finalStatusReponse =
                    await _containerRegistryManagementService.WaitForImportCompletionAsync(location);

                if (!finalStatusReponse.IsSuccessStatusCode)
                {
                    logger.LogError($"Error: {finalStatusReponse.StatusCode} - {finalStatusReponse.ReasonPhrase}");
                }

                logger.LogInformation("Import completed asynchronously");
                return finalStatusReponse;
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);

        }
    }
}

