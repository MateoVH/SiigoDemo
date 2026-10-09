using System.Net;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

public sealed class InvoicesTests
{
    [Fact]
    public async Task Creates_an_invoice_with_a_generated_idempotency_key()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/invoices", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.InvoiceJson));
        using var client = TestClient.Create(siigo);

        var invoice = await client.Invoices.CreateAsync(Fixtures.InvoiceRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("FV-2-22", invoice.Name);
        var key = Assert.Single(siigo.RequestsTo("/v1/invoices")).IdempotencyKey;
        Assert.NotNull(key);
        Assert.Equal(30, key.Length);
        Assert.True(key.All(char.IsAsciiLetterOrDigit));
    }

    [Fact]
    public async Task Generates_a_new_key_for_each_invoice()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/invoices", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.InvoiceJson));
        using var client = TestClient.Create(siigo);
        var request = Fixtures.InvoiceRequest();

        await client.Invoices.CreateAsync(request, TestContext.Current.CancellationToken);
        await client.Invoices.CreateAsync(request, TestContext.Current.CancellationToken);

        var keys = siigo.RequestsTo("/v1/invoices").Select(attempt => attempt.IdempotencyKey).ToList();
        Assert.NotEqual(keys[0], keys[1]);
    }

    [Fact]
    public async Task Sends_the_idempotency_key_chosen_by_the_caller()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/invoices", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.Created, Fixtures.InvoiceJson));
        using var client = TestClient.Create(siigo);
        var request = Fixtures.InvoiceRequest();
        request.IdempotencyKey = "Pedido20261009001";

        await client.Invoices.CreateAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("Pedido20261009001", Assert.Single(siigo.RequestsTo("/v1/invoices")).IdempotencyKey);
    }

    [Theory]
    [InlineData("pedido-123")]
    [InlineData("pedido 123")]
    [InlineData("1234567890123456789012345678901")]
    public async Task Rejects_an_idempotency_key_Siigo_would_refuse_before_sending_it(string key)
    {
        var siigo = new FakeSiigoHandler();
        using var client = TestClient.Create(siigo);
        var request = Fixtures.InvoiceRequest();
        request.IdempotencyKey = key;

        await Assert.ThrowsAsync<ArgumentException>(() => client.Invoices.CreateAsync(request, TestContext.Current.CancellationToken));

        Assert.Empty(siigo.Requests);
    }

    [Fact]
    public async Task Downloads_the_PDF()
    {
        var pdf = "%PDF-1.4 factura"u8.ToArray();
        var siigo = new FakeSiigoHandler().On(HttpMethod.Get, "/v1/invoices/i-1/pdf", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, new { id = "i-1", base64 = Convert.ToBase64String(pdf) }));
        using var client = TestClient.Create(siigo);

        var content = await client.Invoices.GetPdfAsync("i-1", TestContext.Current.CancellationToken);

        Assert.Equal(pdf, content);
    }

    [Fact]
    public async Task Sends_an_invoice_by_email()
    {
        var siigo = new FakeSiigoHandler().On(HttpMethod.Post, "/v1/invoices/i-1/mail", _ =>
            FakeSiigoHandler.Json(HttpStatusCode.OK, new { status = "sent", observations = "" }));
        using var client = TestClient.Create(siigo);

        var mail = await client.Invoices.SendByEmailAsync(
            "i-1", "facturas@cliente.example", "contabilidad@cliente.example", TestContext.Current.CancellationToken);
        await client.Invoices.SendByEmailAsync("i-1", "facturas@cliente.example", TestContext.Current.CancellationToken);

        Assert.Equal("sent", mail.Status);
        var requests = siigo.RequestsTo("/v1/invoices/i-1/mail");
        Assert.Equal("facturas@cliente.example", (string?)requests[0].BodyJson["mail_to"]);
        Assert.Equal("contabilidad@cliente.example", (string?)requests[0].BodyJson["copy_to"]);
        Assert.Null(requests[1].BodyJson["copy_to"]);
    }
}
