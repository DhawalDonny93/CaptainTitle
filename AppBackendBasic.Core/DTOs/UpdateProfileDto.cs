using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AppBackendBasic.Core.DTOs;

public class UpdateProfileDto
{
    [StringLength(100)]
    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    [JsonPropertyName("age")]
    public int Age { get; set; }
}
