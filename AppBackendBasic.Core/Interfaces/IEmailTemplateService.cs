using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<List<EmailTemplate>> GetAllTemplatesAsync();
        Task<EmailTemplate?> GetTemplateByIdAsync(int id);
        Task<EmailTemplate?> GetTemplateByTypeAsync(string templateType);
        Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateDto dto);
        Task<EmailTemplate> UpdateTemplateAsync(int id, UpdateEmailTemplateDto dto);
        Task<bool> DeleteTemplateAsync(int id);
        Task<string> RenderTemplateAsync(int id, Dictionary<string, string> variables);
        Task<bool> SendTestEmailAsync(int id, string recipientEmail);
    }
}
