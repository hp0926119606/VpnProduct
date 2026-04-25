using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using VpnProduct.Application.Interfaces;
using VpnProduct.Application.Models.VpnPeers;
using VpnProduct.Domain.Entities;

namespace VpnProduct.Web.Controllers
{
    [ApiController]
    [Route("api/vpnpeers")]
    public class VpnPeersController : ControllerBase
    {
        private readonly IVpnPeerService _vpnPeerService;

        public VpnPeersController(IVpnPeerService vpnPeerService)
        {
            _vpnPeerService = vpnPeerService;
        }

        [HttpGet("node/{vpnNodeId:guid}")]
        public async Task<IActionResult> GetByNodeId([FromRoute] Guid vpnNodeId, CancellationToken cancellationToken)
        {
            var result = await _vpnPeerService.GetByNodeIdAsync(vpnNodeId, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVpnPeerRequest request, CancellationToken cancellationToken)
        {
            var peer = new VpnPeer
            {
                VpnNodeId = request.VpnNodeId,
                Name = request.Name,
                PublicKey = request.PublicKey,
                AssignedIp = request.AssignedIp,
                IsActive = request.IsActive
            };

            var created = await _vpnPeerService.CreateAsync(peer, cancellationToken);
            return Ok(created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateVpnPeerRequest request, CancellationToken cancellationToken)
        {
            var peer = new VpnPeer
            {
                Name = request.Name,
                PublicKey = request.PublicKey,
                AssignedIp = request.AssignedIp,
                IsActive = request.IsActive
            };

            var updated = await _vpnPeerService.UpdateAsync(id, peer, cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await _vpnPeerService.DeleteAsync(id, cancellationToken);
            return Ok(new { success = true });
        }
    }
}
