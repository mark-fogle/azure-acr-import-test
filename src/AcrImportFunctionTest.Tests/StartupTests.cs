using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AcrImportFunctionTest.Tests
{
    public class StartupTests
    {
        private readonly IHost _host;

        public StartupTests()
        {
            _host = new HostBuilder()
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new [] { 
                            new KeyValuePair<string, string>("ContainerRegistryManagementConfig:SubscriptionId", "Test"),
                            new KeyValuePair<string, string>("ContainerRegistryManagementConfig:ResourceGroupName", "Test"),
                            new KeyValuePair<string, string>("ContainerRegistryManagementConfig:ContainerRegistryName", "Test"),
                            new KeyValuePair<string, string>("ContainerImageImportSource:RegistryUri", "Test"),
                            new KeyValuePair<string, string>("ContainerImageImportSource:Credentials:UserName", "Test"),
                            new KeyValuePair<string, string>("ContainerImageImportSource:Credentials:Password", "Test")
                        }
                    );
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ImportFunction>();
                })
                .ConfigureWebJobs((context, builder) =>
                {
                    new Startup().Configure(new WebJobsBuilderContext
                    {
                        Configuration = context.Configuration
                    }, builder);
                })
                .Build();
        }

        [Fact]
        public void ServicesShouldBeAbleToResolveImportFunction()
        {
            _host.Services.GetRequiredService<ImportFunction>();
        }
    }
}