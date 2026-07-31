// namespace Microsoft.Extensions.DependencyInjection;

// using System;
// using System.Net.Http;
// using Polly;
// using Polly.Extensions.Http;

// /// <summary>
// /// Extension methods for registering HTTP Client resilience policies.
// /// </summary>
// public static class ResilienceExtensions
// {
//     /// <summary>
//     /// Adds standard retry and circuit breaker policies to a named <see cref="HttpClient"/>.
//     /// </summary>
//     public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
//         this IServiceCollection services) 
//         where TClient : class 
//         where TImplementation : class, TClient
//     {
//         ArgumentNullException.ThrowIfNull(services);

//         return services.AddHttpClient<TClient, TImplementation>()
//             .AddTransientHttpErrorPolicy(policy => 
//                 policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
//             .AddTransientHttpErrorPolicy(policy => 
//                 policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
//     }
// }