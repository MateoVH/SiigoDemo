// SiigoSharp demo: catalogs → customer → product → sales invoice → PDF.
//
//   dotnet run --project samples/SiigoSharp.Demo                 simulated Siigo, in Spanish
//   dotnet run --project samples/SiigoSharp.Demo -- --lang en    simulated Siigo, in English
//   dotnet run --project samples/SiigoSharp.Demo -- --real       your Siigo company (credentials from user-secrets)

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiigoSharp;
using SiigoSharp.Demo;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

var language = ArgumentAfter("--lang")?.Equals("en", StringComparison.OrdinalIgnoreCase) == true
    ? SiigoLanguage.English
    : SiigoLanguage.Spanish;
var real = args.Contains("--real");
var text = DemoText.For(language);

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
var cancellationToken = cancellation.Token;

AnsiConsole.Write(new FigletText("SiigoSharp").Color(Color.SpringGreen3));
AnsiConsole.MarkupLine($"[grey]{Markup.Escape(text.Subtitle)}[/]{Environment.NewLine}");

var options = new SiigoClientOptions { Language = language };
var simulator = real ? null : new SimulatedSiigoApi();

if (real)
{
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();
    configuration.GetSection("Siigo").Bind(options);
    options.Language = language;
    if (string.IsNullOrWhiteSpace(options.PartnerId))
    {
        options.PartnerId = "SiigoSharpDemo";
    }

    // Siigo test companies allow 10 requests per minute, so wait longer between retries.
    options.Retry.MaxRetries = 5;
    options.Retry.BaseDelay = TimeSpan.FromSeconds(5);
    options.Retry.MaxDelay = TimeSpan.FromSeconds(60);
    options.Timeout = TimeSpan.FromMinutes(4);

    if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.AccessKey))
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(text.MissingCredentials)}[/]");
        return 1;
    }

    AnsiConsole.MarkupLine(Markup.Escape(string.Format(text.RealMode, options.BaseAddress.Host, options.Username)));
}
else
{
    options.Username = "demo@siigosharp.example";
    options.AccessKey = "simulated";
    options.PartnerId = "SiigoSharpDemo";
    options.Retry.BaseDelay = TimeSpan.FromSeconds(1); // Keeps the simulated run short.
    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(text.SimulatedMode)}[/]");
}

var logs = new ConsoleLogProvider();
using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));

try
{
    using var siigo = new SiigoClient(options, simulator, loggerFactory);

    // 1. Catalogs: the ids that customers, products and invoices refer to.
    Section(text.CatalogsStep);
    var (documentType, iva, payment, seller, taxes) = await Status(text.LoadingCatalogs, async () =>
    {
        var documentTypes = await siigo.Catalogs.GetDocumentTypesAsync(DocumentTypeCodes.SalesInvoice, cancellationToken);
        var allTaxes = await siigo.Catalogs.GetTaxesAsync(cancellationToken);
        var paymentTypes = await siigo.Catalogs.GetPaymentTypesAsync(DocumentTypeCodes.SalesInvoice, cancellationToken);
        var users = await siigo.Catalogs.GetUsersAsync(cancellationToken);

        return (
            documentTypes.First(type => type.Active != false),
            allTaxes.First(tax => tax.Type == TaxTypes.Iva && tax.Percentage == 19),
            paymentTypes.FirstOrDefault(type => type.Active != false && type.DueDate != true) ?? paymentTypes.First(),
            users.First(user => user.Active != false),
            allTaxes);
    });

    AnsiConsole.Write(new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey)
        .AddColumns(text.Field, text.Value)
        .AddRow(text.DocumentType, Markup.Escape($"{documentType.Id} · {documentType.Name}"))
        .AddRow(text.Tax, Markup.Escape($"{iva.Id} · {iva.Name}"))
        .AddRow(text.PaymentType, Markup.Escape($"{payment.Id} · {payment.Name}"))
        .AddRow(text.Seller, Markup.Escape($"{seller.Id} · {seller.FullName}")));

    if (real && !AnsiConsole.Confirm(text.ConfirmWrites, defaultValue: false))
    {
        AnsiConsole.MarkupLine(text.Cancelled);
        return 0;
    }

    // 2. Customer: reuse it when it exists, create it otherwise.
    Section(text.CustomerStep);
    var (customer, customerCreated) = await Status(text.FindingCustomer, async () =>
    {
        var existing = await siigo.Customers.FindByIdentificationAsync(DemoData.CustomerNit, cancellationToken: cancellationToken);
        return existing is not null
            ? (existing, false)
            : (await siigo.Customers.CreateAsync(DemoData.Customer(), cancellationToken), true);
    });
    Done(customerCreated ? text.CustomerCreated : text.CustomerFound, $"{customer.FullName} · NIT {customer.Identification}");

    // 3. Product: the same, by code.
    Section(text.ProductStep);
    var (product, productCreated) = await Status(text.FindingProduct, async () =>
    {
        var existing = await siigo.Products.FindByCodeAsync(DemoData.ProductCode, cancellationToken);
        if (existing is not null)
        {
            return (existing, false);
        }

        var groups = await siigo.Catalogs.GetAccountGroupsAsync(cancellationToken);
        var group = groups.First(accountGroup => accountGroup.Active != false);
        return (await siigo.Products.CreateAsync(DemoData.Product(group.Id, iva.Id), cancellationToken), true);
    });
    Done(productCreated ? text.ProductCreated : text.ProductFound, $"{product.Code} · {product.Name}");

    // 4. Invoice: the payment must equal the total that Siigo computes, or it answers invalid_total_payments.
    Section(text.InvoiceStep);
    List<InvoiceItemRequest> items =
    [
        new() { Code = product.Code!, Quantity = 2, Price = DemoData.UnitPrice, Taxes = [new IdReference(iva.Id)] },
    ];
    var totals = InvoiceTotals.Calculate(items, taxes, documentType, language);
    var today = DateOnly.FromDateTime(DateTime.Now);

    var invoice = await Status(text.CreatingInvoice, () => siigo.Invoices.CreateAsync(
        new InvoiceRequest
        {
            Document = new IdReference(documentType.Id),
            Date = today,
            Customer = new InvoiceCustomerReference { Identification = customer.Identification! },
            Seller = seller.Id,
            Items = items,
            Payments = [new InvoicePaymentRequest { Id = payment.Id, Value = totals.Total, DueDate = payment.DueDate == true ? today.AddDays(30) : null }],
            Observations = "Factura de prueba creada con SiigoSharp",
            SendToDian = false,

            // Derived from your own order number, the key also protects retries made after a crash.
            IdempotencyKey = $"Pedido{DateTime.Now:yyyyMMddHHmmss}",
        },
        cancellationToken));

    AnsiConsole.Write(new Panel(new Rows(
            new Markup($"[bold]{text.Invoice}:[/] {Markup.Escape(invoice.Name ?? invoice.Id ?? "")}"),
            new Markup($"[bold]{text.Subtotal}:[/] {text.Money(totals.Subtotal)}   [bold]IVA:[/] {text.Money(totals.Taxes)}"),
            new Markup($"[bold]{text.Total}:[/] [green]{text.Money(invoice.Total ?? totals.Total)}[/]"),
            new Markup($"[bold]{text.DianStatus}:[/] {Markup.Escape(invoice.Stamp?.Status == InvoiceStampStatus.Draft ? text.NotSentToDian : invoice.Stamp?.Status ?? "-")}"),
            new Markup($"[bold]{text.PublicLink}:[/] {(invoice.PublicUrl is { } url ? $"[link]{Markup.Escape(url)}[/]" : "-")}")))
        .Header($" {Markup.Escape(text.Invoice)} ").Border(BoxBorder.Rounded).BorderColor(Color.SpringGreen3));

    // 5. PDF
    Section(text.PdfStep);
    var pdf = await Status(text.DownloadingPdf, () => siigo.Invoices.GetPdfAsync(invoice.Id!, cancellationToken));
    var pdfPath = Path.Combine(Path.GetTempPath(), "SiigoSharpDemo", $"{invoice.Name ?? invoice.Id}.pdf");
    Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
    await File.WriteAllBytesAsync(pdfPath, pdf, cancellationToken);
    AnsiConsole.MarkupLine($"   📄 {Markup.Escape(text.PdfSaved)} [link={new Uri(pdfPath).AbsoluteUri}]{Markup.Escape(pdfPath)}[/]");

    // What the library did on its own.
    Section($"✅ {text.Done}");
    AnsiConsole.MarkupLine($"   {Markup.Escape(text.HandledForYou)}");
    AnsiConsole.MarkupLine($"   🔑 [bold]{logs.TokensRenewed}[/] {Markup.Escape(text.TokensRenewed)}");
    AnsiConsole.MarkupLine($"   ⏳ [bold]{logs.Retries}[/] {Markup.Escape(text.Retries)}");
    if (simulator is not null)
    {
        AnsiConsole.MarkupLine($"   🧾 [bold]{simulator.InvoicesCreated}[/] {Markup.Escape(text.InvoicesOnServer)}");
    }

    return 0;
}
catch (SiigoAuthenticationException exception)
{
    Failure(text.SiigoError, $"{exception.Message}{Environment.NewLine}{text.AuthenticationError}");
}
catch (SiigoApiException exception)
{
    Failure(text.SiigoError, exception.Message);
}
catch (OptionsValidationException exception)
{
    Failure(text.ConfigurationError, string.Join(Environment.NewLine, exception.Failures));
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    AnsiConsole.MarkupLine(text.Cancelled);
}

return 1;

string? ArgumentAfter(string name) => args.SkipWhile(argument => argument != name).Skip(1).FirstOrDefault();

static void Section(string title)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule($"[bold]{Markup.Escape(title)}[/]") { Justification = Justify.Left, Style = Style.Parse("grey") });
}

static Task<T> Status<T>(string message, Func<Task<T>> action) =>
    AnsiConsole.Status().Spinner(Spinner.Known.Dots).SpinnerStyle(Style.Parse("springgreen3"))
        .StartAsync(Markup.Escape(message), _ => action());

static void Done(string label, string value) =>
    AnsiConsole.MarkupLine($"   ✅ {Markup.Escape(label)}: [bold]{Markup.Escape(value)}[/]");

static void Failure(string title, string message) =>
    AnsiConsole.Write(new Panel(Markup.Escape(message)).Header($" {Markup.Escape(title)} ").BorderColor(Color.Red));
