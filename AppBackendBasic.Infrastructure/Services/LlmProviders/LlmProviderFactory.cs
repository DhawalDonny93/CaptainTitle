using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AppBackendBasic.Infrastructure.Services.LlmProviders
{
    public class LlmProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public LlmProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILlmProvider GetProvider(string providerName)
        {
            return providerName.ToLower() switch
            {
                "openai" => _serviceProvider.GetRequiredService<OpenAIProvider>(),
                "anthropic" => _serviceProvider.GetRequiredService<AnthropicProvider>(),
                "google" => _serviceProvider.GetRequiredService<GoogleGeminiProvider>(),
                "perplexity" => _serviceProvider.GetRequiredService<PerplexityProvider>(),
                "googlesearch" => _serviceProvider.GetRequiredService<GoogleSearchProvider>(),
                _ => throw new NotSupportedException($"Provider '{providerName}' is not supported")
            };
        }

        public List<string> GetAvailableProviders()
        {
            return new List<string> { "OpenAI", "Anthropic", "Google", "Perplexity", "GoogleSearch" };
        }
    }
}
