using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Serilog;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;
using AppBackendBasic.Infrastructure.Services;
using AppBackendBasic.Services.Authentication;
using AppBackendBasic.Services.Supabase;
using AppBackendBasic.Services.Subscriptions;
using FluentValidation.AspNetCore;
using System.Text.Json.Serialization;
using AppBackendBasic.API.Middleware;
using AppBackendBasic.Services.Onboarding;
using AppBackendBasic.Services.Device;
using AppBackendBasic.Services.Security;
using AppBackendBasic.Services.Account;
using AppBackendBasic.Services.Compliance;
using AppBackendBasic.Services.Notifications;
using AppBackendBasic.Services.Media;
using AppBackendBasic.Services.BackgroundJobs;
using AppBackendBasic.Services.Admin;
using AppBackendBasic.Services.Caching;
using Microsoft.AspNetCore.DataProtection;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using AppBackendBasic.Services.BackgroundJobs.Jobs;
using AppBackendBasic.Services.LLM;
using AppBackendBasic.Infrastructure.Services;
using AppBackendBasic.Services.Captions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Configure database with Supabase-optimized settings
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Add connection pooling parameters to connection string
    var builder_cs = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
    {
        // Conservative pooling for Supabase to avoid connection exhaustion
        Pooling = true,
        MinPoolSize = 1,
        MaxPoolSize = 15, // 15 EF + 5 Hangfire = 20 total, stays under Supabase limits
        ConnectionIdleLifetime = 60, // 1 minute - shorter to release connections faster
        ConnectionPruningInterval = 10,
        
        // Timeout settings
        Timeout = 30,
        CommandTimeout = 30,
        
        // Keepalive to detect dead connections
        KeepAlive = 30,
        TcpKeepAlive = true,
        TcpKeepAliveInterval = 10,
        TcpKeepAliveTime = 30
    };
    
    options.UseNpgsql(builder_cs.ConnectionString, npgsqlOptions =>
    {
        // DO NOT enable retry on failure - it causes duplicate inserts
        // The application handles retries at the service layer instead
        
        // Use prepared statements for better performance
        npgsqlOptions.MaxBatchSize(100);
    });
    
    // Enable sensitive data logging in development for debugging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
}, ServiceLifetime.Scoped); // Explicitly set to Scoped

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            logger.LogInformation("Token received for path: {Path}, Token length: {Length}", 
                context.Request.Path, token?.Length ?? 0);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Authentication failed for path: {Path}. Error: {Error}", 
                context.Request.Path, context.Exception.Message);
            
            // Log more details about the failure
            if (context.Exception is SecurityTokenExpiredException)
            {
                logger.LogError("Token has expired");
            }
            else if (context.Exception is SecurityTokenInvalidIssuerException)
            {
                logger.LogError("Invalid issuer. Expected: {Expected}", 
                    context.Options.TokenValidationParameters.ValidIssuer);
            }
            else if (context.Exception is SecurityTokenInvalidAudienceException)
            {
                logger.LogError("Invalid audience. Expected: {Expected}", 
                    context.Options.TokenValidationParameters.ValidAudience);
            }
            else if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
            {
                logger.LogError("Signature key not found or invalid");
            }
            
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("Token validated successfully for user: {UserId}, path: {Path}", userId, context.Request.Path);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication challenge for path: {Path}. Error: {Error}, ErrorDescription: {ErrorDescription}", 
                context.Request.Path, context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Add MemoryCache for rate limiting
builder.Services.AddMemoryCache();

// Configure Data Protection for encryption at rest
builder.Services.AddDataProtection()
    .SetApplicationName("CaptainTitle")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")));

// Register repositories and services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserProviderRepository, UserProviderRepository>();
builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();
builder.Services.AddScoped<IUserTeamMappingRepository, UserTeamMappingRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddHttpClient<IEmailService, SendGridEmailService>();
builder.Services.AddHttpClient<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<EmailVerificationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IBiometricAuthService, BiometricAuthService>();
builder.Services.AddHttpClient<IAppleAuthService, AppleAuthService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
// Web-only subscription service (simplified, no mobile/RevenueCat)
builder.Services.AddScoped<IWebSubscriptionService, WebSubscriptionService>();
// Keep old subscription service for backward compatibility (used by LlmService, WebhooksController, etc.)
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<OnboardingDataPopulationService>();
builder.Services.AddScoped<OnboardingSeedService>();
builder.Services.AddScoped<OnboardingManagementService>();
builder.Services.AddScoped<DeviceManagementService>();
builder.Services.AddScoped<IPaywallAnalyticsService, PaywallAnalyticsService>();
builder.Services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
// Register Supabase service
builder.Services.AddHttpClient<SupabaseService>();
builder.Services.AddScoped<SupabaseService>();
// Register Security services
builder.Services.AddScoped<IInputSanitizationService, InputSanitizationService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
// Register Account services
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
// Register Compliance services
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IAgeVerificationService, AgeVerificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
// Register Notification services
builder.Services.AddHttpClient("ExpoClient", client =>
{
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<ExpoPushNotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
builder.Services.AddScoped<INotificationSchedulerService, NotificationSchedulerService>();
builder.Services.AddScoped<IInAppNotificationService, InAppNotificationService>();
builder.Services.AddScoped<NotificationTemplateService>();
builder.Services.AddScoped<UnsubscribeTokenService>();
builder.Services.AddScoped<IWebNotificationService, WebNotificationService>();
// Register Admin services
builder.Services.AddScoped<AdminSeedService>();
builder.Services.AddScoped<SystemSettingsService>();
// Register Caching services
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
if (redisEnabled)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:ConnectionString"];
        options.InstanceName = builder.Configuration["Redis:InstanceName"];
    });
    builder.Services.AddScoped<ICacheService, CacheService>();
}
else
{
    // Use memory cache as fallback
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddScoped<ICacheService, CacheService>();
}
// Register Background Jobs
// TODO: Uncomment NotificationProcessorJob registration when email notification pipeline is fully implemented
// This service processes queued notifications and sends them via configured email providers
// Dependencies: Email service integration, notification queue setup, retry logic
//builder.Services.AddHostedService<NotificationProcessorJob>();

// Hangfire is DISABLED for now (background jobs not required). Re-enable by uncommenting this block and the dashboard block below.
//// Configure Hangfire with PostgreSQL storage (pool-limited to avoid Supabase connection exhaustion)
//var hangfireConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(
//    builder.Configuration.GetConnectionString("DefaultConnection"))
//{
//    Pooling = true,
//    MinPoolSize = 1,
//    MaxPoolSize = 5, // Hangfire needs far fewer connections than EF Core
//    ConnectionIdleLifetime = 60,
//    ConnectionPruningInterval = 10,
//}.ConnectionString;

//builder.Services.AddHangfire(configuration => configuration
//    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
//    .UseSimpleAssemblyNameTypeSerializer()
//    .UseRecommendedSerializerSettings()
//    .UsePostgreSqlStorage(hangfireConnectionString));

//// Enable Hangfire server for background job processing
//builder.Services.AddHangfireServer();

//// Register Hangfire job services
//builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
//builder.Services.AddScoped<EmailQueueJob>();
//builder.Services.AddScoped<CleanupJob>();
//builder.Services.AddScoped<JobScheduler>();

// Register Captain Title (caption generator) services
builder.Services.Configure<CaptionOptions>(builder.Configuration.GetSection(CaptionOptions.SectionName));
builder.Services.AddSingleton<ICaptionStorage, LocalCaptionStorage>();
builder.Services.AddSingleton<IFfmpegService, FfmpegService>();
builder.Services.AddSingleton<ICaptionMediaTokenService, CaptionMediaTokenService>();
builder.Services.AddHttpClient<IWhisperClient, WhisperClient>();
builder.Services.AddHttpClient<ISarvamClient, SarvamClient>();
builder.Services.AddScoped<ICaptionCatalogService, CaptionCatalogService>();
builder.Services.AddScoped<ICaptionEntitlementService, CaptionEntitlementService>();
builder.Services.AddScoped<ICaptionTranscriptionService, CaptionTranscriptionService>();
builder.Services.AddScoped<IHinglishService, HinglishService>();
builder.Services.AddScoped<ICaptionProjectService, CaptionProjectService>();
builder.Services.AddScoped<ICaptionJobRunner, CaptionJobRunner>();
builder.Services.AddScoped<ICaptionRenderService, CaptionRenderService>();
builder.Services.AddHttpClient<ICaptionBillingService, CaptionBillingService>();
builder.Services.AddSingleton<CaptionJobWorker>();
builder.Services.AddSingleton<ICaptionJobQueue>(sp => sp.GetRequiredService<CaptionJobWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<CaptionJobWorker>());

// Register Media services
builder.Services.AddScoped<IPhotoService, PhotoService>();
// Register LLM services
builder.Services.AddHttpClient<ILlmService, LlmService>();
builder.Services.AddScoped<ILlmService, LlmService>();


// Register LLM Provider services
builder.Services.AddHttpClient<AppBackendBasic.Infrastructure.Services.LlmProviders.OpenAIProvider>();
builder.Services.AddHttpClient<AppBackendBasic.Infrastructure.Services.LlmProviders.AnthropicProvider>();
builder.Services.AddHttpClient<AppBackendBasic.Infrastructure.Services.LlmProviders.GoogleGeminiProvider>();
builder.Services.AddHttpClient<AppBackendBasic.Infrastructure.Services.LlmProviders.PerplexityProvider>();
builder.Services.AddHttpClient<AppBackendBasic.Infrastructure.Services.LlmProviders.GoogleSearchProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.OpenAIProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.AnthropicProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.GoogleGeminiProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.PerplexityProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.GoogleSearchProvider>();
builder.Services.AddScoped<AppBackendBasic.Infrastructure.Services.LlmProviders.LlmProviderFactory>(); // Changed from Singleton to Scoped
builder.Services.AddScoped<ILlmProviderConfigService, AppBackendBasic.Infrastructure.Services.LlmProviderConfigService>();
builder.Services.AddScoped<ILlmProviderService, AppBackendBasic.Infrastructure.Services.LlmProviderService>();
builder.Services.AddScoped<AppBackendBasic.Core.Interfaces.ILlmJobConfigService, AppBackendBasic.Infrastructure.Services.LlmJobConfigService>();

// Register application services
builder.Services.AddScoped<IAdminMonitoringService, AppBackendBasic.Infrastructure.Services.AdminMonitoringService>();
builder.Services.AddScoped<IFeatureFlagService, AppBackendBasic.Infrastructure.Services.FeatureFlagService>();
builder.Services.AddScoped<IPaymentService, AppBackendBasic.Infrastructure.Services.StripePaymentService>();
builder.Services.AddScoped<IEmailTemplateService, AppBackendBasic.Infrastructure.Services.EmailTemplateService>();
builder.Services.AddScoped<IAnalyticsService, AppBackendBasic.Infrastructure.Services.AnalyticsService>();
builder.Services.AddHttpClient();
// Configure Anti-Forgery tokens
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    // Allow HTTP in development for Hangfire dashboard
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// CORS: explicit allow-list from configuration (Cors:AllowedOrigins). Local dev ports are always allowed in Development.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
// Always allowed (appsettings.json is not checked in, so these defaults must live in code): the production site and
// the local dev servers, which are pointed at the production API for debugging.
corsOrigins = corsOrigins.Concat(new[]
    {
        "https://captaintitle.netlify.app",
        "http://localhost:3000", "http://localhost:3001", "http://localhost:5173", "http://localhost:5174",
        "http://127.0.0.1:3000", "http://127.0.0.1:5173"
    })
    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
builder.Services.AddCors(options =>
{
    // Single policy covering all environments - avoids env-detection bugs on shared hosts
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Pagination", "X-Total-Count", "Content-Range", "Accept-Ranges", "Content-Disposition")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Captain Title API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed admin user, system settings, and onboarding data on startup
// using (var scope = app.Services.CreateScope())
// {
//     var adminSeedService = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
//     await adminSeedService.SeedAdminUserAsync();
    
//     var systemSettingsService = scope.ServiceProvider.GetRequiredService<SystemSettingsService>();
//     await systemSettingsService.SeedDefaultSettingsAsync();
    
//     var onboardingSeedService = scope.ServiceProvider.GetRequiredService<OnboardingSeedService>();
//     await onboardingSeedService.SeedOnboardingDataAsync();
// }

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    
//    // Hangfire Dashboard (only in development for security)
//    // TODO: Ensure all recurring jobs are properly configured in the job initialization section below before enabling dashboard in production
//    // Dashboard provides visibility into job execution history, failures, and scheduling
//    // Hangfire Dashboard
//    app.UseHangfireDashboard("/hangfire", new DashboardOptions
//    {
//        Authorization = new[] { new HangfireAuthorizationFilter() },
//        IsReadOnlyFunc = (DashboardContext context) => !app.Environment.IsDevelopment()
//    });
}

    app.UseSwagger();
    app.UseSwaggerUI();
// Recurring Hangfire jobs: none are registered by default (the AI-SEO jobs were removed).
// Add jobs here with IRecurringJobManager.AddOrUpdate<T>(...) when needed.

// CORS - MUST be first so preflight OPTIONS requests get CORS headers before
// any other middleware can redirect or reject them
app.UseCors("CorsPolicy");

app.UseHttpsEnforcement();

// Disabled: HttpsEnforcementMiddleware already handles HTTPS redirection
// app.UseHttpsRedirection();

// Use request/response logging middleware (early in pipeline to capture all requests)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Diagnostic logging for DbContext tracking issues
app.UseMiddleware<DiagnosticLoggingMiddleware>();

// Response Compression (early in pipeline)
app.UseResponseCompression();

// Use security headers middleware (after CORS)
app.UseSecurityHeaders();

// Advanced rate limiting (includes login brute force protection)
app.UseAdvancedRateLimiting();

// Use IP rate limiting middleware (before authentication)
app.UseIpRateLimiting();

// Use authentication middleware (built-in ASP.NET Core)
app.UseAuthentication();

// Use session timeout middleware (after authentication)
app.UseSessionTimeout();

// Use quota enforcement middleware (after authentication, before authorization)
app.UseQuotaEnforcement();

// Use authorization middleware
app.UseAuthorization();
app.MapControllers();

// Basic health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    });
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program class accessible to integration tests
public partial class Program { }
