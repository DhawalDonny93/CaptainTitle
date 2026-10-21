using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserTaskService _taskService;

        public UserTasksController(ApplicationDbContext context, IUserTaskService taskService)
        {
            _context = context;
            _taskService = taskService;
        }

        // GET: api/UserTasks/project/5
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<UserTask>>> GetByProject(int projectId)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.UserId != userId)
                return Forbid();

            // Order by status priority: InProgress, Pending, Cancelled, Completed
            var statusOrder = new Dictionary<string, int>
            {
                { "InProgress", 1 },
                { "Pending", 2 },
                { "Cancelled", 3 },
                { "Completed", 4 }
            };

            var tasks = await _context.UserTasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            return tasks
                .OrderBy(t => statusOrder.ContainsKey(t.Status) ? statusOrder[t.Status] : 99)
                .ThenByDescending(t => t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : 1)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();
        }

        // POST: api/UserTasks
        [HttpPost]
        public async Task<ActionResult<UserTask>> CreateTask(CreateTaskDto dto)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            // Verify project ownership
            var project = await _context.Projects.FindAsync(dto.ProjectId);
            if (project == null || project.UserId != userId)
                return BadRequest(new { message = "Invalid project or unauthorized" });

            var task = new UserTask
            {
                ProjectId = dto.ProjectId,
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description ?? string.Empty,
                Priority = dto.Priority ?? "Medium",
                Status = dto.Status ?? "Pending",
                DueDate = dto.DueDate,
                SourceUrl = dto.SourceUrl,
                Source = "Manual",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserTasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByProject), new { projectId = task.ProjectId }, task);
        }

        // PUT: api/UserTasks/5/complete
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var task = await _context.UserTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            if (task.Project.UserId != userId) return Forbid();

            task.IsCompleted = !task.IsCompleted;
            if (task.IsCompleted)
            {
                task.Status = "Completed";
            }
            else
            {
                task.Status = "Pending";
            }
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/UserTasks/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var task = await _context.UserTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            if (task.Project.UserId != userId) return Forbid();

            // Valid statuses: Pending, InProgress, Completed, Cancelled
            var validStatuses = new[] { "Pending", "InProgress", "Completed", "Cancelled" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { message = "Invalid status. Valid values: Pending, InProgress, Completed, Cancelled" });

            task.Status = request.Status;
            task.IsCompleted = request.Status == "Completed";
            task.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // PUT: api/UserTasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var existingTask = await _context.UserTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
            if (existingTask == null) return NotFound();
            if (existingTask.Project.UserId != userId) return Forbid();

            if (dto.Title != null) existingTask.Title = dto.Title;
            if (dto.Description != null) existingTask.Description = dto.Description;
            if (dto.Priority != null) existingTask.Priority = dto.Priority;
            if (dto.Status != null)
            {
                existingTask.Status = dto.Status;
                existingTask.IsCompleted = dto.Status == "Completed";
            }
            if (dto.DueDate.HasValue) existingTask.DueDate = dto.DueDate;
            existingTask.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/UserTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var task = await _context.UserTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            if (task.Project.UserId != userId) return Forbid();

            _context.UserTasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/UserTasks/user
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<UserTask>>> GetUserTasks()
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            return await _context.UserTasks
                .Include(t => t.Project)
                .Where(t => t.Project.UserId == userId)
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.Priority)
                .ToListAsync();
        }
    }

    public class UpdateTaskStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
