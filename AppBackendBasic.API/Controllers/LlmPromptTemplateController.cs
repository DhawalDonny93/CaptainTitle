using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LlmPromptTemplateController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LlmPromptTemplateController> _logger;

    public LlmPromptTemplateController(ApplicationDbContext context, ILogger<LlmPromptTemplateController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active prompt templates (for frontend to display options)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] string? category = null)
    {
        var query = _context.Set<LlmPromptTemplate>()
            .Where(t => t.IsActive);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        var templates = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .Select(t => new LlmPromptTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category,
                RequiresImage = t.RequiresImage,
                IsActive = t.IsActive
            })
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Get template by ID (Admin only - includes full details)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTemplate(int id)
    {
        var template = await _context.Set<LlmPromptTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound(new { message = "Template not found." });
        }

        return Ok(template);
    }

    /// <summary>
    /// Create new prompt template (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreatePromptTemplateRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var template = new LlmPromptTemplate
        {
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            UserPromptTemplate = request.UserPromptTemplate,
            ResponseFormat = request.ResponseFormat,
            ResponseSchema = request.ResponseSchema,
            Model = request.Model,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            RequiresImage = request.RequiresImage,
            Category = request.Category,
            CreatedByUserId = userId,
            IsActive = true
        };

        _context.Set<LlmPromptTemplate>().Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Prompt template created: {TemplateId} by user {UserId}", template.Id, userId);

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    /// <summary>
    /// Update prompt template (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdatePromptTemplateRequest request)
    {
        var template = await _context.Set<LlmPromptTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound(new { message = "Template not found." });
        }

        template.Name = request.Name;
        template.Description = request.Description;
        template.SystemPrompt = request.SystemPrompt;
        template.UserPromptTemplate = request.UserPromptTemplate;
        template.ResponseFormat = request.ResponseFormat;
        template.ResponseSchema = request.ResponseSchema;
        template.Model = request.Model;
        template.Temperature = request.Temperature;
        template.MaxTokens = request.MaxTokens;
        template.RequiresImage = request.RequiresImage;
        template.Category = request.Category;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Prompt template updated: {TemplateId}", template.Id);

        return Ok(template);
    }

    /// <summary>
    /// Delete (deactivate) prompt template (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.Set<LlmPromptTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound(new { message = "Template not found." });
        }

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Prompt template deactivated: {TemplateId}", template.Id);

        return Ok(new { message = "Template deactivated successfully." });
    }

    /// <summary>
    /// Get template categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Set<LlmPromptTemplate>()
            .Where(t => t.IsActive)
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
