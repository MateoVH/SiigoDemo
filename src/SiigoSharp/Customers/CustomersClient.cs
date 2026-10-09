using SiigoSharp.Internal;

namespace SiigoSharp;

internal sealed class CustomersClient(SiigoConnection connection) : ICustomersClient
{
    private const string Path = "v1/customers";

    public Task<SiigoPage<Customer>> ListAsync(CustomerQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new CustomerQuery();
        var uri = new QueryStringBuilder()
            .Add("identification", query.Identification)
            .Add("branch_office", query.BranchOffice)
            .Add("created_start", query.CreatedStart)
            .Add("created_end", query.CreatedEnd)
            .Add("updated_start", query.UpdatedStart)
            .Add("updated_end", query.UpdatedEnd)
            .AddPaging(query)
            .AppendTo(Path);

        return connection.GetAsync<SiigoPage<Customer>>(uri, cancellationToken);
    }

    public IAsyncEnumerable<Customer> ListAllAsync(CustomerQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new CustomerQuery();
        return Paginator.EnumerateAsync(
            query.Page ?? 1, (page, token) => ListAsync(query with { Page = page }, token), cancellationToken);
    }

    public Task<Customer> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return connection.GetAsync<Customer>($"{Path}/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public async Task<Customer?> FindByIdentificationAsync(
        string identification, int branchOffice, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identification);

        var page = await ListAsync(
                new CustomerQuery { Identification = identification, BranchOffice = branchOffice }, cancellationToken)
            .ConfigureAwait(false);

        // The filter is applied by Siigo; matching again keeps the result exact whatever its matching rules.
        return page.Results.FirstOrDefault(customer =>
            customer.Identification == identification && (customer.BranchOffice ?? 0) == branchOffice);
    }

    public Task<Customer> CreateAsync(CustomerRequest customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return connection.PostAsync<CustomerRequest, Customer>(Path, customer, cancellationToken);
    }

    public Task<Customer> UpdateAsync(string id, CustomerRequest customer, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(customer);
        return connection.PutAsync<CustomerRequest, Customer>($"{Path}/{Uri.EscapeDataString(id)}", customer, cancellationToken);
    }
}
