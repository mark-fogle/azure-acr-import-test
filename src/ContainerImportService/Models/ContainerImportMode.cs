namespace ContainerImportService.Models;

/// <summary>
/// When Force, any existing target tags will be overwritten. When NoForce, any existing target tags will fail the operation before any copying begins.
/// </summary>
public enum ContainerImportMode
{
    Force,
    NoForce
}