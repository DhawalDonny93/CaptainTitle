using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppBackendBasic.API.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Adds database context with connection resilience and retry policies
    /// </summary>
    public static IServiceCollection AddDatabaseWithResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Enable automatic retry on transient failures
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: new[]
                    {
                        "57P03", // cannot_connect_now
                        "58000", // system_error
                        "58030", // io_error
                        "40001", // serialization_failure
                        "40P01"  // deadlock_detected
                    });

                // Set command timeout
                npgsqlOptions.CommandTimeout(30);

                // Enable query splitting for better performance
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                // Migration assembly
                npgsqlOptions.MigrationsAssembly("AppBackendBasic.Infrastructure");
            });

            // Enable sensitive data logging in development only
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Configures connection pooling for PostgreSQL
    /// </summary>
    public static IServiceCollection ConfigureConnectionPooling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                // Connection pooling settings
                Pooling = true,
                MinPoolSize = configuration.GetValue<int>("Database:MinPoolSize", 5),
                MaxPoolSize = configuration.GetValue<int>("Database:MaxPoolSize", 100),
                ConnectionIdleLifetime = configuration.GetValue<int>("Database:ConnectionIdleLifetime", 300),
                ConnectionPruningInterval = configuration.GetValue<int>("Database:ConnectionPruningInterval", 10),
                
                // Timeout settings
                Timeout = configuration.GetValue<int>("Database:ConnectionTimeout", 30),
                CommandTimeout = configuration.GetValue<int>("Database:CommandTimeout", 30),
                
                // Performance settings
                MaxAutoPrepare = 20,
                AutoPrepareMinUsages = 2,
                
                // Reliability
                KeepAlive = 30,
                TcpKeepAlive = true,
                TcpKeepAliveInterval = 10
            };

            // Update configuration with optimized connection string
            configuration["ConnectionStrings:DefaultConnection"] = builder.ToString();
        }

        return services;
    }

    /// <summary>
    /// Adds database health checks with detailed diagnostics
    /// </summary>
    public static IHealthChecksBuilder AddDatabaseHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.AddNpgSql(
            connectionString!,
            name: "database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "postgresql", "ready" },
            timeout: TimeSpan.FromSeconds(3));

        return builder;
    }
}
