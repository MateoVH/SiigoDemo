namespace SiigoSharp;

/// <summary>Operations on sales invoices (<c>/v1/invoices</c>).</summary>
public interface IInvoicesClient
{
    /// <summary>Reads one page of invoices.</summary>
    Task<SiigoPage<Invoice>> ListAsync(InvoiceQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads every invoice that matches the filters, requesting the pages as they are consumed.</summary>
    IAsyncEnumerable<Invoice> ListAllAsync(InvoiceQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads an invoice by its Siigo id.</summary>
    /// <exception cref="SiigoApiException">Siigo answered with an error, e.g. 404 when the id does not exist.</exception>
    Task<Invoice> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a sales invoice. The request carries an <c>Idempotency-Key</c>, so it is retried after
    /// network or server errors without risk of creating the invoice twice.
    /// </summary>
    /// <exception cref="SiigoApiException">
    /// Siigo rejected the invoice, e.g. with <c>invalid_total_payments</c> when the payments do not add up
    /// to the total.
    /// </exception>
    Task<Invoice> CreateAsync(InvoiceRequest invoice, CancellationToken cancellationToken = default);

    /// <summary>Downloads the PDF of an invoice.</summary>
    /// <returns>The content of the PDF file.</returns>
    Task<byte[]> GetPdfAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>E-mails an invoice.</summary>
    /// <param name="id">The Siigo id of the invoice.</param>
    /// <param name="mailTo">The recipient.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The delivery status reported by Siigo.</returns>
    Task<InvoiceMail> SendByEmailAsync(string id, string mailTo, CancellationToken cancellationToken = default) =>
        SendByEmailAsync(id, mailTo, copyTo: null, cancellationToken);

    /// <summary>E-mails an invoice, with copies.</summary>
    /// <param name="id">The Siigo id of the invoice.</param>
    /// <param name="mailTo">The recipient.</param>
    /// <param name="copyTo">Recipients of a copy, separated by semicolons (up to 5).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The delivery status reported by Siigo.</returns>
    Task<InvoiceMail> SendByEmailAsync(string id, string mailTo, string? copyTo, CancellationToken cancellationToken = default);
}
