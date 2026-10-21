using System.ComponentModel.DataAnnotations;
using AppBackendBasic.Core.Attributes;

namespace AppBackendBasic.Core.Entities;

public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? FirstName { get; set; }  // ADD HERE

    [MaxLength(30)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }  // ADD HERE (encrypted)


    [MaxLength(100)]
    public string? Gender { get; set; }

    [Encrypted]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }

    [MaxLength(500)]
    public string? ProfilePhotoThumbnailUrl { get; set; }

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    public bool OnboardingCompleted { get; set; } = false;
    public bool IsFirstLogin { get; set; } = true;
    // Age Verification (COPPA Compliance)
    public bool AgeVerified { get; set; } = false;
    public bool RequiresParentalConsent { get; set; } = false;
    public bool ParentalConsentGranted { get; set; } = false;

    // Account Lockout
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    public bool? AgreesTermsandPolicy { get; set; } = true;

    // Account Deletion (GDPR - Right to be Forgotten)
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime? ScheduledDeletionDate { get; set; }
    public string? DeletionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<PaywallEvent> PaywallEvents { get; set; } = new List<PaywallEvent>();
    public virtual ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();
    public virtual ICollection<UserOnboardingProgress> OnboardingProgress { get; set; } = new List<UserOnboardingProgress>();
    public virtual ICollection<UserOnboardingAnswer> OnboardingAnswers { get; set; } = new List<UserOnboardingAnswer>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public bool IsSubscribed { get; set; }
    
    // Stripe Integration
    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }
}
