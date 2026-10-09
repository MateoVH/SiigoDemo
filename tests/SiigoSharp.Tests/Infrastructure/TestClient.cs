using Microsoft.Extensions.Logging;

namespace SiigoSharp.Tests.Infrastructure;

internal static class TestClient
{
    public static SiigoClientOptions CreateOptions(Action<SiigoClientOptions>? configure = null)
    {
        var options = new SiigoClientOptions
        {
            Username = "api@empresa.com",
            AccessKey = "secret-access-key",
            PartnerId = "SiigoSharpTests",
        };
        configure?.Invoke(options);
        return options;
    }

    public static SiigoClient Create(
        FakeSiigoHandler handler,
        Action<SiigoClientOptions>? configure = null,
        TimeProvider? timeProvider = null,
        ILoggerFactory? loggerFactory = null) =>
        new(CreateOptions(configure), handler, loggerFactory, timeProvider ?? new TestTimeProvider());
}
