namespace AppBackendBasic.Core.Configuration
{
    public class LlmProviderSettings
    {
        public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
        public string DefaultProvider { get; set; } = "OpenAI";
        public bool EnableFallback { get; set; } = true;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class ProviderConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 4096;
        public double Temperature { get; set; } = 0.7;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 0;
        public string? BaseUrl { get; set; }
        public Dictionary<string, string>? AdditionalHeaders { get; set; }
    }
}
