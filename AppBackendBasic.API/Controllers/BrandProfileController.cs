using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AppBackendBasic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BrandProfileController : ControllerBase
    {
        private readonly IBrandProfileService _brandProfileService;
        private readonly ICompetitorService _competitorService;
        private readonly ILogger<BrandProfileController> _logger;

        public BrandProfileController(
            IBrandProfileService brandProfileService,
            ICompetitorService competitorService,
            ILogger<BrandProfileController> logger)
        {
            _brandProfileService = brandProfileService;
            _competitorService = competitorService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get brand profile for a project
        /// </summary>
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetBrandProfile(int projectId)
        {
            try
            {
                var brandProfile = await _brandProfileService.GetByProjectIdAsync(projectId);
                
                if (brandProfile == null)
                {
                    return Ok(new
                    {
                        projectId,
                        overview = (string?)null,
                        description = (string?)null,
                        topics = new string[] { },
                        logoUrl = (string?)null
                    });
                }

                return Ok(new
                {
                    id = brandProfile.Id,
                    projectId = brandProfile.ProjectId,
                    overview = brandProfile.Overview,
                    description = brandProfile.Description,
                    topics = string.IsNullOrEmpty(brandProfile.Topics) 
                        ? new string[] { } 
                        : JsonSerializer.Deserialize<string[]>(brandProfile.Topics) ?? new string[] { },
                    logoUrl = brandProfile.LogoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brand profile for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Error retrieving brand profile" });
            }
        }

        /// <summary>
        /// Create or update brand profile
        /// </summary>
        [HttpPost("{projectId}")]
        public async Task<IActionResult> CreateOrUpdateBrandProfile(int projectId, [FromBody] BrandProfileRequest request)
        {
            try
            {
                var topicsJson = request.Topics != null && request.Topics.Length > 0
                    ? JsonSerializer.Serialize(request.Topics)
                    : null;

                var brandProfile = await _brandProfileService.CreateOrUpdateAsync(
                    projectId,
                    request.Overview,
                    request.Description,
                    topicsJson);

                return Ok(new
                {
                    id = brandProfile.Id,
                    projectId = brandProfile.ProjectId,
                    overview = brandProfile.Overview,
                    description = brandProfile.Description,
                    topics = request.Topics ?? new string[] { },
                    logoUrl = brandProfile.LogoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating brand profile for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Error saving brand profile" });
            }
        }

        /// <summary>
        /// Upload logo for brand profile
        /// </summary>
        [HttpPost("{projectId}/logo")]
        public async Task<IActionResult> UploadLogo(int projectId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return BadRequest(new { message = "File size exceeds 5MB limit" });
                }

                using var stream = file.OpenReadStream();
                var logoUrl = await _brandProfileService.UploadLogoAsync(projectId, stream, file.FileName);

                return Ok(new { logoUrl });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Error uploading logo" });
            }
        }

        /// <summary>
        /// Get competitors for a project
        /// </summary>
        [HttpGet("{projectId}/competitors")]
        public async Task<IActionResult> GetCompetitors(int projectId, [FromQuery] string? competitorType = null)
        {
            try
            {
                var competitors = await _competitorService.GetByProjectIdAsync(projectId);
                
                // Filter by competitor type if specified
                if (!string.IsNullOrEmpty(competitorType))
                {
                    competitors = competitors.Where(c => c.CompetitorType.Equals(competitorType, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                return Ok(competitors.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    domain = c.Domain,
                    logoUrl = c.LogoUrl,
                    competitorType = c.CompetitorType,
                    createdAt = c.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competitors for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Error retrieving competitors" });
            }
        }

        /// <summary>
        /// Add competitor
        /// </summary>
        [HttpPost("{projectId}/competitors")]
        public async Task<IActionResult> AddCompetitor(int projectId, [FromBody] AddCompetitorRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Domain))
                {
                    return BadRequest(new { message = "Domain is required" });
                }

                var competitor = await _competitorService.AddCompetitorAsync(projectId, request.Domain);

                return Ok(new
                {
                    id = competitor.Id,
                    name = competitor.Name,
                    domain = competitor.Domain,
                    competitorType = competitor.CompetitorType,
                    logoUrl = competitor.LogoUrl,
                    createdAt = competitor.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding competitor for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Error adding competitor" });
            }
        }

        /// <summary>
        /// Delete competitor
        /// </summary>
        [HttpDelete("competitors/{competitorId}")]
        public async Task<IActionResult> DeleteCompetitor(int competitorId)
        {
            try
            {
                await _competitorService.DeleteCompetitorAsync(competitorId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting competitor {CompetitorId}", competitorId);
                return StatusCode(500, new { message = "Error deleting competitor" });
            }
        }
    }

    public class BrandProfileRequest
    {
        public string? Overview { get; set; }
        public string? Description { get; set; }
        public string[]? Topics { get; set; }
    }

    public class AddCompetitorRequest
    {
        public string Domain { get; set; } = string.Empty;
    }
}
