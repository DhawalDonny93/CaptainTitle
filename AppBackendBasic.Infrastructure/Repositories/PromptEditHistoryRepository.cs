using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;

namespace AppBackendBasic.Infrastructure.Repositories
{
    public interface IPromptEditHistoryRepository
    {
        Task<PromptEditHistory?> GetByIdAsync(int id);
        Task<IEnumerable<PromptEditHistory>> GetByPromptIdAsync(int promptTrackingId);
        Task<IEnumerable<PromptEditHistory>> GetByUserIdAsync(int userId);
        Task<IEnumerable<PromptEditHistory>> GetByPromptIdAndDateRangeAsync(int promptTrackingId, DateTime startDate, DateTime endDate);
        Task<PromptEditHistory> RecordChangeAsync(int promptTrackingId, int userId, string fieldName, string? oldValue, string? newValue, string changeType, string? reason = null, string? ipAddress = null);
        Task<IEnumerable<PromptEditHistory>> GetPromptCreationHistoryAsync(int promptTrackingId);
        Task<PromptEditHistory?> GetLastChangeForFieldAsync(int promptTrackingId, string fieldName);
    }

    public class PromptEditHistoryRepository : IPromptEditHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public PromptEditHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PromptEditHistory?> GetByIdAsync(int id)
        {
            return await _context.PromptEditHistories
                .Include(p => p.PromptTracking)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<PromptEditHistory>> GetByPromptIdAsync(int promptTrackingId)
        {
            return await _context.PromptEditHistories
                .Where(p => p.PromptTrackingId == promptTrackingId)
                .Include(p => p.User)
                .OrderByDescending(p => p.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PromptEditHistory>> GetByUserIdAsync(int userId)
        {
            return await _context.PromptEditHistories
                .Where(p => p.UserId == userId)
                .Include(p => p.PromptTracking)
                .OrderByDescending(p => p.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PromptEditHistory>> GetByPromptIdAndDateRangeAsync(int promptTrackingId, DateTime startDate, DateTime endDate)
        {
            return await _context.PromptEditHistories
                .Where(p => p.PromptTrackingId == promptTrackingId && p.ChangedAt >= startDate && p.ChangedAt <= endDate)
                .Include(p => p.User)
                .OrderByDescending(p => p.ChangedAt)
                .ToListAsync();
        }

        public async Task<PromptEditHistory> RecordChangeAsync(
            int promptTrackingId,
            int userId,
            string fieldName,
            string? oldValue,
            string? newValue,
            string changeType,
            string? reason = null,
            string? ipAddress = null)
        {
            var history = new PromptEditHistory
            {
                PromptTrackingId = promptTrackingId,
                UserId = userId,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeType = changeType,
                Reason = reason,
                IpAddress = ipAddress,
                ChangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.PromptEditHistories.Add(history);
            await _context.SaveChangesAsync();
            return history;
        }

        public async Task<IEnumerable<PromptEditHistory>> GetPromptCreationHistoryAsync(int promptTrackingId)
        {
            return await _context.PromptEditHistories
                .Where(p => p.PromptTrackingId == promptTrackingId && p.ChangeType == "Created")
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<PromptEditHistory?> GetLastChangeForFieldAsync(int promptTrackingId, string fieldName)
        {
            return await _context.PromptEditHistories
                .Where(p => p.PromptTrackingId == promptTrackingId && p.FieldName == fieldName)
                .Include(p => p.User)
                .OrderByDescending(p => p.ChangedAt)
                .FirstOrDefaultAsync();
        }
    }
}
