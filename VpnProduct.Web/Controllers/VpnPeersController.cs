using Microsoft.AspNetCore.Mvc;
using VpnProduct.Application.Interfaces;
using VpnProduct.Application.Models.VpnPeers;

namespace VpnProduct.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VpnPeersController : ControllerBase
    {
        private readonly IVpnPeerService _vpnPeerService;

        public VpnPeersController(IVpnPeerService vpnPeerService)
        {
            _vpnPeerService = vpnPeerService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVpnPeerRequest request, CancellationToken cancellationToken)
        {
            var result = await _vpnPeerService.CreateAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
