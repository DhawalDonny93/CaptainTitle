namespace AppBackendBasic.Core.DTOs
{
    public class GeneratePromptResearchRequest
    {
        public int ProjectId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int Count { get; set; } = 20;
    }

    public class UpdatePromptResearchStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class AnalyzeRelevanceRequest
    {
        public string PromptText { get; set; } = string.Empty;
    }
}
