using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnProduct.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientConfigToVpnPeer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientConfig",
                table: "VpnPeers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientConfig",
                table: "VpnPeers");
        }
    }
}
