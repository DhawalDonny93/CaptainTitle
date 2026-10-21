using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add Status field to UserTask table
    /// </summary>
    public partial class AddStatusToUserTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UserTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "TO-DO");

            // Update existing tasks to have TO-DO status if not completed, DONE if completed
            migrationBuilder.Sql(@"
                UPDATE UserTasks 
                SET Status = CASE 
                    WHEN IsCompleted = 1 THEN 'DONE' 
                    ELSE 'TO-DO' 
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserTasks");
        }
    }
}
