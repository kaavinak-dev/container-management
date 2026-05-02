using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Engines.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Engines.DataBaseStorageEngines.ProjectDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260412000000_AddProjectFabric")]
    public partial class AddProjectFabric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create project_networks table
            migrationBuilder.CreateTable(
                name: "project_networks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    NetworkDockerId = table.Column<string>(type: "text", nullable: false),
                    NetworkName = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_networks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_networks_ProjectId",
                table: "project_networks",
                column: "ProjectId",
                unique: true);

            // Create project_resources table
            migrationBuilder.CreateTable(
                name: "project_resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    NetworkRecordId = table.Column<int>(type: "integer", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    ContainerDockerId = table.Column<string>(type: "text", nullable: false),
                    ContainerName = table.Column<string>(type: "text", nullable: false),
                    Alias = table.Column<string>(type: "text", nullable: false),
                    ImageTag = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EnvironmentJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHealthCheck = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_resources_project_networks_NetworkRecordId",
                        column: x => x.NetworkRecordId,
                        principalTable: "project_networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_resources_ProjectId",
                table: "project_resources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_resources_NetworkRecordId",
                table: "project_resources",
                column: "NetworkRecordId");

            // Add NetworkRecordId FK column to editor_sessions
            migrationBuilder.AddColumn<int>(
                name: "NetworkRecordId",
                table: "editor_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_editor_sessions_NetworkRecordId",
                table: "editor_sessions",
                column: "NetworkRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_editor_sessions_project_networks_NetworkRecordId",
                table: "editor_sessions",
                column: "NetworkRecordId",
                principalTable: "project_networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_editor_sessions_project_networks_NetworkRecordId",
                table: "editor_sessions");

            migrationBuilder.DropIndex(
                name: "IX_editor_sessions_NetworkRecordId",
                table: "editor_sessions");

            migrationBuilder.DropColumn(
                name: "NetworkRecordId",
                table: "editor_sessions");

            migrationBuilder.DropTable(name: "project_resources");
            migrationBuilder.DropTable(name: "project_networks");
        }
    }
}
