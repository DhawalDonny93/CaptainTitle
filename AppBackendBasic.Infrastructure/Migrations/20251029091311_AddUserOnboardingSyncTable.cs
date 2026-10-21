using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingSyncTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OnboardingStepId",
                table: "UserOnboardingProgress",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "StepId",
                table: "UserOnboardingProgress",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepKey",
                table: "UserOnboardingProgress",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                table: "UserOnboardingProgress",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserOnboardingSyncId",
                table: "UserOnboardingProgress",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OnboardingQuestionId",
                table: "UserOnboardingAnswer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "QuestionId",
                table: "UserOnboardingAnswer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionKey",
                table: "UserOnboardingAnswer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserOnboardingProgressId",
                table: "UserOnboardingAnswer",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHtml",
                table: "OnboardingStep",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "OnboardingStep",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "OnboardingStep",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                table: "OnboardingStep",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserOnboardingSync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingSync", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingSync_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_UserOnboardingSyncId",
                table: "UserOnboardingProgress",
                column: "UserOnboardingSyncId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingAnswer_UserOnboardingProgressId",
                table: "UserOnboardingAnswer",
                column: "UserOnboardingProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingSync_UserId",
                table: "UserOnboardingSync",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOnboardingAnswer_UserOnboardingProgress_UserOnboardingP~",
                table: "UserOnboardingAnswer",
                column: "UserOnboardingProgressId",
                principalTable: "UserOnboardingProgress",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOnboardingProgress_UserOnboardingSync_UserOnboardingSyn~",
                table: "UserOnboardingProgress",
                column: "UserOnboardingSyncId",
                principalTable: "UserOnboardingSync",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOnboardingAnswer_UserOnboardingProgress_UserOnboardingP~",
                table: "UserOnboardingAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOnboardingProgress_UserOnboardingSync_UserOnboardingSyn~",
                table: "UserOnboardingProgress");

            migrationBuilder.DropTable(
                name: "UserOnboardingSync");

            migrationBuilder.DropIndex(
                name: "IX_UserOnboardingProgress_UserOnboardingSyncId",
                table: "UserOnboardingProgress");

            migrationBuilder.DropIndex(
                name: "IX_UserOnboardingAnswer_UserOnboardingProgressId",
                table: "UserOnboardingAnswer");

            migrationBuilder.DropColumn(
                name: "StepId",
                table: "UserOnboardingProgress");

            migrationBuilder.DropColumn(
                name: "StepKey",
                table: "UserOnboardingProgress");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "UserOnboardingProgress");

            migrationBuilder.DropColumn(
                name: "UserOnboardingSyncId",
                table: "UserOnboardingProgress");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "UserOnboardingAnswer");

            migrationBuilder.DropColumn(
                name: "QuestionKey",
                table: "UserOnboardingAnswer");

            migrationBuilder.DropColumn(
                name: "UserOnboardingProgressId",
                table: "UserOnboardingAnswer");

            migrationBuilder.DropColumn(
                name: "ContentHtml",
                table: "OnboardingStep");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "OnboardingStep");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "OnboardingStep");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "OnboardingStep");

            migrationBuilder.AlterColumn<Guid>(
                name: "OnboardingStepId",
                table: "UserOnboardingProgress",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OnboardingQuestionId",
                table: "UserOnboardingAnswer",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
