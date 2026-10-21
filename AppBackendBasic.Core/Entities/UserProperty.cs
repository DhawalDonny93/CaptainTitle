using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackendBasic.Core.Entities;

public class UserProperty
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string Properties { get; set; } = "{}";
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
