using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SiigoSharp.Internal;

namespace SiigoSharp;

/// <summary>
/// Client for the Siigo API. It is thread-safe and keeps the access token cached, so create it once and
/// reuse it (or register it with <c>services.AddSiigo(...)</c>).
/// </summary>
/// <example>
/// <code>
/// using var siigo = new SiigoClient(new SiigoClientOptions
/// {
///     Username = "api@miempresa.com",
///     AccessKey = configuration["Siigo:AccessKey"]!,
///     PartnerId = "MiIntegracion",
/// });
///
/// var customer = await siigo.Customers.FindByIdentificationAsync("900123456");
/// </code>
/// </example>
public sealed class SiigoClient : ISiigoClient, IDisposable
{
    private readonly IDisposable[] _ownedResources;

    /// <summary>Creates a client that manages its own HTTP connections.</summary>
    /// <param name="options">Credentials and settings. They are validated right away.</param>
    /// <param name="loggerFactory">Receives token renewals and retries. Optional.</param>
    /// <exception cref="Microsoft.Extensions.Options.OptionsValidationException">A setting is missing or invalid.</exception>
    public SiigoClient(SiigoClientOptions options, ILoggerFactory? loggerFactory = null)
        : this(options, httpMessageHandler: null, loggerFactory)
    {
    }

    /// <summary>
    /// Creates a client that sends its requests through <paramref name="httpMessageHandler"/>, for example
    /// a handler configured with a proxy, or a fake one in tests. The client does not dispose it.
    /// </summary>
    /// <param name="options">Credentials and settings. They are validated right away.</param>
    /// <param name="httpMessageHandler">The handler that sends the requests; <c>null</c> to use a default one.</param>
    /// <param name="loggerFactory">Receives token renewals and retries. Optional.</param>
    /// <exception cref="Microsoft.Extensions.Options.OptionsValidationException">A setting is missing or invalid.</exception>
    public SiigoClient(SiigoClientOptions options, HttpMessageHandler? httpMessageHandler, ILoggerFactory? loggerFactory = null)
        : this(options, httpMessageHandler, loggerFactory, TimeProvider.System)
    {
    }

    internal SiigoClient(
        SiigoClientOptions options, HttpMessageHandler? httpMessageHandler, ILoggerFactory? loggerFactory, TimeProvider timeProvider)
        : this(Standalone.Create(options, httpMessageHandler, loggerFactory ?? NullLoggerFactory.Instance, timeProvider))
    {
    }

    /// <summary>Used by dependency injection, where <see cref="IHttpClientFactory"/> owns the connections.</summary>
    internal SiigoClient(SiigoConnection connection)
        : this(connection, ownedResources: [])
    {
    }

    private SiigoClient(Standalone standalone)
        : this(standalone.Connection, standalone.OwnedResources)
    {
    }

    private SiigoClient(SiigoConnection connection, IDisposable[] ownedResources)
    {
        _ownedResources = ownedResources;
        Customers = new CustomersClient(connection);
        Products = new ProductsClient(connection);
        Invoices = new InvoicesClient(connection);
        Catalogs = new CatalogsClient(connection);
    }

    /// <inheritdoc />
    public ICustomersClient Customers { get; }

    /// <inheritdoc />
    public IProductsClient Products { get; }

    /// <inheritdoc />
    public IInvoicesClient Invoices { get; }

    /// <inheritdoc />
    public ICatalogsClient Catalogs { get; }

    /// <summary>Releases the HTTP connections created by this client.</summary>
    public void Dispose()
    {
        foreach (var resource in _ownedResources)
        {
            resource.Dispose();
        }
    }

    /// <summary>
    /// Builds the pipeline of a client that is not managed by dependency injection:
    /// retries → authentication → network. The token request travels through the same pipeline,
    /// marked to skip authentication.
    /// </summary>
    private sealed record Standalone(SiigoConnection Connection, IDisposable[] OwnedResources)
    {
        public static Standalone Create(
            SiigoClientOptions options, HttpMessageHandler? handler, ILoggerFactory loggerFactory, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(options);
            SiigoClientOptionsValidator.ThrowIfInvalid(options);
            options = options.Clone();

            var ownsHandler = handler is null;

            // Recycling connections every few minutes lets a long-lived client notice DNS changes.
            handler ??= new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };

            var authLogger = loggerFactory.CreateLogger(LogCategories.Authentication);
            HttpClient? httpClient = null;
            var tokens = new SiigoTokenProvider(() => httpClient!, options, timeProvider, authLogger);

            var pipeline = new SiigoRetryHandler(options, timeProvider, loggerFactory.CreateLogger(LogCategories.Http))
            {
                InnerHandler = new SiigoAuthenticationHandler(tokens, options.Language, authLogger) { InnerHandler = handler },
            };

            // A handler supplied by the caller stays theirs: the HttpClient must not dispose it.
            httpClient = new HttpClient(pipeline, disposeHandler: false);
            SiigoHttp.Configure(httpClient, options);

            var connection = new SiigoConnection(() => httpClient, new Messages(options.Language), timeProvider);
            IDisposable[] owned = ownsHandler ? [httpClient, handler] : [httpClient];
            return new Standalone(connection, owned);
        }
    }
}
