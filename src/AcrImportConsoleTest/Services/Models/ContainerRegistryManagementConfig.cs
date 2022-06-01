namespace AcrImportConsoleTest.Services.Models;

public class ContainerRegistryManagementConfig
{
    /// <summary>
    /// The Microsoft Azure subscription ID of destination Azure Container Registry.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// The name of the resource group to which the destination container registry belongs.
    /// </summary>
    public string? ResourceGroupName { get; set; }

    /// <summary>
    /// The name of the destination container registry.
    /// </summary>
    public string? ContainerRegistryName { get; set; }
}