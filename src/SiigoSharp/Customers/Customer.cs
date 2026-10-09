using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiigoSharp;

/// <summary>A customer (or supplier, or other third party) as stored in Siigo.</summary>
public sealed class Customer
{
    /// <summary>The Siigo id (a GUID).</summary>
    public string? Id { get; init; }

    /// <summary>Customer, supplier or other.</summary>
    public CustomerType? Type { get; init; }

    /// <summary>Natural person or company.</summary>
    public PersonType? PersonType { get; init; }

    /// <summary>The identification type, e.g. code <c>"31"</c>, name <c>"NIT"</c>.</summary>
    public CodeName? IdType { get; init; }

    /// <summary>The identification number, without the verification digit.</summary>
    public string? Identification { get; init; }

    /// <summary>The NIT verification digit.</summary>
    public string? CheckDigit { get; init; }

    /// <summary>The company name (one element), or first names and last names (two elements).</summary>
    public IReadOnlyList<string> Name { get; init; } = [];

    /// <summary>The trade name.</summary>
    public string? CommercialName { get; init; }

    /// <summary>The branch office; 0 is the main one.</summary>
    public int? BranchOffice { get; init; }

    /// <summary>Whether the third party is active.</summary>
    public bool? Active { get; init; }

    /// <summary>Whether the third party is responsible for VAT (IVA).</summary>
    public bool? VatResponsible { get; init; }

    /// <summary>The DIAN fiscal responsibilities.</summary>
    public IReadOnlyList<FiscalResponsibility> FiscalResponsibilities { get; init; } = [];

    /// <summary>The address.</summary>
    public Address? Address { get; init; }

    /// <summary>The phone numbers.</summary>
    public IReadOnlyList<Phone> Phones { get; init; } = [];

    /// <summary>The contact people.</summary>
    public IReadOnlyList<Contact> Contacts { get; init; } = [];

    /// <summary>Free-text comments.</summary>
    public string? Comments { get; init; }

    /// <summary>The seller and the collector assigned to the third party.</summary>
    public RelatedUsers? RelatedUsers { get; init; }

    /// <summary>Creation and update dates.</summary>
    public ResourceMetadata? Metadata { get; init; }

    /// <summary>Fields returned by Siigo that this version of the library does not map.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }

    /// <summary>The name as a single string: the company name, or first and last names.</summary>
    [JsonIgnore]
    public string FullName => string.Join(" ", Name.Where(part => !string.IsNullOrWhiteSpace(part)));
}

/// <summary>The Siigo users related to a third party, by id (see <see cref="ICatalogsClient.GetUsersAsync"/>).</summary>
public sealed class RelatedUsers
{
    /// <summary>The seller.</summary>
    public int? SellerId { get; set; }

    /// <summary>The collector.</summary>
    public int? CollectorId { get; set; }
}
