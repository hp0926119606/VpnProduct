using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnProduct.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VpnNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InterfaceName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServerAddressCidr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ListenPort = table.Column<int>(type: "integer", nullable: false),
                    AgentToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConfigVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastHeartbeatAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VpnNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    ConfigVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncJobs_VpnNodes_VpnNodeId",
                        column: x => x.VpnNodeId,
                        principalTable: "VpnNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VpnPeers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VpnNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    AssignedIp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VpnPeers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VpnPeers_VpnNodes_VpnNodeId",
                        column: x => x.VpnNodeId,
                        principalTable: "VpnNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_VpnNodeId",
                table: "SyncJobs",
                column: "VpnNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_VpnPeers_VpnNodeId",
                table: "VpnPeers",
                column: "VpnNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncJobs");

            migrationBuilder.DropTable(
                name: "VpnPeers");

            migrationBuilder.DropTable(
                name: "VpnNodes");
        }
    }
}
