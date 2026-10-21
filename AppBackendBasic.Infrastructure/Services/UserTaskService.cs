using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class UserTaskService : IUserTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserTaskService> _logger;

        public UserTaskService(ApplicationDbContext context, ILogger<UserTaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateTaskAsync(int projectId, string title, string description, string priority)
        {
            var task = new UserTask
            {
                ProjectId = projectId,
                Title = title,
                Description = description,
                Priority = priority,
                Source = "SystemGenerated",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserTasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task GenerateTasksFromAuditsAsync(int projectId)
        {
            var pendingAudits = await _context.SiteAudits
                .Where(a => a.ProjectId == projectId && a.OverallScore < 50)
                .ToListAsync();

            foreach (var audit in pendingAudits)
            {
                // check if task already exists
                bool exists = await _context.UserTasks.AnyAsync(t => t.ProjectId == projectId && t.Title.Contains("Site Audit") && !t.IsCompleted);
                
                if (!exists)
                {
                    string title = $"Review Site Audit Issues";
                    string desc = $"Site audit completed with score {audit.OverallScore}. Found {audit.IssuesFound} issues. Review: {audit.ReportUrl}";
                    string priority = audit.OverallScore < 30 ? "High" : "Medium";
                    
                    await CreateTaskAsync(projectId, title, desc, priority);
                    _logger.LogInformation("Generated task for failed audit");
                }
            }
        }
    }
}
