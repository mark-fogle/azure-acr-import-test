// File:  Startup.cs
// Author: Mark Fogle
// Company: ActiGraph
// Created: 2022-07-01
// Purpose:

using AcrImportFunctionTest;
using Azure.Core;
using Azure.Identity;
using ContainerImportService;
using ContainerImportService.Models;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace AcrImportFunctionTest;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;
        builder.Services.AddLogging();
        builder.Services.AddSingleton(configuration.GetSection(nameof(ContainerRegistryManagementConfig)).Get<ContainerRegistryManagementConfig>());
        builder.Services.AddSingleton(configuration.GetSection(nameof(ContainerImageImportSource)).Get<ContainerImageImportSource>());
        builder.Services.AddHttpClient<IContainerRegistryManagementService, ContainerRegistryManagementService>();
        builder.Services.AddScoped<TokenCredential>(_ => new DefaultAzureCredential());
    }
}