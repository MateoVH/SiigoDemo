using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using SiigoSharp.Internal;
using SiigoSharp.Tests.Infrastructure;

namespace SiigoSharp.Tests;

/// <summary>The requests must match, field by field, the examples in Siigo's documentation.</summary>
public sealed class SerializationTests
{
    [Fact]
    public void Serializes_a_customer_like_the_documented_example()
    {
        AssertJson(
            """
            {
              "type": "Customer", "person_type": "Person", "id_type": "13", "identification": "13832081", "check_digit": "4",
              "name": ["Marcos", "Castillo"], "commercial_name": "Siigo", "branch_office": 0, "active": true, "vat_responsible": false,
              "fiscal_responsibilities": [{ "code": "R-99-PN" }],
              "address": { "address": "Cra. 18 #79A - 42", "city": { "country_code": "Co", "state_code": "19", "city_code": "19001" }, "postal_code": "110911" },
              "phones": [{ "indicative": "57", "number": "3006003345", "extension": "132" }],
              "contacts": [{ "first_name": "Marcos", "last_name": "Castillo", "email": "marcos.castillo@contacto.com",
                             "phone": { "indicative": "57", "number": "3006003345", "extension": "132" } }],
              "comments": "Comentarios", "related_users": { "seller_id": 629, "collector_id": 629 }
            }
            """,
            Fixtures.CustomerRequest());
    }

    [Fact]
    public void Serializes_a_product_like_the_documented_example()
    {
        var product = new ProductRequest
        {
            Code = "Item-1",
            Name = "Camiseta de algodón",
            AccountGroup = 1253,
            Type = ProductType.Product,
            StockControl = false,
            Active = true,
            TaxClassification = TaxClassification.Taxed,
            TaxIncluded = false,
            TaxConsumptionValue = 0,
            Taxes = [new IdReference(13156)],
            Prices = [ProductPrice.Create(12000)],
            Unit = "94",
            UnitLabel = "unidad",
            Reference = "REF1",
            Description = "Camiseta de algodón blanca",
            AdditionalFields = new ProductAdditionalFields { Barcode = "B0123", Brand = "Gef", Tariff = "151612", Model = "Loiry" },
        };

        AssertJson(
            """
            {
              "code": "Item-1", "name": "Camiseta de algodón", "account_group": 1253, "type": "Product", "stock_control": false,
              "active": true, "tax_classification": "Taxed", "tax_included": false, "tax_consumption_value": 0,
              "taxes": [{ "id": 13156 }],
              "prices": [{ "currency_code": "COP", "price_list": [{ "position": 1, "value": 12000 }] }],
              "unit": "94", "unit_label": "unidad", "reference": "REF1", "description": "Camiseta de algodón blanca",
              "additional_fields": { "barcode": "B0123", "brand": "Gef", "tariff": "151612", "model": "Loiry" }
            }
            """,
            product);
    }

    [Fact]
    public void Serializes_an_invoice_like_the_documented_example()
    {
        var invoice = Fixtures.InvoiceRequest();
        invoice.CostCenter = 235;
        invoice.Currency = new InvoiceCurrency { Code = "USD", ExchangeRate = 3825.03m };
        invoice.Observations = "Observaciones";
        invoice.SendToDian = true;
        invoice.SendByEmail = true;
        invoice.IdempotencyKey = "Pedido1234";

        AssertJson(
            """
            {
              "document": { "id": 24446 }, "date": "2023-12-15",
              "customer": { "identification": "13832081", "branch_office": 0 },
              "cost_center": 235, "currency": { "code": "USD", "exchange_rate": 3825.03 }, "seller": 629,
              "stamp": { "send": true }, "mail": { "send": true }, "observations": "Observaciones",
              "items": [{ "code": "Item-1", "description": "Camiseta de algodón", "quantity": 1, "price": 1069.77, "discount": 0,
                          "taxes": [{ "id": 13156 }] }],
              "payments": [{ "id": 5636, "value": 1273.03, "due_date": "2021-03-19" }]
            }
            """,
            invoice);
    }

    [Fact]
    public void Keeps_every_invoice_option_through_a_round_trip_with_any_serializer()
    {
        // E.g. an outbox that stores the request and sends it again after a restart.
        var request = Fixtures.InvoiceRequest();
        request.SendToDian = true;
        request.SendByEmail = true;
        request.IdempotencyKey = "Pedido42";

        var copy = JsonSerializer.Deserialize<InvoiceRequest>(JsonSerializer.Serialize(request))!;

        Assert.True(copy.SendToDian);
        Assert.True(copy.SendByEmail);
        Assert.Equal("Pedido42", copy.IdempotencyKey);
        Assert.Equal(1273.03m, Assert.Single(copy.Payments).Value);
    }

    [Fact]
    public void Does_not_send_the_invoice_to_the_DIAN_unless_asked()
    {
        var json = JsonSerializer.SerializeToNode(Fixtures.InvoiceRequest(), SiigoJson.Options)!;

        Assert.False((bool)json["stamp"]!["send"]!);
        Assert.False((bool)json["mail"]!["send"]!);
    }

    [Fact]
    public async Task Reads_a_customer_like_the_documented_example()
    {
        var customer = await GetAsync(Fixtures.CustomerJson, client => client.Customers.GetAsync("c-1", TestContext.Current.CancellationToken));

        Assert.Equal(PersonType.Person, customer.PersonType);
        Assert.Equal("Cédula de ciudadanía", customer.IdType?.Name);
        Assert.Equal("Marcos Castillo", customer.FullName);
        Assert.Equal("No responsable", Assert.Single(customer.FiscalResponsibilities).Name);
        Assert.Equal("Popayán", customer.Address?.City?.CityName);
        Assert.Equal("marcos.castillo@contacto.com", Assert.Single(customer.Contacts).Email);
        Assert.Equal(629, customer.RelatedUsers?.SellerId);
        Assert.Equal(new DateTimeOffset(2020, 6, 15, 3, 33, 17, TimeSpan.Zero), customer.Metadata?.Created);
        Assert.True(customer.ExtensionData?.ContainsKey("custom_fields"));
    }

    [Fact]
    public async Task Reads_a_product_like_the_documented_example()
    {
        var product = await GetAsync(Fixtures.ProductJson, client => client.Products.GetAsync("p-1", TestContext.Current.CancellationToken));

        Assert.Equal(ProductType.Product, product.Type);
        Assert.Equal(TaxClassification.Taxed, product.TaxClassification);
        Assert.Equal("Productos", product.AccountGroup?.Name);
        Assert.Equal(19m, Assert.Single(product.Taxes).Percentage);
        Assert.Equal(12000m, Assert.Single(Assert.Single(product.Prices).PriceList).Value);
        Assert.Equal("unidad", product.Unit?.Name);
        Assert.Equal("Bodega principal", Assert.Single(product.Warehouses).Name);
    }

    [Fact]
    public async Task Reads_an_invoice_like_the_documented_example()
    {
        var invoice = await GetAsync(Fixtures.InvoiceJson, client => client.Invoices.GetAsync("i-1", TestContext.Current.CancellationToken));

        Assert.Equal("FV-2-22", invoice.Name);
        Assert.Equal(new DateOnly(2023, 12, 15), invoice.Date);
        Assert.Equal(2546.05m, invoice.Total);
        Assert.Equal(InvoiceStampStatus.Accepted, invoice.Stamp?.Status);
        Assert.Equal("7eb8f1c2", invoice.Stamp?.Cufe);
        var item = Assert.Single(invoice.Items);
        Assert.Equal(13m, item.Discount?.Percentage);
        Assert.Equal(130m, item.Discount?.Value);
        Assert.Equal(406.51m, Assert.Single(item.Taxes).Value);
        Assert.Equal(new DateOnly(2021, 3, 19), Assert.Single(invoice.Payments).DueDate);
        Assert.Equal("https://documentview.siigo.com/document?data=abc", invoice.PublicUrl);
    }

    [Fact]
    public async Task Tolerates_values_and_shapes_this_version_does_not_know()
    {
        const string json = """
            {
              "id": "p-2", "code": "KIT-1", "type": "Kit", "tax_classification": "SomethingNew",
              "taxes": null, "prices": null, "warehouses": null,
              "metadata": { "created": "2026-10-09T10:00:00Z" }
            }
            """;

        var product = await GetAsync(json, client => client.Products.GetAsync("p-2", TestContext.Current.CancellationToken));

        Assert.Equal("KIT-1", product.Code);
        Assert.Null(product.Type);
        Assert.Null(product.TaxClassification);
        Assert.Empty(product.Taxes);
        Assert.Empty(product.Prices);
        Assert.Empty(product.Warehouses);
    }

    [Fact]
    public async Task Reads_an_item_discount_sent_as_a_plain_number()
    {
        const string json = """{ "id": "i-2", "date": "2026-10-09T00:00:00-05:00", "items": [{ "code": "A", "discount": 1500 }] }""";

        var invoice = await GetAsync(json, client => client.Invoices.GetAsync("i-2", TestContext.Current.CancellationToken));

        Assert.Equal(1500m, Assert.Single(invoice.Items).Discount?.Value);
        Assert.Equal(new DateOnly(2026, 10, 9), invoice.Date);
    }

    private static void AssertJson<T>(string expected, T request)
    {
        var actual = JsonSerializer.SerializeToNode(request, SiigoJson.Options);
        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(expected), actual),
            $"Expected:{Environment.NewLine}{JsonNode.Parse(expected)}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
    }

    private static async Task<T> GetAsync<T>(string json, Func<SiigoClient, Task<T>> get)
    {
        var siigo = new FakeSiigoHandler();
        foreach (var path in new[] { "/v1/customers/c-1", "/v1/products/p-1", "/v1/products/p-2", "/v1/invoices/i-1", "/v1/invoices/i-2" })
        {
            siigo.On(HttpMethod.Get, path, _ => FakeSiigoHandler.Json(HttpStatusCode.OK, json));
        }

        using var client = TestClient.Create(siigo);
        return await get(client);
    }
}
