namespace SiigoSharp.Demo;

/// <summary>The customer and the product the demo invoices. They are reused when they already exist.</summary>
internal static class DemoData
{
    public const string CustomerNit = "900123456";

    public const string ProductCode = "SIIGOSHARP-01";

    public const decimal UnitPrice = 450_000m;

    public static CustomerRequest Customer() => new()
    {
        PersonType = PersonType.Company,
        IdType = IdTypes.Nit,
        Identification = CustomerNit,
        Name = ["Ferretería El Tornillo SAS"],
        VatResponsible = true,
        FiscalResponsibilities = [FiscalResponsibility.NoResponsable],
        Address = new Address { Street = "Carrera 43A # 1-50", City = City.Colombia("05", "05001") },
        Phones = [new Phone { Indicative = "57", Number = "6044440000" }],
        Contacts = [new Contact { FirstName = "Laura", LastName = "Gómez", Email = "facturas@eltornillo.example" }],
        Comments = "Cliente de prueba creado por SiigoSharp",
    };

    public static ProductRequest Product(int accountGroupId, int taxId) => new()
    {
        Code = ProductCode,
        Name = "Licencia anual de software de inventario",
        AccountGroup = accountGroupId,
        Type = ProductType.Service,
        TaxClassification = TaxClassification.Taxed,
        Taxes = [new IdReference(taxId)],
        Prices = [ProductPrice.Create(UnitPrice)],
    };
}
