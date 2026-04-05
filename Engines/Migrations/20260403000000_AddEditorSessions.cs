using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Engines.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Engines.DataBaseStorageEngines.ProjectDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260403000000_AddEditorSessions")]
    public partial class AddEditorSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "editor_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    ContainerName = table.Column<string>(type: "text", nullable: false),
                    WorkspaceVolume = table.Column<string>(type: "text", nullable: false),
                    NpmCacheVolume = table.Column<string>(type: "text", nullable: false),
                    ContainerIp = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastActive = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_editor_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_editor_sessions_ProjectId",
                table: "editor_sessions",
                column: "ProjectId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "editor_sessions");
        }
    }
}
