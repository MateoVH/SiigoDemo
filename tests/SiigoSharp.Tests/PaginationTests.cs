using System.Net;
using System.Web;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class PaginationTests
{
    [Fact]
    public async Task ListAllAsync_reads_every_page()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", request =>
        {
            var query = HttpUtility.ParseQueryString(request.Query);
            var page = int.Parse(query["page"]!);
            var pageSize = int.Parse(query["page_size"]!);
            var results = Enumerable.Range(1, 5).Skip((page - 1) * pageSize).Take(pageSize).Select(n => new { id = $"c{n}" });
            return FakeSiigoHandler.Json(HttpStatusCode.OK, new
            {
                pagination = new { page, page_size = pageSize, total_results = 5 },
                results,
            });
        });
        using var client = TestClient.Create(siigo);

        var ids = new List<string?>();
        await foreach (var customer in client.Customers.ListAllAsync(new CustomerQuery { PageSize = 2 }, TestContext.Current.CancellationToken))
        {
            ids.Add(customer.Id);
        }

        Assert.Equal(["c1", "c2", "c3", "c4", "c5"], ids);
        Assert.Equal(
            ["?page=1&page_size=2", "?page=2&page_size=2", "?page=3&page_size=2"],
            siigo.RequestsTo("/v1/customers").Select(request => request.Query));
    }

    [Fact]
    public async Task ListAllAsync_stops_even_if_the_server_ignores_the_page_parameter()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/customers", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, """
            { "pagination": { "page": 1, "page_size": 2, "total_results": 5 }, "results": [{ "id": "c1" }, { "id": "c2" }] }
            """));
        using var client = TestClient.Create(siigo);

        var read = 0;
        await foreach (var _ in client.Customers.ListAllAsync(new CustomerQuery { PageSize = 2 }, TestContext.Current.CancellationToken))
        {
            read++;
        }

        // Pages 1, 2 and 3 cover the 5 announced results; the loop ends there.
        Assert.Equal(3, siigo.RequestsTo("/v1/customers").Count);
        Assert.Equal(6, read);
    }

    [Fact]
    public async Task ListAsync_sends_only_the_filters_that_are_set()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/invoices", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, """{"pagination":{"page":1,"page_size":25,"total_results":0},"results":[]}"""));
        using var client = TestClient.Create(siigo);

        await client.Invoices.ListAsync(
            new InvoiceQuery
            {
                CustomerIdentification = "900123456",
                DateStart = new DateOnly(2026, 10, 1),
                CreatedStart = new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.FromHours(-5)),
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "?customer_identification=900123456&date_start=2026-10-01&created_start=2026-10-01T13%3A30%3A00Z",
            Assert.Single(siigo.RequestsTo("/v1/invoices")).Query);
    }

    [Theory]
    [InlineData(1, 25, 60, true)]
    [InlineData(3, 25, 60, false)]
    [InlineData(1, 25, 25, false)]
    [InlineData(1, 25, 0, false)]
    public void HasNextPage_follows_the_pagination(int page, int pageSize, int total, bool expected)
    {
        var result = new SiigoPage<string>
        {
            Pagination = new SiigoPagination { Page = page, PageSize = pageSize, TotalResults = total },
            Results = total == 0 ? [] : ["x"],
        };

        Assert.Equal(expected, result.HasNextPage);
    }
}
