using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityOpportunitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommunityOpportunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CommunityOpportunities/project/5
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<CommunityOpportunity>>> GetByProject(int projectId)
        {
            return await _context.CommunityOpportunities
                .Where(o => o.ProjectId == projectId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        // PUT: api/CommunityOpportunities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOpportunity(int id, CommunityOpportunity opportunity)
        {
            if (id != opportunity.Id)
            {
                return BadRequest();
            }

            _context.Entry(opportunity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OpportunityExists(id))
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

        private bool OpportunityExists(int id)
        {
            return _context.CommunityOpportunities.Any(e => e.Id == id);
        }
    }
}
