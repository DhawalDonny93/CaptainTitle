using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AppBackendBasic.Core.Validation;

/// <summary>
/// Validation attribute to prevent XSS attacks in user input
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class NoXssAttribute : ValidationAttribute
{
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
        @"eval\s*\(",
        @"expression\s*\(",
        @"vbscript:",
        @"data:text/html"
    };

    public NoXssAttribute() : base("The field {0} contains potentially dangerous content.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        var input = value.ToString();
        
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Success;

        // Check against XSS patterns
        foreach (var pattern in XssPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                return new ValidationResult(
                    FormatErrorMessage(validationContext.DisplayName),
                    new[] { validationContext.MemberName ?? string.Empty }
                );
            }
        }

        return ValidationResult.Success;
    }
}
