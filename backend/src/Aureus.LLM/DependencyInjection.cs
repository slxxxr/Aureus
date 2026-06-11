using Aureus.LLM.Gemini;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aureus.LLM;

public static class DependencyInjection
{
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.Section));

        services.AddHttpClient<ILlmClient, GeminiClient>((sp, client) =>
        {
            var opts = configuration.GetSection(GeminiOptions.Section).Get<GeminiOptions>() ?? new();
            client.BaseAddress = new Uri(opts.BaseUrl);
        });

        return services;
    }
}
