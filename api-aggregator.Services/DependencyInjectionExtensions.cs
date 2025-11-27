using Microsoft.Extensions.DependencyInjection;
using api_aggregator.Abstractions;
using api_aggregator.Models;
using api_aggregator.Services.ExternalApis;
using api_aggregator.Services.BackgroundServices;
using Polly;
using Polly.Extensions.Http;

namespace api_aggregator.Services;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApiAggregatorServices(this IServiceCollection services)
    {
        // Register configuration options
        services.AddOptions<AggregationOptions>()
            .BindConfiguration(AggregationOptions.SectionName)
            .ValidateOnStart();

        services.AddOptions<ExternalApiOptions>()
            .BindConfiguration(ExternalApiOptions.SectionName)
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateOnStart();

        // Register core services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<IPerformanceAnalyticsService, PerformanceAnalyticsService>();
        services.AddScoped<IDataAggregatorService, DataAggregatorService>();
        
        // Register authentication services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register background services
        services.AddHostedService<PerformanceMonitoringService>();

        // Register external API services with HttpClient and Polly policies
        services.AddHttpClient<IExternalApiService, WeatherApiService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        services.AddHttpClient<IExternalApiService, NewsApiService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        services.AddHttpClient<IExternalApiService, UsersApiService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts if needed
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
    }
}
