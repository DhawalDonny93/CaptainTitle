namespace AppBackendBasic.Core.DTOs
{
    public class GenerateOnboardingPromptsRequest
    {
        public int ProjectId { get; set; }
        public string? Industry { get; set; }
        public string? CompanyName { get; set; }
        public string? TargetAudience { get; set; }
    }

    public class GeneratedPrompt
    {
        public string PromptText { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public List<string> SuggestedPlatforms { get; set; } = new List<string>();
        public bool IsSelected { get; set; } = false;
    }

    public class GenerateOnboardingPromptsResponse
    {
        public bool Success { get; set; }
        public List<GeneratedPrompt> Prompts { get; set; } = new List<GeneratedPrompt>();
        public string? ErrorMessage { get; set; }
    }

    public class SaveOnboardingPromptsRequest
    {
        public int ProjectId { get; set; }
        public List<SelectedPrompt> SelectedPrompts { get; set; } = new List<SelectedPrompt>();
    }

    public class SelectedPrompt
    {
        public string PromptText { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public List<string> Platforms { get; set; } = new List<string>();
    }

    public class SaveOnboardingPromptsResponse
    {
        public bool Success { get; set; }
        public int PromptsCreated { get; set; }
        public List<int> PromptIds { get; set; } = new List<int>();
        public string? ErrorMessage { get; set; }
    }
}
