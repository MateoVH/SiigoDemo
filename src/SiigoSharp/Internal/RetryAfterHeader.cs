namespace SiigoSharp.Internal;

internal static class RetryAfterHeader
{
    /// <summary>
    /// Reads <c>Retry-After</c>, which can be a number of seconds or a date. Returns <c>null</c> when it is absent.
    /// </summary>
    public static TimeSpan? GetDelay(HttpResponseMessage response, TimeProvider timeProvider)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (header?.Date is { } date)
        {
            var delay = date - timeProvider.GetUtcNow();
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }
}
