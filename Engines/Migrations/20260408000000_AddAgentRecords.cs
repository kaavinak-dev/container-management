using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Engines.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Engines.DataBaseStorageEngines.ProjectDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260408000000_AddAgentRecords")]
    public partial class AddAgentRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to editor_sessions (AgentId and ContainerId were added
            // to EditorSessionRecord but never included in the original migration)
            migrationBuilder.AddColumn<string>(
                name: "AgentId",
                table: "editor_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "editor_sessions",
                type: "text",
                nullable: true);

            // Create agent_records table for relay agent registry
            migrationBuilder.CreateTable(
                name: "agent_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    DockerHost = table.Column<string>(type: "text", nullable: false),
                    Hostname = table.Column<string>(type: "text", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_records_AgentId",
                table: "agent_records",
                column: "AgentId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "agent_records");

            migrationBuilder.DropColumn(name: "AgentId", table: "editor_sessions");
            migrationBuilder.DropColumn(name: "ContainerId", table: "editor_sessions");
        }
    }
}
