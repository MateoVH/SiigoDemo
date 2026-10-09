namespace SiigoSharp;

/// <summary>
/// Read-only catalogs of the company. They hold the ids that products and invoices refer to; they change
/// rarely, so it is worth caching them.
/// </summary>
public interface ICatalogsClient
{
    /// <summary>The sales invoice document types (<c>"FV"</c>).</summary>
    Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(CancellationToken cancellationToken = default) =>
        GetDocumentTypesAsync(DocumentTypeCodes.SalesInvoice, cancellationToken);

    /// <summary>The document types of a class, e.g. credit notes (<c>"NC"</c>).</summary>
    /// <param name="type">The document class; see <see cref="DocumentTypeCodes"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(string type, CancellationToken cancellationToken = default);

    /// <summary>The taxes, such as IVA 19%.</summary>
    Task<IReadOnlyList<Tax>> GetTaxesAsync(CancellationToken cancellationToken = default);

    /// <summary>The payment methods available for sales invoices (<c>"FV"</c>).</summary>
    Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync(CancellationToken cancellationToken = default) =>
        GetPaymentTypesAsync(DocumentTypeCodes.SalesInvoice, cancellationToken);

    /// <summary>The payment methods available for a document class.</summary>
    /// <param name="documentType">The document class: FV, NC or RC; see <see cref="DocumentTypeCodes"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync(string documentType, CancellationToken cancellationToken = default);

    /// <summary>The users of the company, who can act as sellers. Reads every page.</summary>
    Task<IReadOnlyList<SiigoUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>The inventory groups (account groups) products belong to.</summary>
    Task<IReadOnlyList<AccountGroup>> GetAccountGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>The cost centers.</summary>
    Task<IReadOnlyList<CostCenter>> GetCostCentersAsync(CancellationToken cancellationToken = default);
}
