using System.Globalization;

namespace SiigoSharp.Demo;

/// <summary>The texts of the demo in Spanish and English. The library's own logs follow the same language.</summary>
internal sealed record DemoText
{
    public static DemoText For(SiigoLanguage language) => language == SiigoLanguage.English ? English : Spanish;

    public required CultureInfo Culture { get; init; }

    public required string Subtitle { get; init; }

    public required string SimulatedMode { get; init; }

    public required string RealMode { get; init; }

    public required string MissingCredentials { get; init; }

    public required string ConfirmWrites { get; init; }

    public required string Cancelled { get; init; }

    public required string CatalogsStep { get; init; }

    public required string CustomerStep { get; init; }

    public required string ProductStep { get; init; }

    public required string InvoiceStep { get; init; }

    public required string PdfStep { get; init; }

    public required string LoadingCatalogs { get; init; }

    public required string FindingCustomer { get; init; }

    public required string FindingProduct { get; init; }

    public required string CreatingInvoice { get; init; }

    public required string DownloadingPdf { get; init; }

    public required string Field { get; init; }

    public required string Value { get; init; }

    public required string DocumentType { get; init; }

    public required string Tax { get; init; }

    public required string PaymentType { get; init; }

    public required string Seller { get; init; }

    public required string CustomerFound { get; init; }

    public required string CustomerCreated { get; init; }

    public required string ProductFound { get; init; }

    public required string ProductCreated { get; init; }

    public required string Invoice { get; init; }

    public required string Subtotal { get; init; }

    public required string Total { get; init; }

    public required string DianStatus { get; init; }

    public required string NotSentToDian { get; init; }

    public required string IdempotencyKey { get; init; }

    public required string PublicLink { get; init; }

    public required string PdfSaved { get; init; }

    public required string Done { get; init; }

    public required string HandledForYou { get; init; }

    public required string TokensRenewed { get; init; }

    public required string Retries { get; init; }

    public required string InvoicesOnServer { get; init; }

    public required string SiigoError { get; init; }

    public required string AuthenticationError { get; init; }

    public required string ConfigurationError { get; init; }

    public string Money(decimal value) => "$" + value.ToString(value % 1 == 0 ? "N0" : "N2", Culture);

    public static DemoText Spanish { get; } = new()
    {
        Culture = CultureInfo.GetCultureInfo("es-CO"),
        Subtitle = "Cliente .NET no oficial para la API de Siigo Nube",
        SimulatedMode = "Modo simulado: Siigo se emula en memoria, sin credenciales. Se provocan fallas reales "
            + "(token vencido, error 503, respuesta perdida y límite de peticiones) para ver cómo las resuelve la librería.",
        RealMode = "Modo real: conectado a {0} como {1}.",
        MissingCredentials = "Faltan credenciales. Configure Siigo:Username, Siigo:AccessKey y Siigo:PartnerId "
            + "con dotnet user-secrets o variables de entorno (Siigo__Username...).",
        ConfirmWrites = "Se crearán un cliente, un producto y una factura de prueba en su empresa de Siigo "
            + "(la factura NO se envía a la DIAN). Use una empresa de pruebas. ¿Continuar?",
        Cancelled = "Cancelado.",
        CatalogsStep = "1 · Catálogos",
        CustomerStep = "2 · Cliente",
        ProductStep = "3 · Producto",
        InvoiceStep = "4 · Factura de venta",
        PdfStep = "5 · PDF",
        LoadingCatalogs = "Consultando tipos de documento, impuestos, formas de pago y vendedores...",
        FindingCustomer = "Buscando el cliente por NIT...",
        FindingProduct = "Buscando el producto por código...",
        CreatingInvoice = "Creando la factura...",
        DownloadingPdf = "Descargando el PDF...",
        Field = "Dato",
        Value = "Valor",
        DocumentType = "Tipo de documento",
        Tax = "Impuesto",
        PaymentType = "Forma de pago",
        Seller = "Vendedor",
        CustomerFound = "Cliente encontrado",
        CustomerCreated = "Cliente creado",
        ProductFound = "Producto encontrado",
        ProductCreated = "Producto creado",
        Invoice = "Factura",
        Subtotal = "Subtotal",
        Total = "Total",
        DianStatus = "Estado DIAN",
        NotSentToDian = "no enviada (borrador)",
        IdempotencyKey = "Idempotency-Key",
        PublicLink = "Enlace público",
        PdfSaved = "PDF guardado en",
        Done = "Listo",
        HandledForYou = "Lo que SiigoSharp resolvió por usted:",
        TokensRenewed = "token(s) renovado(s) automáticamente",
        Retries = "reintento(s) ante errores temporales",
        InvoicesOnServer = "factura(s) en el servidor: la respuesta perdida no generó duplicados",
        SiigoError = "Siigo devolvió un error",
        AuthenticationError = "Revise Siigo:Username, Siigo:AccessKey y Siigo:PartnerId.",
        ConfigurationError = "Configuración incompleta",
    };

    public static DemoText English { get; } = new()
    {
        Culture = CultureInfo.GetCultureInfo("en-US"),
        Subtitle = "Unofficial .NET client for the Siigo Nube API",
        SimulatedMode = "Simulated mode: Siigo is emulated in memory, no credentials needed. Real failures are triggered "
            + "(expired token, 503 error, lost response and rate limit) to show how the library handles them.",
        RealMode = "Real mode: connected to {0} as {1}.",
        MissingCredentials = "Credentials are missing. Set Siigo:Username, Siigo:AccessKey and Siigo:PartnerId "
            + "with dotnet user-secrets or environment variables (Siigo__Username...).",
        ConfirmWrites = "A test customer, product and invoice will be created in your Siigo company "
            + "(the invoice is NOT sent to the DIAN). Use a test company. Continue?",
        Cancelled = "Cancelled.",
        CatalogsStep = "1 · Catalogs",
        CustomerStep = "2 · Customer",
        ProductStep = "3 · Product",
        InvoiceStep = "4 · Sales invoice",
        PdfStep = "5 · PDF",
        LoadingCatalogs = "Reading document types, taxes, payment methods and sellers...",
        FindingCustomer = "Looking up the customer by NIT...",
        FindingProduct = "Looking up the product by code...",
        CreatingInvoice = "Creating the invoice...",
        DownloadingPdf = "Downloading the PDF...",
        Field = "Field",
        Value = "Value",
        DocumentType = "Document type",
        Tax = "Tax",
        PaymentType = "Payment method",
        Seller = "Seller",
        CustomerFound = "Customer found",
        CustomerCreated = "Customer created",
        ProductFound = "Product found",
        ProductCreated = "Product created",
        Invoice = "Invoice",
        Subtotal = "Subtotal",
        Total = "Total",
        DianStatus = "DIAN status",
        NotSentToDian = "not sent (draft)",
        IdempotencyKey = "Idempotency-Key",
        PublicLink = "Public link",
        PdfSaved = "PDF saved to",
        Done = "Done",
        HandledForYou = "What SiigoSharp handled for you:",
        TokensRenewed = "token(s) renewed automatically",
        Retries = "retry(ies) after transient errors",
        InvoicesOnServer = "invoice(s) on the server: the lost response caused no duplicates",
        SiigoError = "Siigo returned an error",
        AuthenticationError = "Check Siigo:Username, Siigo:AccessKey and Siigo:PartnerId.",
        ConfigurationError = "Incomplete configuration",
    };
}
