namespace VpnProduct.Application.Interfaces;

public interface IWireGuardManager
{
    Task ApplyConfigurationAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}