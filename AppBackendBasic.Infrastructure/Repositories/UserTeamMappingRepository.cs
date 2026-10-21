using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Repositories
{
    public interface IUserTeamMappingRepository
    {
        Task<UserTeamMapping?> GetByIdAsync(int id);
        Task<UserTeamMapping?> GetByUserAndTeamAsync(int userId, int teamId);
        Task<IEnumerable<UserTeamMapping>> GetTeamMembersAsync(int teamId);
        Task<IEnumerable<UserTeamMapping>> GetUserTeamsAsync(int userId);
        Task<IEnumerable<Team>> GetUserTeamsDetailedAsync(int userId);
        Task<int?> GetPrimaryTeamIdAsync(int userId);
        Task<UserTeamMapping> AddUserToTeamAsync(int userId, int teamId, string role = "Member", int? invitedBy = null);
        Task<UserTeamMapping> UpdateAsync(UserTeamMapping mapping);
        Task RemoveUserFromTeamAsync(int userId, int teamId);
        Task<bool> IsUserInTeamAsync(int userId, int teamId);
        Task<int> GetTeamMemberCountAsync(int teamId);
        Task<bool> IsTeamAdminAsync(int userId, int teamId);
    }

    public class UserTeamMappingRepository : IUserTeamMappingRepository
    {
        private readonly ApplicationDbContext _context;

        public UserTeamMappingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserTeamMapping?> GetByIdAsync(int id)
        {
            return await _context.UserTeamMappings
                .Include(m => m.User)
                .Include(m => m.Team)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<UserTeamMapping?> GetByUserAndTeamAsync(int userId, int teamId)
        {
            return await _context.UserTeamMappings
                .Include(m => m.User)
                .Include(m => m.Team)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId && m.IsActive);
        }

        public async Task<IEnumerable<UserTeamMapping>> GetTeamMembersAsync(int teamId)
        {
            return await _context.UserTeamMappings
                .Where(m => m.TeamId == teamId && m.IsActive)
                .Include(m => m.User)
                .OrderByDescending(m => m.Role == "Admin")
                .ThenBy(m => m.JoinedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserTeamMapping>> GetUserTeamsAsync(int userId)
        {
            return await _context.UserTeamMappings
                .Where(m => m.UserId == userId && m.IsActive)
                .Include(m => m.Team)
                .OrderByDescending(m => m.Role == "Admin")
                .ThenByDescending(m => m.JoinedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetUserTeamsDetailedAsync(int userId)
        {
            return await _context.UserTeamMappings
                .Where(m => m.UserId == userId && m.IsActive)
                .Select(m => m.Team!)
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<int?> GetPrimaryTeamIdAsync(int userId)
        {
            // Get the first team the user joined (or the one created for them)
            var mapping = await _context.UserTeamMappings
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderBy(m => m.JoinedAt)
                .FirstOrDefaultAsync();

            return mapping?.TeamId;
        }

        public async Task<UserTeamMapping> AddUserToTeamAsync(int userId, int teamId, string role = "Member", int? invitedBy = null)
        {
            var mapping = new UserTeamMapping
            {
                UserId = userId,
                TeamId = teamId,
                Role = role,
                InvitedBy = invitedBy,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserTeamMappings.Add(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }

        public async Task<UserTeamMapping> UpdateAsync(UserTeamMapping mapping)
        {
            mapping.UpdatedAt = DateTime.UtcNow;
            _context.UserTeamMappings.Update(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }

        public async Task RemoveUserFromTeamAsync(int userId, int teamId)
        {
            var mapping = await GetByUserAndTeamAsync(userId, teamId);
            if (mapping != null)
            {
                mapping.IsActive = false;
                mapping.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(mapping);
            }
        }

        public async Task<bool> IsUserInTeamAsync(int userId, int teamId)
        {
            return await _context.UserTeamMappings
                .AnyAsync(m => m.UserId == userId && m.TeamId == teamId && m.IsActive);
        }

        public async Task<int> GetTeamMemberCountAsync(int teamId)
        {
            return await _context.UserTeamMappings
                .CountAsync(m => m.TeamId == teamId && m.IsActive);
        }

        public async Task<bool> IsTeamAdminAsync(int userId, int teamId)
        {
            return await _context.UserTeamMappings
                .AnyAsync(m => m.UserId == userId && m.TeamId == teamId && m.IsActive && m.Role == "Admin");
        }
    }
}
