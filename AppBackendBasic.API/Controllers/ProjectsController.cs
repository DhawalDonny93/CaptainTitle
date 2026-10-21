using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserTeamMappingRepository _userTeamMappingRepository;
    private readonly IWebSubscriptionService _subscriptionService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        ApplicationDbContext context,
        IUserTeamMappingRepository userTeamMappingRepository,
        IWebSubscriptionService subscriptionService,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _userTeamMappingRepository = userTeamMappingRepository;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Extracts and validates user and team IDs from JWT claims
    /// </summary>
    private async Task<(int userId, int teamId)> GetUserAndTeamAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var teamIdClaim = User.FindFirst("team_id")?.Value;

        if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(teamIdClaim, out var teamId))
        {
            _logger.LogWarning("Invalid user or team ID in claims");
            return (0, 0);
        }

        var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
        if (!isInTeam)
        {
            _logger.LogWarning("User {UserId} is not in team {TeamId}", userId, teamId);
            return (0, 0);
        }

        return (userId, teamId);
    }

    // GET: api/Projects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
            return Forbid();

        return await _context.Projects
            .Where(p => p.TeamId == teamId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    // GET: api/Projects/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
            return Forbid();

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TeamId == teamId);

        if (project == null)
        {
            return NotFound();
        }

        return project;
    }

    // POST: api/Projects
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
            return Forbid();

        // Enforce subscription project limit
        try
        {
            var plan = await _subscriptionService.GetUserSubscriptionPlanAsync(userId);
            if (plan != null && plan.MaxProjects != -1)
            {
                var currentCount = await _context.Projects
                    .CountAsync(p => p.TeamId == teamId && p.IsActive);

                if (currentCount >= plan.MaxProjects)
                {
                    return StatusCode(402, new
                    {
                        message = $"You have reached the maximum of {plan.MaxProjects} project(s) on your {plan.DisplayName} plan. Please upgrade to create more projects.",
                        code = "QUOTA_EXCEEDED",
                        limit = plan.MaxProjects,
                        used = currentCount
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check project quota for user {UserId}, proceeding", userId);
        }

        project.UserId = userId;
        project.TeamId = teamId;
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        project.IsActive = true;

        // Set default values for feature flags if not provided
        project.EnableAutoCrawl ??= false;
        project.CrawlMaxPages ??= 100;
        project.CrawlMaxDepth ??= 3;
        project.EnableOpportunityDetection ??= true;
        project.EnableVisibilityAlerts ??= true;
        project.VisibilityCheckFrequency ??= "daily";

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetProject", new { id = project.Id }, project);
    }

    // PUT: api/Projects/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, Project project)
    {
        if (id != project.Id)
        {
            return BadRequest();
        }

        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
            return Forbid();

        var existingProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TeamId == teamId);
        if (existingProject == null)
        {
            return NotFound();
        }

        existingProject.Name = project.Name;
        existingProject.Domain = project.Domain;
        existingProject.BrandVoice = project.BrandVoice;
        existingProject.TargetAudience = project.TargetAudience;
        existingProject.CmsCredentials = project.CmsCredentials;
        existingProject.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProjectExists(id, teamId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Projects/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
            return Forbid();

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TeamId == teamId);
        if (project == null)
        {
            return NotFound();
        }

        // Soft delete
        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProjectExists(int id, int teamId)
    {
        return _context.Projects.Any(e => e.Id == id && e.TeamId == teamId);
    }
}
