using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Hangfire;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiTrackingPromptsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AiTrackingPromptsController> _logger;

    public AiTrackingPromptsController(ApplicationDbContext context, ILogger<AiTrackingPromptsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/AiTrackingPrompts/Project/5
    [HttpGet("Project/{projectId}")]
    public async Task<ActionResult<IEnumerable<PromptTracking>>> GetPromptsByProject(int projectId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.UserId != userInt)
        {
            return NotFound(new { message = "Project not found or unauthorized" });
        }

        return await _context.PromptTrackings
            .Where(p => p.ProjectId == projectId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.AiTrackingResults)
            .ToListAsync();
    }

    // GET: api/AiTrackingPrompts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PromptTracking>> GetAiTrackingPrompt(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var prompt = await _context.PromptTrackings
            .Include(p => p.Project)
            .Include(p => p.AiTrackingResults)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prompt == null)
        {
            return NotFound();
        }

        if (prompt.Project.UserId != userId)
        {
            return Forbid();
        }

        return prompt;
    }

    // POST: api/AiTrackingPrompts
    [HttpPost]
    public async Task<ActionResult<PromptTracking>> CreateAiTrackingPrompt(PromptTracking prompt)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var project = await _context.Projects.FindAsync(prompt.ProjectId);
        if (project == null || project.UserId != userId)
        {
            return BadRequest(new { message = "Invalid Project ID or unauthorized" });
        }

        prompt.Id = 0; // Let database generate ID
        prompt.CreatedAt = DateTime.UtcNow;
        prompt.UpdatedAt = DateTime.UtcNow;
        prompt.IsActive = true;
        
        // Set default values if not provided
        prompt.Status ??= "Pending";
        prompt.TrackingFrequency ??= "Daily";

        _context.PromptTrackings.Add(prompt);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAiTrackingPrompt", new { id = prompt.Id }, prompt);
    }

    // DELETE: api/AiTrackingPrompts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAiTrackingPrompt(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var prompt = await _context.PromptTrackings
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prompt == null)
        {
            return NotFound();
        }

        if (prompt.Project.UserId != userId)
        {
            return Forbid();
        }

        // Hard delete or Soft delete? Let's do Soft Delete
        prompt.IsActive = false;
        prompt.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // POST: api/AiTrackingPrompts/5/check-visibility
    [HttpPost("{id}/check-visibility")]
    public async Task<IActionResult> CheckVisibility(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var prompt = await _context.PromptTrackings
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prompt == null)
        {
            return NotFound();
        }

        if (prompt.Project.UserId != userId)
        {
            return Forbid();
        }

        // Enqueue background job to check visibility
        var jobId = BackgroundJob.Enqueue<IPromptTrackingService>(
            service => service.CheckPromptVisibilityAsync(userId, id));

        // Update status to indicate check is scheduled
        prompt.Status = "Scheduled";
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Visibility check initiated", 
            promptId = id,
            jobId = jobId 
        });
    }
}
