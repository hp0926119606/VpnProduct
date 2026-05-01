using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnProduct.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixVpnNodeRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyncJobs_VpnNodes_VpnNodeId1",
                table: "SyncJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_VpnPeers_VpnNodes_VpnNodeId1",
                table: "VpnPeers");

            migrationBuilder.DropIndex(
                name: "IX_VpnPeers_VpnNodeId1",
                table: "VpnPeers");

            migrationBuilder.DropIndex(
                name: "IX_SyncJobs_VpnNodeId1",
                table: "SyncJobs");

            migrationBuilder.DropColumn(
                name: "VpnNodeId1",
                table: "VpnPeers");

            migrationBuilder.DropColumn(
                name: "VpnNodeId1",
                table: "SyncJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VpnNodeId1",
                table: "VpnPeers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VpnNodeId1",
                table: "SyncJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VpnPeers_VpnNodeId1",
                table: "VpnPeers",
                column: "VpnNodeId1");

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_VpnNodeId1",
                table: "SyncJobs",
                column: "VpnNodeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SyncJobs_VpnNodes_VpnNodeId1",
                table: "SyncJobs",
                column: "VpnNodeId1",
                principalTable: "VpnNodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VpnPeers_VpnNodes_VpnNodeId1",
                table: "VpnPeers",
                column: "VpnNodeId1",
                principalTable: "VpnNodes",
                principalColumn: "Id");
        }
    }
}
