using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBackendBasic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserSession",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TimeoutMinutes",
                table: "UserSession",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "User",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AgeVerified",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AgreesTermsandPolicy",
                table: "User",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "User",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "User",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ParentalConsentGranted",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoThumbnailUrl",
                table: "User",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "User",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresParentalConsent",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDeletionDate",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawData",
                table: "SubscriptionTransaction",
                type: "jsonb",
                nullable: true);

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
                name: "EntitlementIdentifier",
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
                name: "OfferIdentifier",
                table: "Subscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenewalInfo",
                table: "Subscription",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCatId",
                table: "Subscription",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Changes = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DataExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDownloaded = table.Column<bool>(type: "boolean", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataExports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataExports_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeviceModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InAppNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActionText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InAppNotification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Marketing = table.Column<bool>(type: "boolean", nullable: false),
                    Updates = table.Column<bool>(type: "boolean", nullable: false),
                    Security = table.Column<bool>(type: "boolean", nullable: false),
                    Social = table.Column<bool>(type: "boolean", nullable: false),
                    Reminders = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingStep",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StepKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingStep", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentalConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsentToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentalConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentalConsents_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocuments", x => x.Id);
                });

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
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueCatWebhookEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sound = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Badge = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledNotification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomPreferences = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreference_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRole_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuestion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OnboardingStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValidationRules = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<string>(type: "text", nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HelpText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuestion_OnboardingStep_OnboardingStepId",
                        column: x => x.OnboardingStepId,
                        principalTable: "OnboardingStep",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnboardingStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingProgress_OnboardingStep_OnboardingStepId",
                        column: x => x.OnboardingStepId,
                        principalTable: "OnboardingStep",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOnboardingProgress_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPolicyAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicyAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPolicyAcceptances_PolicyDocuments_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPolicyAcceptances_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingAnswer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnboardingQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswerValue = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingAnswer_OnboardingQuestion_OnboardingQuestionId",
                        column: x => x.OnboardingQuestionId,
                        principalTable: "OnboardingQuestion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOnboardingAnswer_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataExports_UserId",
                table: "DataExports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId",
                table: "DeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotification_CreatedAt",
                table: "InAppNotification",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotification_IsArchived",
                table: "InAppNotification",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotification_IsRead",
                table: "InAppNotification",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotification_UserId",
                table: "InAppNotification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotification_UserId_IsRead_IsArchived",
                table: "InAppNotification",
                columns: new[] { "UserId", "IsRead", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestion_DisplayOrder",
                table: "OnboardingQuestion",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestion_OnboardingStepId_QuestionKey",
                table: "OnboardingQuestion",
                columns: new[] { "OnboardingStepId", "QuestionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStep_DisplayOrder",
                table: "OnboardingStep",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStep_StepKey",
                table: "OnboardingStep",
                column: "StepKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentalConsents_UserId",
                table: "ParentalConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_AppUserId",
                table: "RevenueCatWebhookEvent",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_CreatedAt",
                table: "RevenueCatWebhookEvent",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_EventType",
                table: "RevenueCatWebhookEvent",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_OriginalTransactionId",
                table: "RevenueCatWebhookEvent",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueCatWebhookEvent_Processed",
                table: "RevenueCatWebhookEvent",
                column: "Processed");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotification_IsCancelled",
                table: "ScheduledNotification",
                column: "IsCancelled");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotification_IsSent",
                table: "ScheduledNotification",
                column: "IsSent");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotification_IsSent_IsCancelled_ScheduledFor_Retry~",
                table: "ScheduledNotification",
                columns: new[] { "IsSent", "IsCancelled", "ScheduledFor", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotification_ScheduledFor",
                table: "ScheduledNotification",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotification_UserId",
                table: "ScheduledNotification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_Category",
                table: "SystemSetting",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_IsPublic",
                table: "SystemSetting",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_Key",
                table: "SystemSetting",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingAnswer_OnboardingQuestionId",
                table: "UserOnboardingAnswer",
                column: "OnboardingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingAnswer_UserId_OnboardingQuestionId",
                table: "UserOnboardingAnswer",
                columns: new[] { "UserId", "OnboardingQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_IsCompleted",
                table: "UserOnboardingProgress",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_OnboardingStepId",
                table: "UserOnboardingProgress",
                column: "OnboardingStepId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_UserId_OnboardingStepId",
                table: "UserOnboardingProgress",
                columns: new[] { "UserId", "OnboardingStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAcceptances_PolicyId",
                table: "UserPolicyAcceptances",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAcceptances_UserId",
                table: "UserPolicyAcceptances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreference_UserId",
                table: "UserPreference",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRevenueCatMapping_RevenueCatId",
                table: "UserRevenueCatMapping",
                column: "RevenueCatId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRevenueCatMapping_UserId_RevenueCatId",
                table: "UserRevenueCatMapping",
                columns: new[] { "UserId", "RevenueCatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_Role",
                table: "UserRole",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId_Role",
                table: "UserRole",
                columns: new[] { "UserId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DataExports");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "InAppNotification");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "ParentalConsents");

            migrationBuilder.DropTable(
                name: "RevenueCatWebhookEvent");

            migrationBuilder.DropTable(
                name: "ScheduledNotification");

            migrationBuilder.DropTable(
                name: "SystemSetting");

            migrationBuilder.DropTable(
                name: "UserOnboardingAnswer");

            migrationBuilder.DropTable(
                name: "UserOnboardingProgress");

            migrationBuilder.DropTable(
                name: "UserPolicyAcceptances");

            migrationBuilder.DropTable(
                name: "UserPreference");

            migrationBuilder.DropTable(
                name: "UserRevenueCatMapping");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "OnboardingQuestion");

            migrationBuilder.DropTable(
                name: "PolicyDocuments");

            migrationBuilder.DropTable(
                name: "OnboardingStep");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserSession");

            migrationBuilder.DropColumn(
                name: "TimeoutMinutes",
                table: "UserSession");

            migrationBuilder.DropColumn(
                name: "AgeVerified",
                table: "User");

            migrationBuilder.DropColumn(
                name: "AgreesTermsandPolicy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ParentalConsentGranted",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoThumbnailUrl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "User");

            migrationBuilder.DropColumn(
                name: "RequiresParentalConsent",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ScheduledDeletionDate",
                table: "User");

            migrationBuilder.DropColumn(
                name: "RawData",
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
                name: "EntitlementIdentifier",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "IsSandbox",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "OfferIdentifier",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "RenewalInfo",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "RevenueCatId",
                table: "Subscription");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "User",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
