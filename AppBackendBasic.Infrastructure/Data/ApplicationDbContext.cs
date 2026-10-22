using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProvider> UserProviders { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<OtpVerification> OtpVerifications { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionTransaction> SubscriptionTransactions { get; set; }
    public DbSet<PaywallEvent> PaywallEvents { get; set; }
    public DbSet<RevenueCatWebhookEvent> RevenueCatWebhookEvents { get; set; }
    public DbSet<UserRevenueCatMapping> UserRevenueCatMappings { get; set; }
    public DbSet<OnboardingStep> OnboardingSteps { get; set; }
    public DbSet<OnboardingQuestion> OnboardingQuestions { get; set; }
    public DbSet<UserOnboardingSync> UserOnboardingSyncs { get; set; }
    public DbSet<UserOnboardingProgress> UserOnboardingProgress { get; set; }
    public DbSet<UserOnboardingAnswer> UserOnboardingAnswers { get; set; }
    public DbSet<DataExport> DataExports { get; set; }
    public DbSet<PolicyDocument> PolicyDocuments { get; set; }
    public DbSet<UserPolicyAcceptance> UserPolicyAcceptances { get; set; }
    public DbSet<ParentalConsent> ParentalConsents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<ScheduledNotification> ScheduledNotifications { get; set; }
    public DbSet<InAppNotification> InAppNotifications { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    // Team Management Entities
    public DbSet<Team> Teams { get; set; }
    public DbSet<UserTeamMapping> UserTeamMappings { get; set; }

    public DbSet<LlmConversation> LlmConversations { get; set; }
    public DbSet<LlmMessage> LlmMessages { get; set; }
    public DbSet<LlmUsageQuota> LlmUsageQuotas { get; set; }
    public DbSet<LlmPromptTemplate> LlmPromptTemplates { get; set; }

    // Subscription Management Entities
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<WaitlistSignup> WaitlistSignups { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<UsageQuota> UsageQuotas { get; set; }
    public DbSet<QuotaUsageLog> QuotaUsageLogs { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    
    // LLM Provider Entities
    public DbSet<LlmProvider> LlmProviders { get; set; }
    public DbSet<LlmRequest> LlmRequests { get; set; }
    public DbSet<LlmCostTracking> LlmCostTrackings { get; set; }
    
    // LLM Provider Tracking
    public DbSet<LlmProviderConfig> LlmProviderConfigs { get; set; }
    
    // LLM Job Configuration (admin-controlled provider+model per job)
    public DbSet<LlmJobConfig> LlmJobConfigs { get; set; }
    
    // Background Job Tracking
    
    // Admin Console Entities
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<SystemHealthMetrics> SystemHealthMetrics { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    
    // Captain Title (caption generator) entities
    public DbSet<CaptionProject> CaptionProjects { get; set; }
    public DbSet<CaptionUsage> CaptionUsages { get; set; }
    public DbSet<CaptionPaymentTransaction> CaptionPaymentTransactions { get; set; }
    public DbSet<CaptionTheme> CaptionThemes { get; set; }
    public DbSet<CaptionFont> CaptionFonts { get; set; }
    public DbSet<CaptionVariant> CaptionVariants { get; set; }
    public DbSet<CaptionSfxType> CaptionSfxTypes { get; set; }
    public DbSet<CaptionSticker> CaptionStickers { get; set; }
    public DbSet<CaptionPlan> CaptionPlans { get; set; }
    public DbSet<CaptionFillerWord> CaptionFillerWords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureCaptionEntities(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("User");
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasFilter("\"PhoneNumber\" IS NOT NULL");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        });

        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("NotificationPreference");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.NotificationPreferences)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProvider entity configuration
        modelBuilder.Entity<UserProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserProvider");
            entity.HasIndex(e => new { e.Provider, e.ProviderId }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ProviderData).HasColumnType("jsonb"); // PostgreSQL JSONB type
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserProviders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken entity configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("RefreshToken");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
            entity.Property(e => e.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSession entity configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserSession");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastActiveAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OtpVerification entity configuration
        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OtpVerification");
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => new { e.PhoneNumber, e.CreatedAt });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // PasswordReset entity configuration
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PasswordReset");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.Email, e.CreatedAt });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DeviceRegistration");
            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RegisteredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Subscription entity configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Subscription");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.ExpiryDate);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.OriginalTransactionId);
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.PurchaseToken }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(s => s.User)
                  .WithMany(u => u.Subscriptions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // SubscriptionTransaction entity configuration
        modelBuilder.Entity<SubscriptionTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SubscriptionTransaction");
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.TransactionId);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(t => t.Subscription)
                  .WithMany(s => s.Transactions)
                  .HasForeignKey(t => t.SubscriptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PaywallEvent entity configuration
        modelBuilder.Entity<PaywallEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PaywallEvent");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.PaywallId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PaywallEvents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // RevenueCatWebhookEvent entity configuration
        modelBuilder.Entity<RevenueCatWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("RevenueCatWebhookEvent");
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.AppUserId);
            entity.HasIndex(e => e.OriginalTransactionId);
            entity.HasIndex(e => e.Processed);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserRevenueCatMapping entity configuration
        modelBuilder.Entity<UserRevenueCatMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserRevenueCatMapping");
            entity.HasIndex(e => new { e.UserId, e.RevenueCatId }).IsUnique();
            entity.HasIndex(e => e.RevenueCatId);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(m => m.User)
                  .WithMany()
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // OnboardingStep entity configuration
        modelBuilder.Entity<OnboardingStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OnboardingStep");
            entity.HasIndex(e => e.StepKey).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // OnboardingQuestion entity configuration
        modelBuilder.Entity<OnboardingQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OnboardingQuestion");
            entity.HasIndex(e => new { e.OnboardingStepId, e.QuestionKey }).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(q => q.OnboardingStep)
                  .WithMany(s => s.Questions)
                  .HasForeignKey(q => q.OnboardingStepId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // UserOnboardingProgress entity configuration
        modelBuilder.Entity<UserOnboardingProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserOnboardingProgress");
            entity.HasIndex(e => new { e.UserId, e.OnboardingStepId }).IsUnique();
            entity.HasIndex(e => e.IsCompleted);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(p => p.User)
                  .WithMany(u => u.OnboardingProgress)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(p => p.OnboardingStep)
                  .WithMany(s => s.UserProgress)
                  .HasForeignKey(p => p.OnboardingStepId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // UserOnboardingAnswer entity configuration
        modelBuilder.Entity<UserOnboardingAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserOnboardingAnswer");
            entity.HasIndex(e => new { e.UserId, e.OnboardingQuestionId }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(a => a.User)
                  .WithMany(u => u.OnboardingAnswers)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(a => a.OnboardingQuestion)
                  .WithMany(q => q.UserAnswers)
                  .HasForeignKey(a => a.OnboardingQuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // UserOnboardingSync entity configuration
        modelBuilder.Entity<UserOnboardingSync>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserOnboardingSync");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ScheduledNotification entity configuration
        modelBuilder.Entity<ScheduledNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ScheduledNotification");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => e.IsSent);
            entity.HasIndex(e => e.IsCancelled);
            entity.HasIndex(e => new { e.IsSent, e.IsCancelled, e.ScheduledFor, e.RetryCount });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // InAppNotification entity configuration
        modelBuilder.Entity<InAppNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InAppNotification");
            
            // Explicit column mappings for PostgreSQL case sensitivity
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Body).HasColumnName("Body");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.Category).HasColumnName("Category");
            entity.Property(e => e.ActionUrl).HasColumnName("ActionUrl");
            entity.Property(e => e.ActionText).HasColumnName("ActionText");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageUrl");
            entity.Property(e => e.DataJson).HasColumnName("DataJson");
            entity.Property(e => e.IsRead).HasColumnName("IsRead");
            entity.Property(e => e.ReadAt).HasColumnName("ReadAt");
            entity.Property(e => e.IsArchived).HasColumnName("IsArchived");
            entity.Property(e => e.ArchivedAt).HasColumnName("ArchivedAt");
            entity.Property(e => e.Priority).HasColumnName("Priority");
            entity.Property(e => e.ExpiresAt).HasColumnName("ExpiresAt");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.IsArchived });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Notification entity configuration (unified for all notification types)
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Notifications");
            
            // Explicit column mappings for PostgreSQL case sensitivity
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Body).HasColumnName("Body");
            entity.Property(e => e.Type).HasColumnName("Type");
            
            // Web notification fields
            entity.Property(e => e.Icon).HasColumnName("Icon");
            entity.Property(e => e.IsGlobal).HasColumnName("IsGlobal");
            entity.Property(e => e.IsDismissible).HasColumnName("IsDismissible");
            entity.Property(e => e.DurationMs).HasColumnName("DurationMs");
            entity.Property(e => e.ActionUrl).HasColumnName("ActionUrl");
            entity.Property(e => e.ActionText).HasColumnName("ActionText");
            
            // Push notification fields
            entity.Property(e => e.Data).HasColumnName("Data");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageUrl");
            entity.Property(e => e.IsSent).HasColumnName("IsSent");
            entity.Property(e => e.SentAt).HasColumnName("SentAt");
            entity.Property(e => e.ScheduledFor).HasColumnName("ScheduledFor");
            
            // Common fields
            entity.Property(e => e.IsRead).HasColumnName("IsRead");
            entity.Property(e => e.ReadAt).HasColumnName("ReadAt");
            entity.Property(e => e.ExpiresAt).HasColumnName("ExpiresAt");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsGlobal);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.IsSent);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsGlobal, e.IsRead });
            
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // UserRole entity configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserRole");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => new { e.UserId, e.Role }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // SystemSetting entity configuration
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SystemSetting");
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublic);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserProfile entity configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserProfile");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.Country);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Theme).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timezone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.TwitterUrl).HasMaxLength(500);
            entity.Property(e => e.GitHubUrl).HasMaxLength(500);
            entity.Property(e => e.DateFormat).HasMaxLength(20);
            entity.Property(e => e.TimeFormat).HasMaxLength(10);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.ProfileVisibility).HasMaxLength(20);
            entity.Property(e => e.CustomPreferences).HasColumnType("jsonb");
            entity.Property(e => e.ExtendedProperties).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // DeviceToken entity configuration
        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DeviceToken");
            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique();
            entity.HasIndex(e => e.Token);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // DataExport entity configuration
        modelBuilder.Entity<DataExport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("DataExport");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExportId).IsUnique();
            entity.HasIndex(e => e.IsReady);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PolicyDocument entity configuration
        modelBuilder.Entity<PolicyDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PolicyDocument");
            entity.HasIndex(e => new { e.Type, e.Version }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserPolicyAcceptance entity configuration
        modelBuilder.Entity<UserPolicyAcceptance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserPolicyAcceptance");
            entity.HasIndex(e => new { e.UserId, e.PolicyId }).IsUnique();
            entity.HasIndex(e => e.AcceptedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AcceptedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Policy)
                  .WithMany()
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ParentalConsent entity configuration
        modelBuilder.Entity<ParentalConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ParentalConsent");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConsentToken).IsUnique();
            entity.HasIndex(e => e.IsGranted);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // AuditLog entity configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AuditLog");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // LlmConversation entity configuration
        modelBuilder.Entity<LlmConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmConversation");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // LlmMessage entity configuration
        modelBuilder.Entity<LlmMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmMessage");
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // LlmUsageQuota entity configuration
        modelBuilder.Entity<LlmUsageQuota>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmUsageQuota");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // LlmPromptTemplate entity configuration
        modelBuilder.Entity<LlmPromptTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmPromptTemplate");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ==========================================
        // LLM provider / request tracking configuration
        // ==========================================

        // LlmProvider entity configuration
        modelBuilder.Entity<LlmProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmProvider");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.CostPer1kInputTokens).HasPrecision(10, 6);
            entity.Property(e => e.CostPer1kOutputTokens).HasPrecision(10, 6);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // LlmProviderConfig entity configuration
        modelBuilder.Entity<LlmProviderConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmProviderConfig");
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => new { e.ProviderId, e.ConfigKey }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("Id");
            entity.Property(e => e.ProviderId).IsRequired().HasColumnName("ProviderId");
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(100).HasColumnName("ConfigKey");
            entity.Property(e => e.ConfigValue).HasColumnName("ConfigValue");
            entity.Property(e => e.IsEncrypted).HasColumnName("IsEncrypted");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("UpdatedAt");

            entity.HasOne(e => e.Provider)
                  .WithMany(p => p.Configs)
                  .HasForeignKey(e => e.ProviderId)
                  .HasConstraintName("FK_LlmProviderConfig_Provider")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LlmJobConfig entity configuration
        modelBuilder.Entity<LlmJobConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmJobConfigs");
            entity.HasIndex(e => e.JobKey).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JobKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.JobName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Temperature).HasPrecision(4, 2);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // LlmRequest entity configuration
        modelBuilder.Entity<LlmRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmRequest");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.RequestType);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Temperature).HasPrecision(3, 2);
            entity.Property(e => e.Cost).HasPrecision(10, 6);
            entity.Property(e => e.ResponseJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Provider)
                  .WithMany(p => p.Requests)
                  .HasForeignKey(e => e.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // LlmCostTracking entity configuration
        modelBuilder.Entity<LlmCostTracking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("LlmCostTracking");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.TrackingDate);
            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.ProviderId, e.TrackingDate }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TotalCost).HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Provider)
                  .WithMany()
                  .HasForeignKey(e => e.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AdminAuditLog entity configuration
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AdminAuditLog");
            entity.HasIndex(e => e.AdminUserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.AdminUser)
                  .WithMany()
                  .HasForeignKey(e => e.AdminUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FeatureFlag entity configuration
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FeatureFlag");
            entity.HasIndex(e => e.FlagKey).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FlagKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // SystemHealthMetrics entity configuration
        modelBuilder.Entity<SystemHealthMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SystemHealthMetrics");
            entity.HasIndex(e => e.MetricDate);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CpuUsagePercent).HasPrecision(5, 2);
            entity.Property(e => e.ErrorRate).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // EmailTemplate entity configuration
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EmailTemplate");
            entity.HasIndex(e => e.TemplateKey).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TemplateKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HtmlBody).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Team entity configuration
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Team");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.StripeCustomerId).HasFilter("\"StripeCustomerId\" IS NOT NULL");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

            entity.HasMany(t => t.UserMappings)
                  .WithOne(m => m.Team)
                  .HasForeignKey(m => m.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

        });

        // UserTeamMapping entity configuration
        modelBuilder.Entity<UserTeamMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserTeamMapping");
            entity.HasIndex(e => new { e.UserId, e.TeamId }).IsUnique();
            entity.HasIndex(e => e.TeamId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

            entity.HasOne(m => m.User)
                  .WithMany()
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Team)
                  .WithMany(t => t.UserMappings)
                  .HasForeignKey(m => m.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }

    public override int SaveChanges()
    {
        //ConvertDateTimesToUtc();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        //ConvertDateTimesToUtc();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ConvertDateTimesToUtc()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime))
                {
                    var value = (DateTime?)property.CurrentValue;
                    if (value.HasValue && value.Value.Kind == DateTimeKind.Unspecified)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                    }
                }
                else if (property.Metadata.ClrType == typeof(DateTime?))
                {
                    var value = (DateTime?)property.CurrentValue;
                    if (value.HasValue && value.Value.Kind == DateTimeKind.Unspecified)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                    }
                }
            }
        }
    }

    private static void ConfigureCaptionEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaptionProject>(e =>
        {
            e.ToTable("CaptionProjects");
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.UserId, p.CreatedAt });
            e.HasIndex(p => p.Status);
        });
        modelBuilder.Entity<CaptionUsage>(e =>
        {
            e.ToTable("CaptionUsages");
            e.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(u => u.UserId).IsUnique();
        });
        modelBuilder.Entity<CaptionPaymentTransaction>(e =>
        {
            e.ToTable("CaptionPaymentTransactions");
            e.HasIndex(t => t.SessionId).IsUnique();
            e.HasIndex(t => t.UserId);
        });
        modelBuilder.Entity<CaptionTheme>(e => { e.ToTable("CaptionThemes"); e.HasIndex(x => x.Key).IsUnique(); });
        modelBuilder.Entity<CaptionFont>(e => { e.ToTable("CaptionFonts"); e.HasIndex(x => x.Key).IsUnique(); });
        modelBuilder.Entity<CaptionVariant>(e => { e.ToTable("CaptionVariants"); e.HasIndex(x => x.Key).IsUnique(); });
        modelBuilder.Entity<CaptionSfxType>(e => { e.ToTable("CaptionSfxTypes"); e.HasIndex(x => x.Key).IsUnique(); });
        modelBuilder.Entity<CaptionSticker>(e => { e.ToTable("CaptionStickers"); e.HasIndex(x => x.Glyph).IsUnique(); });
        modelBuilder.Entity<CaptionPlan>(e => { e.ToTable("CaptionPlans"); e.HasIndex(x => x.Key).IsUnique(); });
        modelBuilder.Entity<CaptionFillerWord>(e => { e.ToTable("CaptionFillerWords"); e.HasIndex(x => x.Word).IsUnique(); });
    }
}
