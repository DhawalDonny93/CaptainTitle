using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Repositories
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(int id);
        Task<Team?> GetByNameAsync(string name);
        Task<IEnumerable<Team>> GetAllAsync();
        Task<IEnumerable<Team>> GetActiveTeamsAsync();
        Task<Team> CreateAsync(Team team);
        Task<Team> UpdateAsync(Team team);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class TeamRepository : ITeamRepository
    {
        private readonly ApplicationDbContext _context;

        public TeamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Team?> GetByIdAsync(int id)
        {
            return await _context.Teams
                .Include(t => t.UserMappings)
                .Include(t => t.Projects)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Team?> GetByNameAsync(string name)
        {
            return await _context.Teams
                .Include(t => t.UserMappings)
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<IEnumerable<Team>> GetAllAsync()
        {
            return await _context.Teams
                .Include(t => t.UserMappings)
                .ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetActiveTeamsAsync()
        {
            return await _context.Teams
                .Where(t => t.IsActive)
                .Include(t => t.UserMappings)
                .ToListAsync();
        }

        public async Task<Team> CreateAsync(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<Team> UpdateAsync(Team team)
        {
            team.UpdatedAt = DateTime.UtcNow;
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task DeleteAsync(int id)
        {
            var team = await GetByIdAsync(id);
            if (team != null)
            {
                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Teams.AnyAsync(t => t.Id == id);
        }
    }
}
