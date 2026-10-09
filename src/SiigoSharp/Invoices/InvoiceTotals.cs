using SiigoSharp.Internal;

namespace SiigoSharp;

/// <summary>
/// The totals of an invoice, computed the way Siigo computes them. Use <see cref="Total"/> as the value
/// of the payments so that Siigo does not reject the invoice with <c>invalid_total_payments</c>.
/// </summary>
/// <param name="Subtotal">The sum of the item bases: quantity × price − discount.</param>
/// <param name="Taxes">The sum of the item taxes.</param>
/// <param name="Total">The invoice total: the sum of the item totals.</param>
/// <remarks>
/// <para>For each item, as documented by Siigo:</para>
/// <code>
/// base  = round(quantity × price − discount, 2)
/// tax   = round(base × percentage / 100, 2)    // for each tax of the item
/// total = round(base + taxes, 2)
/// </code>
/// <para>
/// Rounding is half away from zero. Supported: percentage taxes of type IVA and Impoconsumo, and
/// discounts expressed as values (Siigo does not document the rounding of percentage discounts, so they
/// are refused rather than guessed). Withholdings do not change the item totals.
/// </para>
/// </remarks>
public sealed record InvoiceTotals(decimal Subtotal, decimal Taxes, decimal Total)
{
    /// <summary>Computes the totals of the items of an invoice.</summary>
    /// <param name="items">The items of the invoice.</param>
    /// <param name="taxes">The tax catalog of the company (<see cref="ICatalogsClient.GetTaxesAsync"/>).</param>
    /// <param name="documentType">The document type of the invoice; it says how item discounts are expressed.</param>
    /// <param name="language">The language of the exception messages.</param>
    /// <exception cref="ArgumentException">An item refers to a tax that is not in <paramref name="taxes"/>.</exception>
    /// <exception cref="NotSupportedException">
    /// An item has a tax other than IVA or Impoconsumo, or a discount while the document type expresses
    /// discounts as percentages.
    /// </exception>
    public static InvoiceTotals Calculate(
        IEnumerable<InvoiceItemRequest> items,
        IEnumerable<Tax> taxes,
        DocumentType documentType,
        SiigoLanguage language = SiigoLanguage.Spanish)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(taxes);
        ArgumentNullException.ThrowIfNull(documentType);

        var messages = new Messages(language);
        var catalog = taxes.GroupBy(tax => tax.Id).ToDictionary(group => group.Key, group => group.First());
        var percentageDiscounts = string.Equals(documentType.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase);

        decimal subtotal = 0, taxTotal = 0, total = 0;
        foreach (var item in items)
        {
            if (percentageDiscounts && item.Discount is not (null or 0))
            {
                throw new NotSupportedException(messages.PercentageDiscountsNotSupported(documentType.Name));
            }

            var itemBase = Round((item.Quantity * item.Price) - (item.Discount ?? 0));

            var itemTaxes = 0m;
            foreach (var reference in item.Taxes ?? [])
            {
                if (!catalog.TryGetValue(reference.Id, out var tax))
                {
                    throw new ArgumentException(messages.TaxNotInCatalog(reference.Id), nameof(taxes));
                }

                if (!IsSupported(tax))
                {
                    throw new NotSupportedException(messages.TaxNotSupportedForTotals(tax.Name, tax.Type));
                }

                itemTaxes += Round(itemBase * (tax.Percentage ?? 0) / 100);
            }

            subtotal += itemBase;
            taxTotal += itemTaxes;
            total += Round(itemBase + itemTaxes);
        }

        return new InvoiceTotals(subtotal, taxTotal, total);
    }

    private static bool IsSupported(Tax tax) =>
        string.Equals(tax.Type, TaxTypes.Iva, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(tax.Type, TaxTypes.Impoconsumo, StringComparison.OrdinalIgnoreCase);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
