using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create SiteAuditRuns table
            migrationBuilder.CreateTable(
                name: "SiteAuditRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalChecks = table.Column<int>(type: "integer", nullable: false),
                    PassedChecks = table.Column<int>(type: "integer", nullable: false),
                    FailedChecks = table.Column<int>(type: "integer", nullable: false),
                    WarningChecks = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IsCrawl = table.Column<bool>(type: "boolean", nullable: false),
                    TotalPages = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulPages = table.Column<int>(type: "integer", nullable: false),
                    FailedPages = table.Column<int>(type: "integer", nullable: false),
                    BrokenLinks = table.Column<int>(type: "integer", nullable: false),
                    MaxDepth = table.Column<int>(type: "integer", nullable: true),
                    MaxPages = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAuditRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteAuditRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create SiteAuditDetails table
            migrationBuilder.CreateTable(
                name: "SiteAuditDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteAuditRunId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    CheckName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CheckType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    AutoFixAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: true),
                    Depth = table.Column<int>(type: "integer", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    MetaDescription = table.Column<string>(type: "text", nullable: true),
                    WordCount = table.Column<int>(type: "integer", nullable: true),
                    HeadingsJson = table.Column<string>(type: "text", nullable: true),
                    OutboundLinksJson = table.Column<string>(type: "text", nullable: true),
                    ImagesJson = table.Column<string>(type: "text", nullable: true),
                    HasSchema = table.Column<bool>(type: "boolean", nullable: true),
                    HasLlmsTxt = table.Column<bool>(type: "boolean", nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    MainContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteAuditDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteAuditDetails_SiteAuditRuns_SiteAuditRunId",
                        column: x => x.SiteAuditRunId,
                        principalTable: "SiteAuditRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for SiteAuditRuns
            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditRuns_ProjectId",
                table: "SiteAuditRuns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditRuns_Status",
                table: "SiteAuditRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditRuns_CreatedAt",
                table: "SiteAuditRuns",
                column: "CreatedAt");

            // Create indexes for SiteAuditDetails
            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditDetails_SiteAuditRunId",
                table: "SiteAuditDetails",
                column: "SiteAuditRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditDetails_CheckType",
                table: "SiteAuditDetails",
                column: "CheckType");

            migrationBuilder.CreateIndex(
                name: "IX_SiteAuditDetails_Status",
                table: "SiteAuditDetails",
                column: "Status");

            // Create indexes for CommunityOpportunities (if not already exist)
            migrationBuilder.CreateIndex(
                name: "IX_CommunityOpportunities_Priority",
                table: "CommunityOpportunities",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityOpportunities_EngagementScore",
                table: "CommunityOpportunities",
                column: "EngagementScore");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityOpportunities_IsArchived",
                table: "CommunityOpportunities",
                column: "IsArchived");

            // Create indexes for UserTasks (if not already exist)
            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_Priority",
                table: "UserTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_IsCompleted",
                table: "UserTasks",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserTasks_DueDate",
                table: "UserTasks",
                column: "DueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop SiteAuditDetails table
            migrationBuilder.DropTable(
                name: "SiteAuditDetails");

            // Drop SiteAuditRuns table
            migrationBuilder.DropTable(
                name: "SiteAuditRuns");

            // Drop indexes for CommunityOpportunities
            migrationBuilder.DropIndex(
                name: "IX_CommunityOpportunities_Priority",
                table: "CommunityOpportunities");

            migrationBuilder.DropIndex(
                name: "IX_CommunityOpportunities_EngagementScore",
                table: "CommunityOpportunities");

            migrationBuilder.DropIndex(
                name: "IX_CommunityOpportunities_IsArchived",
                table: "CommunityOpportunities");

            // Drop indexes for UserTasks
            migrationBuilder.DropIndex(
                name: "IX_UserTasks_Priority",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_IsCompleted",
                table: "UserTasks");

            migrationBuilder.DropIndex(
                name: "IX_UserTasks_DueDate",
                table: "UserTasks");
        }
    }
}
