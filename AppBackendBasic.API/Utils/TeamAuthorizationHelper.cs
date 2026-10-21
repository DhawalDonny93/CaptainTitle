using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AppBackendBasic.Infrastructure.Repositories;

namespace AppBackendBasic.API.Utils
{
    /// <summary>
    /// Helper class for team-based authorization and claim extraction
    /// </summary>
    public static class TeamAuthorizationHelper
    {
        /// <summary>
        /// Extract UserId from JWT claims
        /// </summary>
        public static int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            if (user == null) return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")
                ?? user.FindFirst("user_id")
                ?? user.FindFirst("UserId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Extract TeamId from JWT claims (if present)
        /// Falls back to getting the user's primary team if not in claims
        /// </summary>
        public static int? GetTeamIdFromClaims(ClaimsPrincipal user)
        {
            if (user == null) return null;

            // First, try to get TeamId directly from claims
            var teamIdClaim = user.FindFirst("team_id")
                ?? user.FindFirst("TeamId");

            if (teamIdClaim != null && int.TryParse(teamIdClaim.Value, out var teamId))
            {
                return teamId;
            }

            // If not in claims, return null - the controller/service will fetch from DB
            return null;
        }

        /// <summary>
        /// Get user's email from claims (useful for audit logging)
        /// </summary>
        public static string? GetEmailFromClaims(ClaimsPrincipal user)
        {
            if (user == null) return null;
            return user.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Get user's IP address for audit trail
        /// </summary>
        public static string? GetIpAddressFromContext(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            if (httpContext == null) return null;

            // Try to get IP from various sources
            var ip = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrEmpty(ip))
            {
                ip = httpContext.Request.Headers["X-Real-IP"].ToString();
            }
            if (string.IsNullOrEmpty(ip))
            {
                ip = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            return ip;
        }

        /// <summary>
        /// Verify that a user belongs to a team
        /// </summary>
        public static async Task<bool> IsUserInTeamAsync(
            int userId,
            int teamId,
            IUserTeamMappingRepository userTeamMappingRepository)
        {
            if (userTeamMappingRepository == null)
            {
                throw new ArgumentNullException(nameof(userTeamMappingRepository));
            }

            return await userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
        }

        /// <summary>
        /// Check if user is an admin of a team
        /// </summary>
        public static async Task<bool> IsTeamAdminAsync(
            int userId,
            int teamId,
            IUserTeamMappingRepository userTeamMappingRepository)
        {
            if (userTeamMappingRepository == null)
            {
                throw new ArgumentNullException(nameof(userTeamMappingRepository));
            }

            return await userTeamMappingRepository.IsTeamAdminAsync(userId, teamId);
        }

        /// <summary>
        /// Get user's primary team (oldest/first team they joined)
        /// </summary>
        public static async Task<int?> GetUserPrimaryTeamAsync(
            int userId,
            IUserTeamMappingRepository userTeamMappingRepository)
        {
            if (userTeamMappingRepository == null)
            {
                throw new ArgumentNullException(nameof(userTeamMappingRepository));
            }

            return await userTeamMappingRepository.GetPrimaryTeamIdAsync(userId);
        }

        /// <summary>
        /// Validate both UserId and TeamId from claims are present
        /// </summary>
        public static (int? userId, int? teamId) GetUserAndTeamFromClaims(ClaimsPrincipal user)
        {
            return (GetUserIdFromClaims(user), GetTeamIdFromClaims(user));
        }

        /// <summary>
        /// Throw UnauthorizedAccessException with consistent message
        /// </summary>
        public static void ThrowUnauthorized(string reason = "User does not have access to this resource")
        {
            throw new UnauthorizedAccessException(reason);
        }

        /// <summary>
        /// Throw FormatException for missing claims
        /// </summary>
        public static void ThrowMissingClaim(string claimName)
        {
            throw new FormatException($"Required claim '{claimName}' not found in token");
        }
    }
}
