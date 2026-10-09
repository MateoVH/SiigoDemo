namespace SiigoSharp;

/// <summary>Filters for <see cref="IInvoicesClient.ListAsync"/>. Unset filters are not sent.</summary>
public sealed record InvoiceQuery : SiigoQuery
{
    /// <summary>The document type id.</summary>
    public int? DocumentId { get; init; }

    /// <summary>The identification number of the customer.</summary>
    public string? CustomerIdentification { get; init; }

    /// <summary>The branch office of the customer.</summary>
    public int? CustomerBranchOffice { get; init; }

    /// <summary>The full invoice number, e.g. <c>"FV-1-22"</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Invoices dated on or after this day.</summary>
    public DateOnly? DateStart { get; init; }

    /// <summary>Invoices dated on or before this day.</summary>
    public DateOnly? DateEnd { get; init; }

    /// <summary>Created at or after this instant.</summary>
    public DateTimeOffset? CreatedStart { get; init; }

    /// <summary>Created at or before this instant.</summary>
    public DateTimeOffset? CreatedEnd { get; init; }

    /// <summary>Updated at or after this instant.</summary>
    public DateTimeOffset? UpdatedStart { get; init; }

    /// <summary>Updated at or before this instant.</summary>
    public DateTimeOffset? UpdatedEnd { get; init; }
}
