namespace SiigoSharp;

/// <summary>Operations on customers and other third parties (<c>/v1/customers</c>).</summary>
public interface ICustomersClient
{
    /// <summary>Reads one page of customers.</summary>
    Task<SiigoPage<Customer>> ListAsync(CustomerQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads every customer that matches the filters, requesting the pages as they are consumed.</summary>
    /// <example>
    /// <code>
    /// await foreach (var customer in siigo.Customers.ListAllAsync(new CustomerQuery { UpdatedStart = lastSync }))
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    IAsyncEnumerable<Customer> ListAllAsync(CustomerQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Reads a customer by its Siigo id.</summary>
    /// <exception cref="SiigoApiException">Siigo answered with an error, e.g. 404 when the id does not exist.</exception>
    Task<Customer> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Finds a customer (its main branch office) by identification number, or returns <c>null</c>.</summary>
    /// <param name="identification">The identification number, without the verification digit.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task<Customer?> FindByIdentificationAsync(string identification, CancellationToken cancellationToken = default) =>
        FindByIdentificationAsync(identification, branchOffice: 0, cancellationToken);

    /// <summary>Finds a customer by identification number and branch office, or returns <c>null</c>.</summary>
    /// <param name="identification">The identification number, without the verification digit.</param>
    /// <param name="branchOffice">The branch office; 0 is the main one.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    Task<Customer?> FindByIdentificationAsync(string identification, int branchOffice, CancellationToken cancellationToken = default);

    /// <summary>Creates a customer.</summary>
    /// <exception cref="SiigoApiException">
    /// Siigo rejected the data, e.g. with <c>already_exists</c> when the identification is registered.
    /// </exception>
    Task<Customer> CreateAsync(CustomerRequest customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a customer. It is a full replacement: fields left empty are emptied in Siigo, so start
    /// from the complete data of the customer.
    /// </summary>
    Task<Customer> UpdateAsync(string id, CustomerRequest customer, CancellationToken cancellationToken = default);
}
