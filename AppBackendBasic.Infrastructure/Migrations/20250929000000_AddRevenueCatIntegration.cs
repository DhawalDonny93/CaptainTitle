using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <summary>
    /// Migration for RevenueCat integration
    /// </summary>
    public partial class AddRevenueCatIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RevenueCat-specific fields to Subscription table
            migrationBuilder.AddColumn<string>(
                name: "RevenueCatId",
                table: "Subscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntitlementIdentifier",
                table: "Subscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferIdentifier",
                table: "Subscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSandbox",
                table: "Subscription",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RenewalInfo",
                table: "Subscription",
                type: "jsonb",
                nullable: true);

            // Add index for RevenueCatId
            migrationBuilder.CreateIndex(
                name: "IX_Subscription_RevenueCatId",
                table: "Subscription",
                column: "RevenueCatId");

            // Add RevenueCat-specific fields to SubscriptionTransaction table
            migrationBuilder.AddColumn<string>(
                name: "RevenueCatEventId",
                table: "SubscriptionTransaction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreTransactionId",
                table: "SubscriptionTransaction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "SubscriptionTransaction",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawData",
                table: "SubscriptionTransaction",
                type: "jsonb",
                nullable: true);

            // Add index for RevenueCatEventId
            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransaction_RevenueCatEventId",
                table: "SubscriptionTransaction",
                column: "RevenueCatEventId");

            // Create RevenueCatWebhookEvent table
            migrationBuilder.CreateTable(
                name: "RevenueCatWebhookEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OriginalAppUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProductId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EntitlementId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PurchasedAtMs = table.Column<long>(type: "bigint", nullable: true),
                    ExpirationAtMs = table.Column<long>(type: "bigint", nullable: true),
                    Environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OriginalTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueCatWebhookEvent", x => x.Id);
                });

            // Add indexes for RevenueCatWebhookEvent
            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_EventType",
                table: "RevenueCatWebhookEvent",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_AppUserId",
                table: "RevenueCatWebhookEvent",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_OriginalTransactionId",
                table: "RevenueCatWebhookEvent",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_Processed",
                table: "RevenueCatWebhookEvent",
                column: "Processed");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_CreatedAt",
                table: "RevenueCatWebhookEvent",
                column: "CreatedAt");

            // Create UserRevenueCatMapping table
            migrationBuilder.CreateTable(
                name: "UserRevenueCatMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevenueCatId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRevenueCatMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRevenueCatMapping_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add indexes for UserRevenueCatMapping
            migrationBuilder.CreateIndex(
                name: "IX_UserRevenueCatMapping_UserId_RevenueCatId",
                table: "UserRevenueCatMapping",
                columns: new[] { "UserId", "RevenueCatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRevenueCatMapping_RevenueCatId",
                table: "UserRevenueCatMapping",
                column: "RevenueCatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop UserRevenueCatMapping table
            migrationBuilder.DropTable(
                name: "UserRevenueCatMapping");

            // Drop RevenueCatWebhookEvent table
            migrationBuilder.DropTable(
                name: "RevenueCatWebhookEvent");

            // Remove RevenueCat-specific fields from SubscriptionTransaction
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionTransaction_RevenueCatEventId",
                table: "SubscriptionTransaction");

            migrationBuilder.DropColumn(
                name: "RevenueCatEventId",
                table: "SubscriptionTransaction");

            migrationBuilder.DropColumn(
                name: "StoreTransactionId",
                table: "SubscriptionTransaction");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "SubscriptionTransaction");

            migrationBuilder.DropColumn(
                name: "RawData",
                table: "SubscriptionTransaction");

            // Remove RevenueCat-specific fields from Subscription
            migrationBuilder.DropIndex(
                name: "IX_Subscription_RevenueCatId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "RevenueCatId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "EntitlementIdentifier",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "OfferIdentifier",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "IsSandbox",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "RenewalInfo",
                table: "Subscription");
        }
    }
}
