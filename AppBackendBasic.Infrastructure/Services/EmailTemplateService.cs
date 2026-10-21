using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Infrastructure.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            ApplicationDbContext context,
            ILogger<EmailTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EmailTemplate>> GetAllTemplatesAsync()
        {
            return await _context.EmailTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<EmailTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _context.EmailTemplates.FindAsync(id);
        }

        public async Task<EmailTemplate?> GetTemplateByTypeAsync(string templateType)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateKey == templateType && t.IsActive);
        }

        public async Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateDto dto)
        {
            var template = new EmailTemplate
            {
                TemplateKey = dto.TemplateKey,
                Name = dto.Name,
                Subject = dto.Subject,
                HtmlBody = dto.HtmlBody,
                TextBody = dto.TextBody,
                Variables = dto.Variables != null ? string.Join(",", dto.Variables) : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template created: {TemplateName}", template.Name);
            return template;
        }

        public async Task<EmailTemplate> UpdateTemplateAsync(int id, UpdateEmailTemplateDto dto)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
                throw new Exception($"Email template with ID {id} not found");

            if (!string.IsNullOrEmpty(dto.TemplateName))
                template.Name = dto.TemplateName;
            if (!string.IsNullOrEmpty(dto.TemplateType))
                template.TemplateKey = dto.TemplateType;
            if (!string.IsNullOrEmpty(dto.Subject))
                template.Subject = dto.Subject;
            if (!string.IsNullOrEmpty(dto.HtmlBody))
                template.HtmlBody = dto.HtmlBody;
            if (dto.TextBody != null)
                template.TextBody = dto.TextBody;
            if (dto.Variables != null)
                template.Variables = dto.Variables;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template updated: {TemplateName}", template.Name);
            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
                return false;

            _context.EmailTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email template deleted: {TemplateName}", template.Name);
            return true;
        }

        public async Task<string> RenderTemplateAsync(int id, Dictionary<string, string> variables)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
                throw new Exception($"Email template with ID {id} not found");

            var renderedBody = template.HtmlBody;
            foreach (var variable in variables)
            {
                renderedBody = renderedBody.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            return renderedBody;
        }

        public async Task<bool> SendTestEmailAsync(int id, string recipientEmail)
        {
            var template = await _context.EmailTemplates.FindAsync(id);
            if (template == null)
                return false;

            // TODO: Replace placeholder logging with actual email service integration (SendGrid, AWS SES, or similar)
            // Requirements: Configure API credentials in appsettings.json, implement IEmailProvider interface, add sender email validation
            // Test method should send actual test email and verify delivery with configurable recipient
            _logger.LogInformation("Test email would be sent to {Email} using template {TemplateName}", 
                recipientEmail, template.Name);

            return true;
        }
    }
}
