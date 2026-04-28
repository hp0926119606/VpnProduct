using VpnProduct.Application.Models.VpnPeers;

namespace VpnProduct.Application.Interfaces
{
    public interface IVpnPeerService
    {
        Task<CreateVpnPeerResponse> CreateAsync(CreateVpnPeerRequest request, CancellationToken cancellationToken = default);
    }
}
