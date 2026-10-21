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
    public DbSet<PromptEditHistory> PromptEditHistories { get; set; }

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

    // AI SEO Platform Entities
    public DbSet<Project> Projects { get; set; }
    public DbSet<BrandProfile> BrandProfiles { get; set; }
    public DbSet<Competitor> Competitors { get; set; }
    public DbSet<ContentArticle> ContentArticles { get; set; }
    public DbSet<AiTrackingResult> AiTrackingResults { get; set; }
    public DbSet<Keyword> Keywords { get; set; }
    public DbSet<CommunityOpportunity> CommunityOpportunities { get; set; }
    public DbSet<SiteAudit> SiteAudits { get; set; }
    public DbSet<UserTask> UserTasks { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    // LLM Provider Entities
    public DbSet<LlmProvider> LlmProviders { get; set; }
    public DbSet<LlmRequest> LlmRequests { get; set; }
    public DbSet<LlmCostTracking> LlmCostTrackings { get; set; }
    
    // Prompt Management Entities
    public DbSet<PromptTracking> PromptTrackings { get; set; }
    public DbSet<PromptPlatformTracking> PromptPlatformTrackings { get; set; }
    public DbSet<PromptVisibilityMetrics> PromptVisibilityMetrics { get; set; }
    public DbSet<PromptResearchData> PromptResearchDatas { get; set; }
    
    // Citations & Mentions Entities
    public DbSet<Citation> Citations { get; set; }
    public DbSet<CitationMetric> CitationMetrics { get; set; }
    public DbSet<BrandMention> BrandMentions { get; set; }
    public DbSet<CitationAggregation> CitationAggregations { get; set; }
    
    // Site Auditor Entities
    public DbSet<SiteAuditRun> SiteAuditRuns { get; set; }
    public DbSet<SiteAuditDetail> SiteAuditDetails { get; set; }
    public DbSet<SiteAuditHistory> SiteAuditHistories { get; set; }
    public DbSet<SiteAuditIssue> SiteAuditIssues { get; set; }
    
    // Opportunities Auditor Entities
    public DbSet<OpportunityScan> OpportunityScans { get; set; }
    public DbSet<ContentGapOpportunity> ContentGapOpportunities { get; set; }
    public DbSet<SeoOpportunity> SeoOpportunities { get; set; }
    public DbSet<VisibilityOpportunity> VisibilityOpportunities { get; set; }
    public DbSet<CompetitorAnalysis> CompetitorAnalyses { get; set; }
    public DbSet<OpportunityImpact> OpportunityImpacts { get; set; }
    
    // Visibility Dashboard Entities
    public DbSet<VisibilityDashboardMetrics> VisibilityDashboardMetrics { get; set; }
    public DbSet<VisibilityGoal> VisibilityGoals { get; set; }
    public DbSet<VisibilityAlert> VisibilityAlerts { get; set; }
    public DbSet<VisibilityTrend> VisibilityTrends { get; set; }
    public DbSet<CompetitiveIntelligence> CompetitiveIntelligence { get; set; }
    
    // LLM Provider Tracking
    public DbSet<LlmProviderConfig> LlmProviderConfigs { get; set; }
    public DbSet<PromptTemplate> PromptTemplates { get; set; }
    
    // LLM Job Configuration (admin-controlled provider+model per job)
    public DbSet<LlmJobConfig> LlmJobConfigs { get; set; }
    
    // Background Job Tracking
    public DbSet<JobExecutionLog> JobExecutionLogs { get; set; }
    
    // Admin Console Entities
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<SystemHealthMetrics> SystemHealthMetrics { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("User");
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasFilter("\"phoneNumber\" IS NOT NULL");
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
        // AI SEO Platform Entity Configurations
        // ==========================================

        // Project entity configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Projects");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Domain);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(p => p.User)
                  .WithMany() // Assuming User doesn't have a navigation property back to Projects yet
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentArticle entity configuration
        modelBuilder.Entity<ContentArticle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ContentArticles");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Slug);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(a => a.Project)
                  .WithMany(p => p.ContentArticles)
                  .HasForeignKey(a => a.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AiTrackingResult entity configuration
        modelBuilder.Entity<AiTrackingResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AiTrackingResults");
            entity.HasIndex(e => e.PromptId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.Prompt)
                  .WithMany(p => p.AiTrackingResults)
                  .HasForeignKey(r => r.PromptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

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

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
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

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Provider)
                  .WithMany()
                  .HasForeignKey(e => e.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PromptTracking entity configuration
        modelBuilder.Entity<PromptTracking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PromptTrackings");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Topic);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => new { e.ProjectId, e.IsCompetitor });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PromptText).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Topic).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PromptPlatformTracking entity configuration
        modelBuilder.Entity<PromptPlatformTracking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PromptPlatformTrackings");
            entity.HasIndex(e => e.PromptId);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => new { e.Platform, e.IsCompetitor });
            entity.HasIndex(e => new { e.PromptId, e.Platform }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Prompt)
                  .WithMany(p => p.PlatformTrackings)
                  .HasForeignKey(e => e.PromptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PromptVisibilityMetrics entity configuration
        modelBuilder.Entity<PromptVisibilityMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PromptVisibilityMetrics");
            entity.HasIndex(e => e.PromptPlatformId);
            entity.HasIndex(e => e.CheckDate);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => new { e.CheckDate, e.IsCompetitor });
            entity.HasIndex(e => new { e.PromptPlatformId, e.CheckDate });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Sentiment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.PromptPlatform)
                  .WithMany(p => p.VisibilityMetrics)
                  .HasForeignKey(e => e.PromptPlatformId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PromptResearchData entity configuration
        modelBuilder.Entity<PromptResearchData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PromptResearchDatas");
            entity.HasIndex(e => e.Topic);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.DiscoveredAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PromptText).IsRequired();
            entity.Property(e => e.Topic).HasMaxLength(200);
            entity.Property(e => e.Difficulty).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Citation entity configuration
        modelBuilder.Entity<Citation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Citations");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SourceDomain);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.DiscoveredAt);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => e.CompetitorId);
            entity.HasIndex(e => e.CompetitorType);
            entity.HasIndex(e => new { e.ProjectId, e.IsCompetitor });
            entity.HasIndex(e => new { e.Platform, e.IsCompetitor });
            entity.HasIndex(e => new { e.ProjectId, e.CompetitorType });
            entity.HasIndex(e => new { e.ProjectId, e.SourceUrl, e.Platform });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SourceUrl).IsRequired();
            entity.Property(e => e.SourceDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SourceTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CitationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CompetitorType).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Competitor)
                  .WithMany()
                  .HasForeignKey(e => e.CompetitorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // CitationMetric entity configuration
        modelBuilder.Entity<CitationMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CitationMetrics");
            entity.HasIndex(e => e.CitationId);
            entity.HasIndex(e => e.CheckDate);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => new { e.CheckDate, e.IsCompetitor });
            entity.HasIndex(e => new { e.CitationId, e.CheckDate });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Citation)
                  .WithMany(c => c.Metrics)
                  .HasForeignKey(e => e.CitationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BrandMention entity configuration
        modelBuilder.Entity<BrandMention>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("BrandMentions");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.CheckDate);
            entity.HasIndex(e => e.IsMentioned);
            entity.HasIndex(e => e.IsCompetitor);
            entity.HasIndex(e => e.CompetitorId);
            entity.HasIndex(e => e.CompetitorType);
            entity.HasIndex(e => new { e.ProjectId, e.IsCompetitor });
            entity.HasIndex(e => new { e.Platform, e.IsCompetitor });
            entity.HasIndex(e => new { e.ProjectId, e.CompetitorType });
            entity.HasIndex(e => new { e.ProjectId, e.Platform, e.CheckDate });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PromptText).IsRequired();
            entity.Property(e => e.MentionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Sentiment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CompetitorType).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Competitor)
                  .WithMany()
                  .HasForeignKey(e => e.CompetitorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // CitationAggregation entity configuration
        modelBuilder.Entity<CitationAggregation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CitationAggregations");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SourceDomain);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.AggregationDate);
            entity.HasIndex(e => new { e.ProjectId, e.SourceDomain, e.Platform, e.AggregationDate }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SourceDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteAudit entity configuration (legacy table)
        modelBuilder.Entity<SiteAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SiteAudits");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.AuditDate);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AuditDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteAuditRun entity configuration
        modelBuilder.Entity<SiteAuditRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SiteAuditRuns");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteAuditDetail entity configuration
        modelBuilder.Entity<SiteAuditDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SiteAuditDetails");
            entity.HasIndex(e => e.SiteAuditRunId);
            entity.HasIndex(e => e.CheckType);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CheckName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CheckType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DetailsJson).HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Ignore(e => e.Details);

            entity.HasOne(e => e.SiteAuditRun)
                  .WithMany(r => r.Details)
                  .HasForeignKey(e => e.SiteAuditRunId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteAuditHistory entity configuration
        modelBuilder.Entity<SiteAuditHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SiteAuditHistories");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.CheckDate);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OpportunityScan entity configuration
        modelBuilder.Entity<OpportunityScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OpportunityScans");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScanType);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ScanType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentGapOpportunity entity configuration
        modelBuilder.Entity<ContentGapOpportunity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ContentGapOpportunities");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EstimatedImpact);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecommendedContentType).HasMaxLength(50);
            entity.Property(e => e.EstimatedImpact).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.OpportunityScan)
                  .WithMany(s => s.ContentGapOpportunities)
                  .HasForeignKey(e => e.OpportunityScanId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // VisibilityOpportunity entity configuration
        modelBuilder.Entity<VisibilityOpportunity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("VisibilityOpportunities");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.PromptTrackingId);
            // Unique constraint: one active opportunity per (ProjectId, PromptTrackingId, OpportunityType, Platform)
            // Enforced at DB level via migration script; EF index mirrors it for query planning
            entity.HasIndex(e => new { e.ProjectId, e.PromptTrackingId, e.OpportunityType, e.Platform })
                  .HasDatabaseName("idx_visibility_opportunities_unique_prompt");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PromptTrackingId).IsRequired(false);
            entity.Property(e => e.PromptText).IsRequired();
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OpportunityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DiscoveredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PromptTracking)
                  .WithMany()
                  .HasForeignKey(e => e.PromptTrackingId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        // CompetitorAnalysis entity configuration
        modelBuilder.Entity<CompetitorAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CompetitorAnalyses");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.CompetitorDomain);
            entity.HasIndex(e => new { e.ProjectId, e.CompetitorDomain }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CompetitorDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CompetitorName).HasMaxLength(255);
            entity.Property(e => e.CitationShare).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OpportunityImpact entity configuration
        modelBuilder.Entity<OpportunityImpact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OpportunityImpacts");
            entity.HasIndex(e => e.OpportunityType);
            entity.HasIndex(e => e.OpportunityId);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OpportunityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ImpactType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EstimatedValue).HasPrecision(10, 2);
            entity.Property(e => e.ActualValue).HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // CommunityOpportunity entity configuration
        modelBuilder.Entity<CommunityOpportunity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CommunityOpportunities");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Platform);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.EngagementScore);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Sentiment).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Keyword)
                  .WithMany()
                  .HasForeignKey(e => e.KeywordId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // VisibilityDashboardMetrics entity configuration
        modelBuilder.Entity<VisibilityDashboardMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("VisibilityDashboardMetrics");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.MetricDate);
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => new { e.ProjectId, e.MetricDate, e.Period }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Period).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AveragePromptScore).HasPrecision(5, 2);
            entity.Property(e => e.MentionRate).HasPrecision(5, 2);
            entity.Property(e => e.CitationShare).HasPrecision(5, 2);
            entity.Property(e => e.VisibilityChange).HasPrecision(5, 2);
            entity.Property(e => e.PlatformMetrics).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // VisibilityGoal entity configuration
        modelBuilder.Entity<VisibilityGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("VisibilityGoals");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TargetDate);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.GoalType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TargetValue).HasPrecision(10, 2);
            entity.Property(e => e.CurrentValue).HasPrecision(10, 2);
            entity.Property(e => e.Progress).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // VisibilityAlert entity configuration
        modelBuilder.Entity<VisibilityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("VisibilityAlerts");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Platform).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CompetitiveIntelligence entity configuration
        modelBuilder.Entity<CompetitiveIntelligence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CompetitiveIntelligence");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.MetricDate);
            entity.HasIndex(e => e.CompetitorDomain);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CompetitorDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CitationShare).HasPrecision(5, 2);
            entity.Property(e => e.AverageMentionPosition).HasPrecision(4, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // UserTask entity configuration
        modelBuilder.Entity<UserTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserTasks");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // BrandProfile entity configuration
        modelBuilder.Entity<BrandProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("BrandProfiles");
            entity.HasIndex(e => e.ProjectId).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Overview).HasMaxLength(120);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Competitor entity configuration
        modelBuilder.Entity<Competitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Competitors");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.CompetitorType);
            entity.HasIndex(e => new { e.ProjectId, e.CompetitorType });
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.CompetitorType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            entity.HasMany(t => t.Projects)
                  .WithOne(p => p.Team)
                  .HasForeignKey(p => p.TeamId)
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

        // PromptEditHistory entity configuration
        modelBuilder.Entity<PromptEditHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("PromptEditHistory");
            entity.HasIndex(e => e.PromptTrackingId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => e.ChangeType);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

            entity.HasOne(h => h.PromptTracking)
                  .WithMany()
                  .HasForeignKey(h => h.PromptTrackingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.User)
                  .WithMany()
                  .HasForeignKey(h => h.UserId)
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
}
