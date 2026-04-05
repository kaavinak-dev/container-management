using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engines.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    ProjectType = table.Column<string>(type: "text", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StorageUrl = table.Column<string>(type: "text", nullable: false),
                    BucketName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "executable_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactBucket = table.Column<string>(type: "text", nullable: false),
                    ArtifactName = table.Column<string>(type: "text", nullable: false),
                    StorageUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VirusScanResult = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContainerId = table.Column<string>(type: "text", nullable: true),
                    DockerNetworkId = table.Column<string>(type: "text", nullable: true),
                    DockerNetworkName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_executable_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_executable_projects_projects_SourceProjectId",
                        column: x => x.SourceProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "js_project_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectVersion = table.Column<string>(type: "text", nullable: true),
                    DependencyCount = table.Column<int>(type: "integer", nullable: false),
                    VulnerabilityCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalVulnerabilities = table.Column<int>(type: "integer", nullable: false),
                    HighVulnerabilities = table.Column<int>(type: "integer", nullable: false),
                    NodeVersion = table.Column<string>(type: "text", nullable: true),
                    NpmVersion = table.Column<string>(type: "text", nullable: true),
                    PackageSize = table.Column<long>(type: "bigint", nullable: false),
                    UnpackedSize = table.Column<long>(type: "bigint", nullable: false),
                    FileCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_js_project_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_js_project_metadata_executable_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "executable_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutableProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    IssuesJson = table.Column<string>(type: "text", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_assessments_executable_projects_ExecutableProjectId",
                        column: x => x.ExecutableProjectId,
                        principalTable: "executable_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "js_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JsMetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_js_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_js_dependencies_js_project_metadata_JsMetadataId",
                        column: x => x.JsMetadataId,
                        principalTable: "js_project_metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "js_vulnerabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JsMetadataId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_js_vulnerabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_js_vulnerabilities_js_project_metadata_JsMetadataId",
                        column: x => x.JsMetadataId,
                        principalTable: "js_project_metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_executable_projects_SourceProjectId",
                table: "executable_projects",
                column: "SourceProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_js_dependencies_JsMetadataId",
                table: "js_dependencies",
                column: "JsMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_js_project_metadata_ProjectId",
                table: "js_project_metadata",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_js_vulnerabilities_JsMetadataId",
                table: "js_vulnerabilities",
                column: "JsMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessments_ExecutableProjectId",
                table: "risk_assessments",
                column: "ExecutableProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "js_dependencies");

            migrationBuilder.DropTable(
                name: "js_vulnerabilities");

            migrationBuilder.DropTable(
                name: "risk_assessments");

            migrationBuilder.DropTable(
                name: "js_project_metadata");

            migrationBuilder.DropTable(
                name: "executable_projects");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
