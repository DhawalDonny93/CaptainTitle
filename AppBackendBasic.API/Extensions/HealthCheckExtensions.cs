using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace AppBackendBasic.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddComprehensiveHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "postgresql", "ready" },
                timeout: TimeSpan.FromSeconds(3));
        }

        // Redis health check (if enabled)
        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled");
        if (redisEnabled)
        {
            var redisConnectionString = configuration["Redis:ConnectionString"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                healthChecksBuilder.AddRedis(
                    redisConnectionString,
                    name: "redis",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "cache", "redis", "ready" },
                    timeout: TimeSpan.FromSeconds(3));
            }
        }

        // External API health checks
        var supabaseUrl = configuration["Supabase:Url"];
        if (!string.IsNullOrEmpty(supabaseUrl))
        {
            healthChecksBuilder.AddUrlGroup(
                new Uri(supabaseUrl),
                name: "supabase",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "supabase" },
                timeout: TimeSpan.FromSeconds(5));
        }

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Detailed health check with full report
        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.TotalMilliseconds,
                        description = entry.Value.Description,
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data
                    })
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
            }
        });

        // Kubernetes readiness probe
        endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString()
                }));
            }
        });

        // Kubernetes liveness probe
        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // No checks, just returns 200 if app is running
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow
                }));
            }
        });

        return endpoints;
    }
}
