namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for sanitizing user input to prevent XSS attacks
/// </summary>
public interface IInputSanitizationService
{
    /// <summary>
    /// Sanitizes HTML content by removing potentially dangerous tags and attributes
    /// </summary>
    string SanitizeHtml(string input);
    
    /// <summary>
    /// Sanitizes plain text by encoding special characters
    /// </summary>
    string SanitizeText(string input);
    
    /// <summary>
    /// Checks if input contains potentially malicious content
    /// </summary>
    bool ContainsMaliciousContent(string input);
    
    /// <summary>
    /// Validates that input doesn't contain XSS patterns
    /// </summary>
    bool IsValidInput(string input);
}
