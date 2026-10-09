namespace SiigoSharp;

/// <summary>The kind of product (field <c>type</c>).</summary>
public enum ProductType
{
    /// <summary>A physical product (the default).</summary>
    Product,

    /// <summary>A service.</summary>
    Service,

    /// <summary>A consumer good.</summary>
    ConsumerGood,

    /// <summary>A combo of other products (Siigo Nube Premium only).</summary>
    Combo,
}

/// <summary>How VAT (IVA) applies to a product (field <c>tax_classification</c>).</summary>
public enum TaxClassification
{
    /// <summary>Gravado: VAT applies (the default).</summary>
    Taxed,

    /// <summary>Exento: VAT at 0%.</summary>
    Exempt,

    /// <summary>Excluido: outside VAT.</summary>
    Excluded,
}
