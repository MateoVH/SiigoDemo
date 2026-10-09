using System.Net;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class CustomersTests
{
    [Fact]
    public async Task Finds_a_customer_by_identification_in_its_main_branch_office()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, $$"""
            { "pagination": { "page": 1, "page_size": 25, "total_results": 1 }, "results": [{{Fixtures.CustomerJson}}] }
            """));
        using var client = TestClient.Create(siigo);

        var customer = await client.Customers.FindByIdentificationAsync("13832081", TestContext.Current.CancellationToken);

        Assert.Equal("Marcos Castillo", customer?.FullName);
        Assert.Equal("?identification=13832081&branch_office=0", Assert.Single(siigo.RequestsTo("/v1/customers")).Query);
    }

    [Fact]
    public async Task Returns_null_when_only_other_identifications_or_branches_match()
    {
        // Whatever Siigo's matching rules, the result must be exactly the identification and branch asked for.
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, """
            { "pagination": { "page": 1, "page_size": 25, "total_results": 2 },
              "results": [{ "id": "a", "identification": "138320810", "branch_office": 0 },
                          { "id": "b", "identification": "13832081", "branch_office": 1 }] }
            """));
        using var client = TestClient.Create(siigo);

        Assert.Null(await client.Customers.FindByIdentificationAsync("13832081", TestContext.Current.CancellationToken));
        Assert.Equal("b", (await client.Customers.FindByIdentificationAsync("13832081", 1, TestContext.Current.CancellationToken))?.Id);
    }
}
