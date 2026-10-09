using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SiigoSharp;
using SiigoSharp.Internal;

// In this namespace so that services.AddSiigo(...) is found without an extra using directive.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers <see cref="ISiigoClient"/> with dependency injection.</summary>
public static class SiigoServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ISiigoClient"/> as a singleton configured in code.</summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSiigo(options =>
    /// {
    ///     options.Username = builder.Configuration["Siigo:Username"]!;
    ///     options.AccessKey = builder.Configuration["Siigo:AccessKey"]!;
    ///     options.PartnerId = "MiIntegracion";
    /// });
    /// </code>
    /// </example>
    /// <returns>The builder of the API <see cref="HttpClient"/>, to add handlers or a proxy.</returns>
    public static IHttpClientBuilder AddSiigo(this IServiceCollection services, Action<SiigoClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SiigoClientOptions>().Configure(configure);
        return AddSiigoCore(services);
    }

    /// <summary>
    /// Registers <see cref="ISiigoClient"/> as a singleton bound to a configuration section,
    /// e.g. <c>services.AddSiigo(configuration.GetSection("Siigo"))</c>.
    /// </summary>
    /// <returns>The builder of the API <see cref="HttpClient"/>, to add handlers or a proxy.</returns>
    public static IHttpClientBuilder AddSiigo(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SiigoClientOptions>().Bind(configuration);
        return AddSiigoCore(services);
    }

    private static IHttpClientBuilder AddSiigoCore(IServiceCollection services)
    {
        // A second AddSiigo call only adds configuration: the handlers must not be registered twice.
        if (services.Any(service => service.ServiceType == typeof(SiigoTokenProvider)))
        {
            return services.AddHttpClient(SiigoHttp.ClientName);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SiigoClientOptions>, SiigoClientOptionsValidator>());
        services.AddOptions<SiigoClientOptions>().ValidateOnStart();
        services.AddSingleton(provider =>
            new SiigoOptionsSnapshot(provider.GetRequiredService<IOptions<SiigoClientOptions>>().Value.Clone()));

        services.AddSingleton(provider => new SiigoTokenProvider(
            () => CreateHttpClient(provider),
            GetOptions(provider),
            provider.GetRequiredService<TimeProvider>(),
            CreateLogger(provider, LogCategories.Authentication)));

        // A singleton is safe here: it asks IHttpClientFactory for a client on every request,
        // so connections are still rotated.
        services.TryAddSingleton<ISiigoClient>(provider => new SiigoClient(new SiigoConnection(
            () => CreateHttpClient(provider),
            new Messages(GetOptions(provider).Language),
            provider.GetRequiredService<TimeProvider>())));

        return services.AddHttpClient(SiigoHttp.ClientName)
            .ConfigureHttpClient((provider, client) => SiigoHttp.Configure(client, GetOptions(provider)))

            // The factory's default logs write full URLs, whose query strings carry ID numbers (and, at
            // Trace level, the Authorization header). The library logs what matters, paths only.
            .RemoveAllLoggers()
            .RedactLoggedHeaders(["Authorization"])
            .AddHttpMessageHandler(provider => new SiigoRetryHandler(
                GetOptions(provider),
                provider.GetRequiredService<TimeProvider>(),
                CreateLogger(provider, LogCategories.Http)))
            .AddHttpMessageHandler(provider => new SiigoAuthenticationHandler(
                provider.GetRequiredService<SiigoTokenProvider>(),
                GetOptions(provider).Language,
                CreateLogger(provider, LogCategories.Authentication)));
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider) =>
        provider.GetRequiredService<IHttpClientFactory>().CreateClient(SiigoHttp.ClientName);

    private static SiigoClientOptions GetOptions(IServiceProvider provider) =>
        provider.GetRequiredService<SiigoOptionsSnapshot>().Value;

    private static ILogger CreateLogger(IServiceProvider provider, string category) =>
        provider.GetService<ILoggerFactory>()?.CreateLogger(category) ?? NullLogger.Instance;
}
