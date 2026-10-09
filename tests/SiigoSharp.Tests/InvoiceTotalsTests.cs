namespace SiigoSharp.Tests;

public sealed class InvoiceTotalsTests
{
    private static readonly Tax Iva19 = new() { Id = 13156, Name = "IVA 19%", Type = TaxTypes.Iva, Percentage = 19 };
    private static readonly Tax Impoconsumo8 = new() { Id = 13157, Name = "Impoconsumo 8%", Type = TaxTypes.Impoconsumo, Percentage = 8 };
    private static readonly Tax Retefuente = new() { Id = 13158, Name = "Retefuente 2.5%", Type = TaxTypes.Retefuente, Percentage = 2.5m };
    private static readonly Tax[] Catalog = [Iva19, Impoconsumo8, Retefuente];

    private static readonly DocumentType ValueDiscounts = new() { Id = 24446, Name = "Factura electrónica", DiscountType = "Value" };
    private static readonly DocumentType PercentageDiscounts = new() { Id = 24447, Name = "Factura POS", DiscountType = "Percentage" };

    [Fact]
    public void Matches_the_example_of_Siigo_documentation_for_invalid_total_payments()
    {
        // Cantidad 10, ValorUnitario 100.00, Descuento 50.00 → base 950.00, IVA 180.50, total 1130.50.
        var totals = Calculate(Item(quantity: 10, price: 100.00m, discount: 50.00m, Iva19));

        Assert.Equal(new InvoiceTotals(Subtotal: 950.00m, Taxes: 180.50m, Total: 1130.50m), totals);
    }

    [Fact]
    public void Matches_the_payment_of_the_documented_invoice_example()
    {
        // 1 × 1069.77 with IVA 19% is paid with 1273.03 in Siigo's create-invoice example.
        var totals = Calculate(Item(quantity: 1, price: 1069.77m, discount: null, Iva19));

        Assert.Equal(1273.03m, totals.Total);
    }

    [Fact]
    public void Rounds_each_item_before_adding_it_up()
    {
        // Each item: base 0.33, IVA round(0.0627) = 0.06, total 0.39. Rounding only the sum would give 1.19.
        var items = Enumerable.Range(0, 3).Select(_ => Item(quantity: 1, price: 0.333m, discount: null, Iva19)).ToArray();

        Assert.Equal(1.17m, Calculate(items).Total);
    }

    [Fact]
    public void Rounds_half_away_from_zero()
    {
        Assert.Equal(0.13m, Calculate(Item(quantity: 1, price: 0.125m, discount: null)).Total);
    }

    [Fact]
    public void Adds_every_supported_tax_of_an_item()
    {
        var totals = Calculate(Item(quantity: 2, price: 10_000m, discount: null, Iva19, Impoconsumo8));

        Assert.Equal(new InvoiceTotals(Subtotal: 20_000m, Taxes: 5_400m, Total: 25_400m), totals);
    }

    [Theory]
    [InlineData(SiigoLanguage.Spanish, "El impuesto 999 no está en el catálogo de impuestos recibido.")]
    [InlineData(SiigoLanguage.English, "Tax 999 is not in the given tax catalog.")]
    public void Rejects_a_tax_missing_from_the_catalog(SiigoLanguage language, string expected)
    {
        var item = Item(quantity: 1, price: 100m, discount: null);
        item.Taxes = [new IdReference(999)];

        var exception = Assert.Throws<ArgumentException>(() => InvoiceTotals.Calculate([item], Catalog, ValueDiscounts, language));

        Assert.StartsWith(expected, exception.Message);
    }

    [Fact]
    public void Refuses_to_guess_the_effect_of_withholding_taxes()
    {
        Assert.Throws<NotSupportedException>(() => Calculate(Item(quantity: 1, price: 100m, discount: null, Retefuente)));
    }

    [Fact]
    public void Refuses_to_guess_percentage_discounts()
    {
        var discounted = Item(quantity: 1, price: 100m, discount: 10m, Iva19);

        var exception = Assert.Throws<NotSupportedException>(() =>
            InvoiceTotals.Calculate([discounted], Catalog, PercentageDiscounts, SiigoLanguage.English));

        Assert.Contains("Factura POS", exception.Message);
    }

    [Fact]
    public void Computes_items_without_discount_for_any_document_type()
    {
        var totals = InvoiceTotals.Calculate([Item(quantity: 1, price: 100m, discount: null, Iva19)], Catalog, PercentageDiscounts);

        Assert.Equal(119m, totals.Total);
    }

    private static InvoiceTotals Calculate(params InvoiceItemRequest[] items) =>
        InvoiceTotals.Calculate(items, Catalog, ValueDiscounts);

    private static InvoiceItemRequest Item(decimal quantity, decimal price, decimal? discount, params Tax[] taxes) => new()
    {
        Code = "ITEM",
        Quantity = quantity,
        Price = price,
        Discount = discount,
        Taxes = taxes.Select(tax => new IdReference(tax.Id)).ToList(),
    };
}
