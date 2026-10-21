using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.DTOs
{
    public class CreateTaskDto
    {
        public int ProjectId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [MaxLength(20)]
        public string? Priority { get; set; }
        
        [MaxLength(20)]
        public string? Status { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        [MaxLength(500)]
        public string? SourceUrl { get; set; }
    }

    public class UpdateTaskDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }
        
        public string? Description { get; set; }
        
        [MaxLength(20)]
        public string? Priority { get; set; }
        
        [MaxLength(20)]
        public string? Status { get; set; }
        
        public DateTime? DueDate { get; set; }
    }
}
