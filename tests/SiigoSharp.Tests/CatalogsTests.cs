using System.Net;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class CatalogsTests
{
    [Fact]
    public async Task Filters_document_and_payment_types_by_document_class()
    {
        var siigo = new FakeSiigoHandler()
            .On(HttpMethod.Get, "/v1/document-types", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, """
                [{ "id": 24446, "code": "1", "name": "Factura electrónica", "type": "FV", "active": true,
                   "automatic_number": true, "consecutive": 22, "discount_type": "Value", "electronic_type": "ElectronicInvoice" }]
                """))
            .On(HttpMethod.Get, "/v1/payment-types", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, """
                [{ "id": 5636, "name": "Crédito", "type": "Cartera", "active": true, "due_date": true }]
                """));
        using var client = TestClient.Create(siigo);

        // The token can be passed positionally: these overloads default to sales invoices ("FV").
        var documentType = Assert.Single(await client.Catalogs.GetDocumentTypesAsync(TestContext.Current.CancellationToken));
        var paymentType = Assert.Single(await client.Catalogs.GetPaymentTypesAsync(TestContext.Current.CancellationToken));

        Assert.Equal(24446, documentType.Id);
        Assert.Equal("Value", documentType.DiscountType);
        Assert.True(paymentType.DueDate);
        Assert.Equal("?type=FV", siigo.RequestsTo("/v1/document-types")[0].Query);
        Assert.Equal("?document_type=FV", siigo.RequestsTo("/v1/payment-types")[0].Query);
    }

    [Fact]
    public async Task Reads_users_returned_as_a_plain_array()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/users", _ => FakeSiigoHandler.Json(HttpStatusCode.OK, """
            [{ "id": 629, "username": "vendedor@empresa.com", "first_name": "Ana", "last_name": "Ríos", "active": true }]
            """));
        using var client = TestClient.Create(siigo);

        var user = Assert.Single(await client.Catalogs.GetUsersAsync(TestContext.Current.CancellationToken));

        Assert.Equal(629, user.Id);
        Assert.Equal("Ana Ríos", user.FullName);
    }

    [Fact]
    public async Task Reads_every_page_of_users_returned_as_a_paginated_list()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/users", request => FakeSiigoHandler.Json(HttpStatusCode.OK,
            request.Query == "?page=2"
                ? """{ "pagination": { "page": 2, "page_size": 1, "total_results": 2 }, "results": [{ "id": 2 }] }"""
                : """{ "pagination": { "page": 1, "page_size": 1, "total_results": 2 }, "results": [{ "id": 1 }] }"""));
        using var client = TestClient.Create(siigo);

        var users = await client.Catalogs.GetUsersAsync(TestContext.Current.CancellationToken);

        Assert.Equal([1, 2], users.Select(user => user.Id));
    }

    [Fact]
    public async Task Stops_reading_users_even_if_the_server_ignores_the_page_parameter()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/users", _ => FakeSiigoHandler.Json(HttpStatusCode.OK,
            """{ "pagination": { "page": 1, "page_size": 1, "total_results": 2 }, "results": [{ "id": 1 }] }"""));
        using var client = TestClient.Create(siigo);

        await client.Catalogs.GetUsersAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, siigo.RequestsTo("/v1/users").Count);
    }
}
