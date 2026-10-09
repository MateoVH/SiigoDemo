namespace SiigoSharp;

/// <summary>The data to create a sales invoice with <see cref="IInvoicesClient.CreateAsync"/>.</summary>
/// <example>
/// <code>
/// var items = new List&lt;InvoiceItemRequest&gt;
/// {
///     new() { Code = "TORN-001", Quantity = 100, Price = 350m, Taxes = [new IdReference(iva19.Id)] },
/// };
/// var totals = InvoiceTotals.Calculate(items, taxes);
///
/// var invoice = await siigo.Invoices.CreateAsync(new InvoiceRequest
/// {
///     Document = new IdReference(salesInvoiceType.Id),
///     Date = DateOnly.FromDateTime(DateTime.Today),
///     Customer = new InvoiceCustomerReference { Identification = "900123456" },
///     Seller = seller.Id,
///     Items = items,
///     Payments = [new InvoicePaymentRequest { Id = cash.Id, Value = totals.Total }],
/// });
/// </code>
/// </example>
public sealed class InvoiceRequest
{
    /// <summary>
    /// The document type, e.g. the company's electronic sales invoice; see
    /// <see cref="ICatalogsClient.GetDocumentTypesAsync(CancellationToken)"/>.
    /// </summary>
    public required IdReference Document { get; set; }

    /// <summary>The invoice date. Electronic invoices cannot be dated before today.</summary>
    public required DateOnly Date { get; set; }

    /// <summary>The customer, by identification. It must already exist in Siigo.</summary>
    public required InvoiceCustomerReference Customer { get; set; }

    /// <summary>The id of the seller; see <see cref="ICatalogsClient.GetUsersAsync"/>.</summary>
    public required int Seller { get; set; }

    /// <summary>The items, up to 500.</summary>
    public required IList<InvoiceItemRequest> Items { get; set; }

    /// <summary>
    /// The payments. Their values must add up to the invoice total or Siigo rejects the invoice with
    /// <c>invalid_total_payments</c>; <see cref="InvoiceTotals.Calculate"/> computes that total.
    /// </summary>
    public required IList<InvoicePaymentRequest> Payments { get; set; }

    /// <summary>The invoice number; only for document types without automatic numbering.</summary>
    public long? Number { get; set; }

    /// <summary>The cost center id, when the document type uses cost centers.</summary>
    public int? CostCenter { get; set; }

    /// <summary>The currency, for invoices in a foreign currency.</summary>
    public InvoiceCurrency? Currency { get; set; }

    /// <summary>Observations printed on the invoice.</summary>
    public string? Observations { get; set; }

    /// <summary>Withholdings such as ReteIVA or ReteICA, by tax id.</summary>
    public IList<IdReference>? Retentions { get; set; }

    /// <summary>
    /// Whether to send the electronic invoice to the DIAN when it is created (Siigo field <c>stamp.send</c>).
    /// Defaults to <c>false</c>: the invoice stays as a draft, without CUFE, and can be sent later from Siigo Nube.
    /// </summary>
    public bool SendToDian { get; set; }

    /// <summary>Whether to e-mail the invoice to the customer when it is created (Siigo field <c>mail.send</c>).</summary>
    public bool SendByEmail { get; set; }

    /// <summary>
    /// The <c>Idempotency-Key</c> of the request, sent as a header: 1 to 30 letters or digits. When Siigo
    /// receives a key it already processed, it returns the invoice it created instead of creating another one.
    /// </summary>
    /// <remarks>
    /// When it is not set, the library generates a key for each call, which makes its own retries safe. If
    /// your code retries on its own (after a timeout, a crash or a restart), set a key derived from your data,
    /// such as the order number: only then does the retry reach Siigo with the same key.
    /// </remarks>
    public string? IdempotencyKey { get; set; }
}

/// <summary>The customer of an invoice, identified by identification number and branch office.</summary>
public sealed class InvoiceCustomerReference
{
    /// <summary>The identification number, without the verification digit.</summary>
    public required string Identification { get; set; }

    /// <summary>The branch office; 0 (the main one) by default.</summary>
    public int? BranchOffice { get; set; }
}

/// <summary>An item of an invoice.</summary>
public sealed class InvoiceItemRequest
{
    /// <summary>The product code.</summary>
    public required string Code { get; set; }

    /// <summary>The description printed on the invoice.</summary>
    public string? Description { get; set; }

    /// <summary>The quantity, with up to 2 decimals.</summary>
    public required decimal Quantity { get; set; }

    /// <summary>The unit price before taxes, with up to 6 decimals.</summary>
    public required decimal Price { get; set; }

    /// <summary>
    /// The discount of the item: a value or a percentage, depending on the
    /// <see cref="DocumentType.DiscountType"/> of the document type.
    /// </summary>
    public decimal? Discount { get; set; }

    /// <summary>The taxes, by id: at most three, and never two of the same type.</summary>
    public IList<IdReference>? Taxes { get; set; }
}

/// <summary>A payment of an invoice.</summary>
public sealed class InvoicePaymentRequest
{
    /// <summary>The payment method id; see <see cref="ICatalogsClient.GetPaymentTypesAsync(CancellationToken)"/>.</summary>
    public required int Id { get; set; }

    /// <summary>The amount.</summary>
    public required decimal Value { get; set; }

    /// <summary>The due date; required when the payment method has one (e.g. credit).</summary>
    public DateOnly? DueDate { get; set; }
}

/// <summary>The currency of an invoice.</summary>
public sealed class InvoiceCurrency
{
    /// <summary>The currency code, e.g. <c>"USD"</c>.</summary>
    public string? Code { get; set; }

    /// <summary>The exchange rate to Colombian pesos.</summary>
    public decimal? ExchangeRate { get; set; }
}
