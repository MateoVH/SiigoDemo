namespace SiigoSharp;

/// <summary>
/// The data to create a customer, or to replace one with <see cref="ICustomersClient.UpdateAsync"/>.
/// </summary>
/// <example>
/// <code>
/// var request = new CustomerRequest
/// {
///     PersonType = PersonType.Company,
///     IdType = IdTypes.Nit,
///     Identification = "900123456",
///     Name = ["Ferretería El Tornillo S.A.S."],
///     FiscalResponsibilities = [FiscalResponsibility.NoResponsable],
///     Address = new Address { Street = "Cra. 43A #1-50", City = City.Colombia("05", "05001") },
///     Contacts = [new Contact { FirstName = "Laura", LastName = "Gómez", Email = "facturas@eltornillo.co" }],
/// };
/// </code>
/// </example>
public sealed class CustomerRequest
{
    /// <summary>Customer (Siigo's default), supplier or other.</summary>
    public CustomerType? Type { get; set; }

    /// <summary>Natural person or company.</summary>
    public required PersonType PersonType { get; set; }

    /// <summary>The identification type code; see <see cref="IdTypes"/>.</summary>
    public required string IdType { get; set; }

    /// <summary>The identification number, without the verification digit.</summary>
    public required string Identification { get; set; }

    /// <summary>The NIT verification digit. Siigo computes it when it is omitted.</summary>
    public string? CheckDigit { get; set; }

    /// <summary>
    /// A company sends its name as a single element; a person sends first names and last names as two
    /// elements. Up to 100 characters each.
    /// </summary>
    public required IList<string> Name { get; set; }

    /// <summary>The trade name.</summary>
    public string? CommercialName { get; set; }

    /// <summary>The branch office, from 0 (the main one, Siigo's default) to 999.</summary>
    public int? BranchOffice { get; set; }

    /// <summary>Whether the third party is active.</summary>
    public bool? Active { get; set; }

    /// <summary>Whether the third party is responsible for VAT (IVA).</summary>
    public bool? VatResponsible { get; set; }

    /// <summary>
    /// The DIAN fiscal responsibilities; Siigo expects at least one, e.g.
    /// <see cref="FiscalResponsibility.NoResponsable"/>.
    /// </summary>
    public IList<FiscalResponsibility>? FiscalResponsibilities { get; set; }

    /// <summary>The address. Siigo expects the street and the city codes.</summary>
    public Address? Address { get; set; }

    /// <summary>The phone numbers.</summary>
    public IList<Phone>? Phones { get; set; }

    /// <summary>The contact people. Electronic invoices are e-mailed to them.</summary>
    public IList<Contact>? Contacts { get; set; }

    /// <summary>Free-text comments.</summary>
    public string? Comments { get; set; }

    /// <summary>The seller and the collector assigned to the third party.</summary>
    public RelatedUsers? RelatedUsers { get; set; }
}
