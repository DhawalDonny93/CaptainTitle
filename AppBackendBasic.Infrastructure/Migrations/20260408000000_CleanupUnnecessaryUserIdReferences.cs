using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupUnnecessaryUserIdReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop UserId column from Citations table (no longer needed, tracked via PromptTracking)
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Citations");

            // Drop UserId column from SiteAudit table (team-level audit, not user-specific)
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SiteAudit");

            // Drop User foreign key from SiteAudit if it exists
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_SiteAudit_User_UserId",
                    table: "SiteAudit");
            }
            catch
            {
                // Foreign key may not exist
            }

            // Drop CompletedBy column from SiteAuditIssues table
            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "SiteAuditIssues");

            // Drop CompletedByUser foreign key if it exists
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_SiteAuditIssues_User_CompletedBy",
                    table: "SiteAuditIssues");
            }
            catch
            {
                // Foreign key may not exist
            }

            // Drop UserId column from Keyword table (project-level keywords, not user-specific)
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Keyword");

            // Drop User foreign key from Keyword if it exists
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_Keyword_User_UserId",
                    table: "Keyword");
            }
            catch
            {
                // Foreign key may not exist
            }

            // Add TeamId column to Keyword if it doesn't exist
            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Keyword",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Add TeamId column to SiteAudit if it doesn't exist
            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "SiteAudit",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore UserId column to Citations table
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Citations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Restore UserId column to SiteAudit table
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SiteAudit",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Restore the User foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_SiteAudit_User_UserId",
                table: "SiteAudit",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            // Restore CompletedBy column to SiteAuditIssues table
            migrationBuilder.AddColumn<int>(
                name: "CompletedBy",
                table: "SiteAuditIssues",
                type: "integer",
                nullable: true);

            // Restore the foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_SiteAuditIssues_User_CompletedBy",
                table: "SiteAuditIssues",
                column: "CompletedBy",
                principalTable: "User",
                principalColumn: "Id");

            // Restore UserId column to Keyword table
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Keyword",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Restore the User foreign key for Keyword
            migrationBuilder.AddForeignKey(
                name: "FK_Keyword_User_UserId",
                table: "Keyword",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            // Remove TeamId columns
            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Keyword");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "SiteAudit");
        }
    }
}
