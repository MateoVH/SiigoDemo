using SiigoSharp.Internal;

namespace SiigoSharp;

internal sealed class ProductsClient(SiigoConnection connection) : IProductsClient
{
    private const string Path = "v1/products";

    public Task<SiigoPage<Product>> ListAsync(ProductQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new ProductQuery();
        var uri = new QueryStringBuilder()
            .Add("code", query.Code)
            .Add("created_start", query.CreatedStart)
            .Add("created_end", query.CreatedEnd)
            .Add("updated_start", query.UpdatedStart)
            .Add("updated_end", query.UpdatedEnd)
            .AddPaging(query)
            .AppendTo(Path);

        return connection.GetAsync<SiigoPage<Product>>(uri, cancellationToken);
    }

    public IAsyncEnumerable<Product> ListAllAsync(ProductQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new ProductQuery();
        return Paginator.EnumerateAsync(
            query.Page ?? 1, (page, token) => ListAsync(query with { Page = page }, token), cancellationToken);
    }

    public Task<Product> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return connection.GetAsync<Product>($"{Path}/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public async Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var page = await ListAsync(new ProductQuery { Code = code }, cancellationToken).ConfigureAwait(false);
        return page.Results.FirstOrDefault(product => product.Code == code);
    }

    public Task<Product> CreateAsync(ProductRequest product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        return connection.PostAsync<ProductRequest, Product>(Path, product, cancellationToken);
    }

    public Task<Product> UpdateAsync(string id, ProductRequest product, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(product);
        return connection.PutAsync<ProductRequest, Product>($"{Path}/{Uri.EscapeDataString(id)}", product, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return connection.DeleteAsync($"{Path}/{Uri.EscapeDataString(id)}", cancellationToken);
    }
}
