using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Security;

public class InputSanitizationService : IInputSanitizationService
{
    private readonly ILogger<InputSanitizationService> _logger;
    
    // Common XSS patterns to detect
    private static readonly string[] XssPatterns = new[]
    {
        @"<script[\s\S]*?>[\s\S]*?</script>",
        @"javascript:",
        @"on\w+\s*=",
        @"<iframe[\s\S]*?>",
        @"<object[\s\S]*?>",
        @"<embed[\s\S]*?>",
        @"<applet[\s\S]*?>",
        @"<meta[\s\S]*?>",
        @"<link[\s\S]*?>",
        @"<style[\s\S]*?>[\s\S]*?</style>",
        @"eval\s*\(",
        @"expression\s*\(",
        @"vbscript:",
        @"data:text/html"
    };

    // Allowed HTML tags for sanitization (if you need to allow some HTML)
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "b", "i", "u", "strong", "em", "br", "p", "span"
    };

    public InputSanitizationService(ILogger<InputSanitizationService> logger)
    {
        _logger = logger;
    }

    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        try
        {
            // Remove all HTML tags except allowed ones
            var sanitized = Regex.Replace(input, @"<(?!\/?(b|i|u|strong|em|br|p|span)\b)[^>]*>", string.Empty, 
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Remove event handlers and javascript
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*[""'][^""']*[""']", string.Empty, 
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            sanitized = Regex.Replace(sanitized, @"javascript:", string.Empty, 
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // HTML encode the result
            sanitized = HttpUtility.HtmlEncode(sanitized);

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML input");
            // If sanitization fails, return HTML encoded version as fallback
            return HttpUtility.HtmlEncode(input);
        }
    }

    public string SanitizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        try
        {
            // HTML encode to prevent XSS
            var sanitized = HttpUtility.HtmlEncode(input);

            // Remove any null bytes
            sanitized = sanitized.Replace("\0", string.Empty);

            // Normalize whitespace
            sanitized = Regex.Replace(sanitized, @"\s+", " ", RegexOptions.Compiled);

            return sanitized.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing text input");
            return HttpUtility.HtmlEncode(input);
        }
    }

    public bool ContainsMaliciousContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            // Check against known XSS patterns
            foreach (var pattern in XssPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                {
                    _logger.LogWarning("Malicious content detected: Pattern '{Pattern}' found in input", pattern);
                    return true;
                }
            }

            // Check for encoded script tags
            var decoded = HttpUtility.HtmlDecode(input);
            if (decoded != input)
            {
                // Input was encoded, check decoded version
                foreach (var pattern in XssPatterns)
                {
                    if (Regex.IsMatch(decoded, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                    {
                        _logger.LogWarning("Malicious content detected in decoded input: Pattern '{Pattern}'", pattern);
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for malicious content");
            // Err on the side of caution
            return true;
        }
    }

    public bool IsValidInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true; // Empty input is valid

        return !ContainsMaliciousContent(input);
    }
}
