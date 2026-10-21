using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Infrastructure.Repositories;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ContentArticlesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContentArticlesController> _logger;
    private readonly IUserTeamMappingRepository _userTeamMappingRepository;

    public ContentArticlesController(ApplicationDbContext context, ILogger<ContentArticlesController> logger, IUserTeamMappingRepository userTeamMappingRepository)
    {
        _context = context;
        _logger = logger;
        _userTeamMappingRepository = userTeamMappingRepository;
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
            return (0, 0);
        }

        var isInTeam = await _userTeamMappingRepository.IsUserInTeamAsync(userId, teamId);
        return isInTeam ? (userId, teamId) : (0, 0);
    }

    // GET: api/ContentArticles/Project/5
    [HttpGet("Project/{projectId}")]
    public async Task<ActionResult<IEnumerable<ContentArticle>>> GetArticlesByProject(int projectId)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
        {
            return Unauthorized(new { message = "Invalid user or team" });
        }

        // Verify project belongs to team
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.TeamId != teamId)
        {
            return NotFound(new { message = "Project not found or unauthorized" });
        }

        return await _context.ContentArticles
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // GET: api/ContentArticles/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ContentArticle>> GetContentArticle(int id)
    {
        var article = await _context.ContentArticles
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        // Check if user owns the project
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (article.Project.UserId != userId)
        {
            return Forbid();
        }

        return article;
    }

    // POST: api/ContentArticles
    [HttpPost]
    public async Task<ActionResult<ContentArticle>> PostContentArticle([FromBody] AppBackendBasic.API.DTOs.CreateContentArticleDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for content article creation: {Errors}", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        if (dto == null)
        {
            return BadRequest(new { message = "Article data is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Title is required" });
        }

        // Verify user owns the project
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var project = await _context.Projects.FindAsync(dto.ProjectId);
        if (project == null || project.UserId != userId)
        {
            return Forbid();
        }

        // Map DTO to entity
        var article = new ContentArticle
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            Slug = dto.Slug,
            ContentHtml = dto.ContentHtml,
            ContentMarkdown = dto.ContentMarkdown,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            FeaturedImageUrl = dto.FeaturedImageUrl,
            Status = dto.Status,
            ScheduledPublishAt = dto.ScheduledPublishAt,
            WordCount = dto.WordCount,
            AiModelUsed = dto.AiModelUsed,
            GenerationPrompt = dto.GenerationPrompt,
            SeoScore = dto.SeoScore,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.ContentArticles.Add(article);
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Content article created successfully with ID {ArticleId} for project {ProjectId}", 
                article.Id, article.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save content article for project {ProjectId}", dto.ProjectId);
            return BadRequest(new { message = "Failed to save article", error = ex.Message });
        }

        return CreatedAtAction(nameof(GetContentArticle), new { id = article.Id }, article);
    }

    // PUT: api/ContentArticles/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContentArticle(int id, ContentArticle article)
    {
        if (id != article.Id)
        {
            return BadRequest();
        }

        // Verify user owns the project
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var existingArticle = await _context.ContentArticles
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (existingArticle == null)
        {
            return NotFound();
        }

        if (existingArticle.Project.UserId != userId)
        {
            return Forbid();
        }

        existingArticle.Title = article.Title;
        existingArticle.ContentHtml = article.ContentHtml;
        existingArticle.ContentMarkdown = article.ContentMarkdown;
        existingArticle.Status = article.Status;
        existingArticle.MetaTitle = article.MetaTitle;
        existingArticle.MetaDescription = article.MetaDescription;
        existingArticle.FeaturedImageUrl = article.FeaturedImageUrl;
        existingArticle.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ContentArticleExists(id))
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

    // DELETE: api/ContentArticles/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContentArticle(int id)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
        {
            return Unauthorized();
        }

        var article = await _context.ContentArticles
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        if (article.Project.TeamId != teamId)
        {
            return Forbid();
        }

        _context.ContentArticles.Remove(article);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/ContentArticles/Project/{projectId}/CreationData
    [HttpGet("Project/{projectId}/CreationData")]
    public async Task<ActionResult<ContentCreationDataDto>> GetContentCreationData(int projectId)
    {
        var (userId, teamId) = await GetUserAndTeamAsync();
        if (teamId == 0)
        {
            return Unauthorized(new { message = "Invalid user or team" });
        }

        // Verify project belongs to team
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.TeamId != teamId)
        {
            return NotFound(new { message = "Project not found or unauthorized" });
        }

        // Fetch all data in parallel for better performance
        var topicsTask = _context.PromptResearchDatas
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.RelevanceScore)
            .Take(50)
            .Select(p => new TopicOptionDto
            {
                Id = p.Id,
                Topic = p.PromptText,
                Description = p.Category,
                SearchVolume = p.RelevanceScore ?? 0
            })
            .ToListAsync();

        var promptsTask = _context.PromptTrackings
            .Where(p => p.ProjectId == projectId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new PromptOptionDto
            {
                Id = p.Id,
                PromptText = p.PromptText,
                Category = p.Topic,
                Priority = 0
            })
            .ToListAsync();

        var citationsTask = _context.Citations
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CitationCount)
            .Take(20)
            .Select(c => new CitationOptionDto
            {
                Id = c.Id,
                Url = c.SourceUrl,
                Title = c.SourceTitle,
                CitationCount = c.CitationCount,
                Domain = c.SourceDomain
            })
            .ToListAsync();

        var brandKitsTask = _context.BrandProfiles
            .Where(b => b.ProjectId == projectId)
            .Select(b => new BrandKitOptionDto
            {
                Id = b.Id,
                Name = b.Overview ?? "Brand Profile",
                Description = b.Description
            })
            .ToListAsync();

        var audienceSegmentsTask = _context.Projects
            .Where(p => p.TeamId == teamId && !string.IsNullOrEmpty(p.TargetAudience))
            .Select(p => p.TargetAudience!)
            .Distinct()
            .ToListAsync();

        await Task.WhenAll(topicsTask, promptsTask, citationsTask, brandKitsTask, audienceSegmentsTask);

        var audienceSegments = audienceSegmentsTask.Result
            .Select((name, index) => new AudienceSegmentOptionDto
            {
                Id = index + 1,
                Name = name,
                Description = null
            })
            .ToList();

        var result = new ContentCreationDataDto
        {
            Topics = topicsTask.Result,
            Prompts = promptsTask.Result,
            Citations = citationsTask.Result,
            BrandKits = brandKitsTask.Result,
            AudienceSegments = audienceSegments
        };

        return Ok(result);
    }

    private bool ContentArticleExists(int id)
    {
        return _context.ContentArticles.Any(e => e.Id == id);
    }
}
