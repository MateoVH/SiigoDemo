using Microsoft.Extensions.Options;

namespace SiigoSharp.Internal;

/// <summary>
/// Validates <see cref="SiigoClientOptions"/> with messages in the configured language. With dependency
/// injection it runs at startup; a standalone <see cref="SiigoClient"/> runs it in its constructor.
/// </summary>
internal sealed class SiigoClientOptionsValidator : IValidateOptions<SiigoClientOptions>
{
    public ValidateOptionsResult Validate(string? name, SiigoClientOptions options)
    {
        var failures = GetFailures(options);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    public static void ThrowIfInvalid(SiigoClientOptions options)
    {
        var failures = GetFailures(options);
        if (failures.Count > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(SiigoClientOptions), failures);
        }
    }

    private static List<string> GetFailures(SiigoClientOptions options)
    {
        var messages = new Messages(options.Language);
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            failures.Add(messages.OptionRequired(nameof(options.Username)));
        }

        if (string.IsNullOrWhiteSpace(options.AccessKey))
        {
            failures.Add(messages.OptionRequired(nameof(options.AccessKey)));
        }

        if (string.IsNullOrWhiteSpace(options.PartnerId))
        {
            failures.Add(messages.OptionRequired(nameof(options.PartnerId)));
        }
        else if (!IsAlphanumeric(options.PartnerId, minLength: 3, maxLength: 100))
        {
            // Siigo rejects other values with invalid_partner_id.
            failures.Add(messages.PartnerIdFormat);
        }

        if (options.BaseAddress is not { IsAbsoluteUri: true })
        {
            failures.Add(messages.OptionMustBeAbsoluteUrl(nameof(options.BaseAddress)));
        }

        if (options.Timeout <= TimeSpan.Zero && options.Timeout != Timeout.InfiniteTimeSpan)
        {
            failures.Add(messages.OptionMustBePositive(nameof(options.Timeout)));
        }

        if (options.TokenRefreshMargin < TimeSpan.Zero)
        {
            failures.Add(messages.OptionMustNotBeNegative(nameof(options.TokenRefreshMargin)));
        }

        if (options.Retry is null)
        {
            failures.Add(messages.OptionRequired(nameof(options.Retry)));
            return failures;
        }

        if (options.Retry.MaxRetries < 0)
        {
            failures.Add(messages.OptionMustNotBeNegative("Retry:MaxRetries"));
        }

        if (options.Retry.BaseDelay < TimeSpan.Zero)
        {
            failures.Add(messages.OptionMustNotBeNegative("Retry:BaseDelay"));
        }

        if (options.Retry.MaxDelay < TimeSpan.Zero)
        {
            failures.Add(messages.OptionMustNotBeNegative("Retry:MaxDelay"));
        }

        return failures;
    }

    internal static bool IsAlphanumeric(string value, int minLength, int maxLength) =>
        value.Length >= minLength && value.Length <= maxLength && value.All(char.IsAsciiLetterOrDigit);
}
