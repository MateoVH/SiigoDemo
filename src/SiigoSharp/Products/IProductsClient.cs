namespace SiigoSharp;

/// <summary>Operations on products and services (<c>/v1/products</c>).</summary>
public interface IProductsClient
{
    /// <summary>Reads one page of products, newest first.</summary>
    Task<SiigoPage<Product>> ListAsync(ProductQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads every product that matches the filters, requesting the pages as they are consumed.</summary>
    IAsyncEnumerable<Product> ListAllAsync(ProductQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads a product by its Siigo id.</summary>
    /// <exception cref="SiigoApiException">Siigo answered with an error, e.g. 404 when the id does not exist.</exception>
    Task<Product> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Finds a product by its code, or returns <c>null</c>.</summary>
    Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Creates a product.</summary>
    /// <exception cref="SiigoApiException">Siigo rejected the data, e.g. with <c>already_exists</c> when the code is taken.</exception>
    Task<Product> CreateAsync(ProductRequest product, CancellationToken cancellationToken = default);

    /// <summary>Replaces a product with the given data.</summary>
    Task<Product> UpdateAsync(string id, ProductRequest product, CancellationToken cancellationToken = default);

    /// <summary>Deletes a product.</summary>
    /// <remarks>
    /// A DELETE is retried after transient errors. If a response was lost after Siigo deleted the product,
    /// the retry answers 404: catch <see cref="SiigoApiException"/> with <c>StatusCode == HttpStatusCode.NotFound</c>
    /// when "already deleted" is fine for you.
    /// </remarks>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
