namespace SiigoSharp;

/// <summary>
/// Entry point to the Siigo API. Depend on this interface to replace the client with a fake in tests.
/// </summary>
public interface ISiigoClient
{
    /// <summary>Customers (and other third parties): <c>/v1/customers</c>.</summary>
    ICustomersClient Customers { get; }

    /// <summary>Products and services: <c>/v1/products</c>.</summary>
    IProductsClient Products { get; }

    /// <summary>Sales invoices: <c>/v1/invoices</c>.</summary>
    IInvoicesClient Invoices { get; }

    /// <summary>
    /// Read-only catalogs with the ids that customers, products and invoices refer to: document types,
    /// taxes, payment types, sellers, account groups and cost centers.
    /// </summary>
    ICatalogsClient Catalogs { get; }
}
