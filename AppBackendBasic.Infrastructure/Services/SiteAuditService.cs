using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

namespace AppBackendBasic.Infrastructure.Services
{
    public class SiteAuditService : ISiteAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SiteAuditService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IUserTaskService _taskService;

        public SiteAuditService(
            ApplicationDbContext context, 
            ILogger<SiteAuditService> logger, 
            IHttpClientFactory httpClientFactory,
            IUserTaskService taskService)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _taskService = taskService;
        }

        public async Task RunAllProjectAuditsAsync()
        {
             _logger.LogInformation("Starting All Projects Site Audit job at {Time}", DateTime.UtcNow);
             var projects = await _context.Projects.ToListAsync();
             foreach(var project in projects)
             {
                 await RunFullAuditAsync(project.Id);
             }
             _logger.LogInformation("Completed All Projects Site Audit job.");
        }

        public async Task RunFullAuditAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || string.IsNullOrEmpty(project.Domain)) return;

            string domain = project.Domain;
            if (!domain.StartsWith("http")) domain = "https://" + domain;

            // Check 1: llms.txt
            await CheckLlmsTxt(project.Id, domain);

            // Check 2: Schema (Mock)
            await CheckSchema(project.Id, domain);

             // Check 3: DOM Clarity (Mock)
            await CheckDomClarity(project.Id, domain);

            await _context.SaveChangesAsync();

            // Auto-generate tasks from failures
            await _taskService.GenerateTasksFromAuditsAsync(projectId);
        }

        private async Task CheckLlmsTxt(int projectId, string baseUrl)
        {
            var audit = new SiteAudit
            {
                ProjectId = projectId,
                AuditDate = DateTime.UtcNow,
                OverallScore = 0,
                IssuesFound = 0,
                ReportUrl = null
            };

            try
            {
                var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/llms.txt");
                if (response.IsSuccessStatusCode)
                {
                    audit.OverallScore = 100;
                    audit.IssuesFound = 0;
                    audit.ReportUrl = "llms.txt file found and accessible.";
                }
                else
                {
                    audit.OverallScore = 0;
                    audit.IssuesFound = 1;
                    audit.ReportUrl = $"llms.txt not found (Status: {response.StatusCode}).";
                }
            }
            catch (Exception ex)
            {
                audit.OverallScore = 0;
                audit.IssuesFound = 1;
                audit.ReportUrl = $"Error accessing llms.txt: {ex.Message}";
            }

            _context.SiteAudits.Add(audit);
        }

        private async Task CheckSchema(int projectId, string baseUrl)
        {
            // Mock Check
             var audit = new SiteAudit
            {
                ProjectId = projectId,
                AuditDate = DateTime.UtcNow,
                OverallScore = 50,
                IssuesFound = 2,
                ReportUrl = "Found basic Schema.org markup, but missing 'Product' or 'Organization' specific entities."
            };
             _context.SiteAudits.Add(audit);
             await Task.CompletedTask;
        }

        private async Task CheckDomClarity(int projectId, string baseUrl)
        {
            // Mock Check
             var audit = new SiteAudit
            {
                ProjectId = projectId,
                AuditDate = DateTime.UtcNow,
                OverallScore = 90,
                IssuesFound = 0,
                ReportUrl = "Semantic HTML structure is good. Main content is easily parsing by LLMs."
            };
             _context.SiteAudits.Add(audit);
             await Task.CompletedTask;
        }
    }
}
