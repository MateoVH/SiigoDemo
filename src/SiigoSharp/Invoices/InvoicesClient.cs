using SiigoSharp.Internal;

namespace SiigoSharp;

internal sealed class InvoicesClient(SiigoConnection connection) : IInvoicesClient
{
    private const string Path = "v1/invoices";

    public Task<SiigoPage<Invoice>> ListAsync(InvoiceQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new InvoiceQuery();
        var uri = new QueryStringBuilder()
            .Add("document_id", query.DocumentId)
            .Add("customer_identification", query.CustomerIdentification)
            .Add("customer_branch_office", query.CustomerBranchOffice)
            .Add("name", query.Name)
            .Add("date_start", query.DateStart)
            .Add("date_end", query.DateEnd)
            .Add("created_start", query.CreatedStart)
            .Add("created_end", query.CreatedEnd)
            .Add("updated_start", query.UpdatedStart)
            .Add("updated_end", query.UpdatedEnd)
            .AddPaging(query)
            .AppendTo(Path);

        return connection.GetAsync<SiigoPage<Invoice>>(uri, cancellationToken);
    }

    public IAsyncEnumerable<Invoice> ListAllAsync(InvoiceQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new InvoiceQuery();
        return Paginator.EnumerateAsync(
            query.Page ?? 1, (page, token) => ListAsync(query with { Page = page }, token), cancellationToken);
    }

    public Task<Invoice> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return connection.GetAsync<Invoice>($"{Path}/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public Task<Invoice> CreateAsync(InvoiceRequest invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var idempotencyKey = invoice.IdempotencyKey ?? NewIdempotencyKey();
        if (!SiigoClientOptionsValidator.IsAlphanumeric(idempotencyKey, minLength: 1, maxLength: 30))
        {
            throw new ArgumentException(connection.Messages.IdempotencyKeyFormat, nameof(invoice));
        }

        return connection.PostIdempotentAsync<InvoiceRequest, Invoice>(Path, invoice, idempotencyKey, cancellationToken);
    }

    public async Task<byte[]> GetPdfAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var path = $"{Path}/{Uri.EscapeDataString(id)}/pdf";
        var file = await connection.GetAsync<InvoiceFile>(path, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(file.Base64))
        {
            throw new SiigoException(connection.Messages.EmptyResponse(HttpMethod.Get, new Uri(path, UriKind.Relative)));
        }

        try
        {
            return Convert.FromBase64String(file.Base64);
        }
        catch (FormatException exception)
        {
            throw new SiigoException(connection.Messages.InvalidResponse(HttpMethod.Get, new Uri(path, UriKind.Relative)), exception);
        }
    }

    public Task<InvoiceMail> SendByEmailAsync(
        string id, string mailTo, string? copyTo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(mailTo);

        return connection.PostAsync<InvoiceMailRequest, InvoiceMail>(
            $"{Path}/{Uri.EscapeDataString(id)}/mail", new InvoiceMailRequest(mailTo, copyTo), cancellationToken);
    }

    // 30 hexadecimal characters: the most Siigo accepts, and 120 random bits.
    private static string NewIdempotencyKey() => Guid.NewGuid().ToString("N")[..30];

    private sealed class InvoiceFile
    {
        public string? Base64 { get; init; }
    }

    private sealed record InvoiceMailRequest(string MailTo, string? CopyTo);
}
