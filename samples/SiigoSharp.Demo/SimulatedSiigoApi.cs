using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace SiigoSharp.Demo;

/// <summary>
/// An in-memory imitation of the Siigo API for running the demo without credentials. It follows the
/// documented request and response shapes, validates invoice payments like Siigo, and stages four
/// real-world failures, once each:
/// <list type="number">
/// <item>the access token expires early (401),</item>
/// <item>a read fails with a server error (503),</item>
/// <item>an invoice is created but the response is lost on the way back (502),</item>
/// <item>the rate limit is hit (429).</item>
/// </list>
/// </summary>
internal sealed class SimulatedSiigoApi : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly Dictionary<string, JsonObject> _customers = [];
    private readonly Dictionary<string, JsonObject> _products = [];
    private readonly Dictionary<string, JsonObject> _invoices = [];
    private readonly Dictionary<string, string> _invoiceIdsByIdempotencyKey = [];
    private string? _validToken;
    private int _tokensIssued;
    private int _nextInvoiceNumber = 1001;
    private bool _tokenExpired;
    private bool _productsFailed;
    private bool _invoiceResponseLost;
    private bool _rateLimited;

    public int InvoicesCreated
    {
        get
        {
            lock (_gate)
            {
                return _invoices.Count;
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(120, 320), cancellationToken); // Network latency.
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        lock (_gate)
        {
            var response = Handle(request, body);
            response.RequestMessage = request;
            return response;
        }
    }

    private HttpResponseMessage Handle(HttpRequestMessage request, string? body)
    {
        var path = request.RequestUri!.AbsolutePath;
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);

        if (!request.Headers.Contains("Partner-Id"))
        {
            return Error(HttpStatusCode.BadRequest, "header_required", "The Partner-Id header is required");
        }

        if (path == "/auth")
        {
            _validToken = $"simulated-token-{++_tokensIssued}";
            return Json(HttpStatusCode.Created, new JsonObject
            {
                ["access_token"] = _validToken,
                ["expires_in"] = 86400,
                ["token_type"] = "Bearer",
                ["scope"] = "SiigoAPI",
            });
        }

        if (request.Headers.Authorization?.Parameter is not { } token || token != _validToken)
        {
            return Error(HttpStatusCode.Unauthorized, "unauthorized", "The access token is not valid");
        }

        // Failure 1: once the catalogs have been read, the token expires before its time.
        if (path.StartsWith("/v1/customers", StringComparison.Ordinal) && !_tokenExpired)
        {
            _tokenExpired = true;
            _validToken = null;
            return Error(HttpStatusCode.Unauthorized, "unauthorized", "The access token has expired");
        }

        return (request.Method.Method, path) switch
        {
            ("GET", "/v1/document-types") => Json(HttpStatusCode.OK, Catalogs.DocumentTypes),
            ("GET", "/v1/taxes") => Json(HttpStatusCode.OK, Catalogs.Taxes),
            ("GET", "/v1/payment-types") => Json(HttpStatusCode.OK, Catalogs.PaymentTypes),
            ("GET", "/v1/users") => Json(HttpStatusCode.OK, Page(JsonNode.Parse(Catalogs.Users)!.AsArray())),
            ("GET", "/v1/account-groups") => Json(HttpStatusCode.OK, Catalogs.AccountGroups),
            ("GET", "/v1/customers") => Json(HttpStatusCode.OK, Page(Find(_customers, query["identification"]))),
            ("POST", "/v1/customers") => CreateCustomer(JsonNode.Parse(body!)!.AsObject()),
            ("GET", "/v1/products") => ListProducts(query),
            ("POST", "/v1/products") => CreateProduct(JsonNode.Parse(body!)!.AsObject()),
            ("POST", "/v1/invoices") => CreateInvoice(request, JsonNode.Parse(body!)!.AsObject()),
            ("GET", _) when path.StartsWith("/v1/invoices/", StringComparison.Ordinal) && path.EndsWith("/pdf", StringComparison.Ordinal)
                => Pdf(path.Split('/')[3]),
            _ => Error(HttpStatusCode.NotFound, "not_found", $"{request.Method} {path} is not simulated"),
        };
    }

    private HttpResponseMessage ListProducts(NameValueCollection query)
    {
        // Failure 2: a transient server error on a read.
        if (!_productsFailed)
        {
            _productsFailed = true;
            return Error(HttpStatusCode.ServiceUnavailable, "service_unavailable", "The service is temporarily unavailable");
        }

        return Json(HttpStatusCode.OK, Page(Find(_products, query["code"])));
    }

    private HttpResponseMessage CreateCustomer(JsonObject request)
    {
        var identification = (string)request["identification"]!;
        if (_customers.ContainsKey(identification))
        {
            return Error(HttpStatusCode.BadRequest, "already_exists", "The identification already exists", "identification");
        }

        var customer = request.DeepClone().AsObject();
        customer["id"] = Guid.NewGuid().ToString();
        customer["type"] ??= "Customer";
        customer["branch_office"] ??= 0;
        customer["check_digit"] ??= "8";
        customer["id_type"] = new JsonObject { ["code"] = (string?)request["id_type"], ["name"] = "NIT" };
        customer["fiscal_responsibilities"] = new JsonArray(new JsonObject { ["code"] = "R-99-PN", ["name"] = "No aplica - Otros" });
        customer["address"]!["city"]!["city_name"] = "Medellín";
        customer["metadata"] = Metadata();
        _customers[identification] = customer;
        return Json(HttpStatusCode.Created, customer);
    }

    private HttpResponseMessage CreateProduct(JsonObject request)
    {
        var code = (string)request["code"]!;
        if (_products.ContainsKey(code))
        {
            return Error(HttpStatusCode.BadRequest, "already_exists", "The code already exists", "code");
        }

        var taxes = JsonNode.Parse(Catalogs.Taxes)!.AsArray();
        var product = request.DeepClone().AsObject();
        product["id"] = Guid.NewGuid().ToString();
        product["account_group"] = new JsonObject { ["id"] = (int)request["account_group"]!, ["name"] = "Servicios" };
        product["taxes"] = new JsonArray(request["taxes"]!.AsArray()
            .Select(reference => taxes.First(tax => (int)tax!["id"]! == (int)reference!["id"]!)!.DeepClone())
            .ToArray());
        product["unit"] = new JsonObject { ["code"] = "94", ["name"] = "unidad" };
        product["metadata"] = Metadata();
        _products[code] = product;
        return Json(HttpStatusCode.Created, product);
    }

    private HttpResponseMessage CreateInvoice(HttpRequestMessage request, JsonObject invoiceRequest)
    {
        var idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null;

        // Like Siigo: the same Idempotency-Key returns the invoice created the first time.
        if (idempotencyKey is not null && _invoiceIdsByIdempotencyKey.TryGetValue(idempotencyKey, out var existingId))
        {
            return Json(HttpStatusCode.Created, _invoices[existingId]);
        }

        var identification = (string)invoiceRequest["customer"]!["identification"]!;
        if (!_customers.TryGetValue(identification, out var customer))
        {
            return Error(HttpStatusCode.BadRequest, "invalid_reference", "The customer doesn't exist", "customer.identification");
        }

        var items = new JsonArray();
        var total = 0m;
        foreach (var item in invoiceRequest["items"]!.AsArray())
        {
            var (node, itemTotal) = PriceItem(item!.AsObject());
            items.Add(node);
            total += itemTotal;
        }

        var paid = invoiceRequest["payments"]!.AsArray().Sum(payment => (decimal)payment!["value"]!);
        if (paid != total)
        {
            return Error(HttpStatusCode.BadRequest, "invalid_total_payments",
                $"The total payments ({paid}) must be equal to the total invoice ({total})", "payments");
        }

        var number = _nextInvoiceNumber++;
        var id = Guid.NewGuid().ToString();
        var invoice = new JsonObject
        {
            ["id"] = id,
            ["document"] = invoiceRequest["document"]!.DeepClone(),
            ["prefix"] = "FV",
            ["number"] = number,
            ["name"] = $"FV-1-{number}",
            ["date"] = invoiceRequest["date"]!.DeepClone(),
            ["customer"] = new JsonObject { ["id"] = customer["id"]!.DeepClone(), ["identification"] = identification, ["branch_office"] = 0 },
            ["total"] = total,
            ["balance"] = 0,
            ["seller"] = invoiceRequest["seller"]!.DeepClone(),
            ["stamp"] = new JsonObject { ["status"] = "Draft", ["cufe"] = null },
            ["mail"] = new JsonObject { ["status"] = "not_sent", ["observations"] = "" },
            ["observations"] = invoiceRequest["observations"]?.DeepClone(),
            ["items"] = items,
            ["payments"] = invoiceRequest["payments"]!.DeepClone(),
            ["public_url"] = $"https://siigosharp.example/simulado/{id}",
            ["metadata"] = Metadata(),
        };

        _invoices[id] = invoice;
        if (idempotencyKey is not null)
        {
            _invoiceIdsByIdempotencyKey[idempotencyKey] = id;
        }

        // Failure 3: the invoice was created, but the response never reaches the client.
        if (!_invoiceResponseLost)
        {
            _invoiceResponseLost = true;
            return new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>502 Bad Gateway</html>") };
        }

        return Json(HttpStatusCode.Created, invoice);
    }

    /// <summary>
    /// Siigo's documented formula, written independently of the library's InvoiceTotals:
    /// base = round(qty × price − discount, 2); tax = round(base × % / 100, 2); total = round(base + taxes, 2).
    /// </summary>
    private (JsonObject Item, decimal Total) PriceItem(JsonObject item)
    {
        var code = (string)item["code"]!;
        var product = _products[code];
        var quantity = (decimal)item["quantity"]!;
        var price = (decimal)item["price"]!;
        var discount = (decimal?)item["discount"] ?? 0;
        var itemBase = Round((quantity * price) - discount);

        var taxes = new JsonArray();
        var taxTotal = 0m;
        foreach (var reference in item["taxes"]?.AsArray() ?? new JsonArray())
        {
            var tax = JsonNode.Parse(Catalogs.Taxes)!.AsArray()
                .First(candidate => (int)candidate!["id"]! == (int)reference!["id"]!)!.AsObject();
            var value = Round(itemBase * (decimal)tax["percentage"]! / 100);
            taxTotal += value;
            var applied = tax.DeepClone().AsObject();
            applied["value"] = value;
            applied["base_value"] = itemBase;
            taxes.Add(applied);
        }

        var total = Round(itemBase + taxTotal);
        return (new JsonObject
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["code"] = code,
            ["description"] = item["description"]?.DeepClone() ?? product["name"]!.DeepClone(),
            ["quantity"] = quantity,
            ["price"] = price,
            ["discount"] = new JsonObject { ["percentage"] = 0, ["value"] = discount },
            ["taxes"] = taxes,
            ["total"] = total,
        }, total);
    }

    private HttpResponseMessage Pdf(string invoiceId)
    {
        // Failure 4: the rate limit (Siigo allows 10 requests per minute in test companies).
        if (!_rateLimited)
        {
            _rateLimited = true;
            return Error(HttpStatusCode.TooManyRequests, "requests_limit", "The request limit has been exceeded");
        }

        if (!_invoices.TryGetValue(invoiceId, out var invoice))
        {
            return Error(HttpStatusCode.NotFound, "not_found", "The invoice doesn't exist");
        }

        var es = CultureInfo.GetCultureInfo("es-CO");
        var customer = _customers[(string)invoice["customer"]!["identification"]!];
        var lines = new List<string>
        {
            $"FACTURA ELECTRÓNICA DE VENTA {invoice["name"]}",
            "Documento simulado por SiigoSharp - no es una factura válida / Simulated document",
            "",
            $"Cliente: {string.Join(" ", customer["name"]!.AsArray().Select(part => (string?)part))}",
            $"NIT: {customer["identification"]}-{customer["check_digit"]}",
            $"Fecha: {invoice["date"]}",
            "",
        };
        foreach (var item in invoice["items"]!.AsArray())
        {
            lines.Add($"{(decimal)item!["quantity"]!:0.##} x {item["description"]}  ${((decimal)item["total"]!).ToString("N0", es)}");
        }

        lines.Add("");
        lines.Add($"TOTAL: ${((decimal)invoice["total"]!).ToString("N0", es)}");
        lines.Add("Estado DIAN: borrador (no enviada)");

        return Json(HttpStatusCode.OK, new JsonObject
        {
            ["id"] = invoiceId,
            ["base64"] = Convert.ToBase64String(MiniPdf.Create(lines)),
        });
    }

    private static JsonArray Find(Dictionary<string, JsonObject> records, string? key) =>
        key is not null && records.TryGetValue(key, out var record) ? new JsonArray(record.DeepClone()) : new JsonArray();

    private static JsonObject Page(JsonArray results) => new()
    {
        ["pagination"] = new JsonObject { ["page"] = 1, ["page_size"] = 25, ["total_results"] = results.Count },
        ["results"] = results,
    };

    private static JsonObject Metadata() => new()
    {
        ["created"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        ["last_updated"] = null,
    };

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static HttpResponseMessage Json(HttpStatusCode status, JsonNode body) =>
        Json(status, body.ToJsonString());

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Error(HttpStatusCode status, string code, string message, params string[] parameters) =>
        Json(status, JsonSerializer.Serialize(new
        {
            Status = (int)status,
            Errors = new[] { new { Code = code, Message = message, Params = parameters, Detail = "Simulated by SiigoSharp" } },
        }));

    /// <summary>The catalogs of the simulated company, shaped like Siigo's responses.</summary>
    private static class Catalogs
    {
        public const string DocumentTypes = """
            [{ "id": 24446, "code": "1", "name": "Factura electrónica de venta", "type": "FV", "active": true,
               "seller_by_item": false, "cost_center": false, "cost_center_mandatory": false, "automatic_number": true,
               "consecutive": 1001, "discount_type": "Value", "decimals": true, "electronic_type": "ElectronicInvoice" }]
            """;

        public const string Taxes = """
            [{ "id": 13156, "name": "IVA 19%", "type": "IVA", "percentage": 19, "active": true },
             { "id": 13157, "name": "IVA 5%", "type": "IVA", "percentage": 5, "active": true },
             { "id": 13158, "name": "Impoconsumo 8%", "type": "Impoconsumo", "percentage": 8, "active": true },
             { "id": 13159, "name": "Retefuente 2.5%", "type": "Retefuente", "percentage": 2.5, "active": true }]
            """;

        public const string PaymentTypes = """
            [{ "id": 5636, "name": "Efectivo", "type": "Cartera", "active": true, "due_date": false },
             { "id": 5637, "name": "Crédito 30 días", "type": "Cartera", "active": true, "due_date": true }]
            """;

        public const string Users = """
            [{ "id": 629, "username": "ventas@simulada.example", "first_name": "Valentina", "last_name": "Ríos",
               "email": "ventas@simulada.example", "active": true, "identification": "1020304050" }]
            """;

        public const string AccountGroups = """
            [{ "id": 1253, "name": "Productos", "active": true }, { "id": 1254, "name": "Servicios", "active": true }]
            """;
    }
}
