using System.Text.Json.Serialization;

namespace SiigoSharp;

/// <summary>A postal address.</summary>
public sealed class Address
{
    /// <summary>The street address, e.g. <c>"Cra. 18 #79A - 42"</c> (Siigo field <c>address</c>).</summary>
    [JsonPropertyName("address")]
    public string? Street { get; set; }

    /// <summary>The city, identified by DANE codes.</summary>
    public City? City { get; set; }

    /// <summary>The postal code.</summary>
    public string? PostalCode { get; set; }
}

/// <summary>A city, identified by its country code and its DANE department and municipality codes.</summary>
public sealed class City
{
    /// <summary>The country code, e.g. <c>"Co"</c>.</summary>
    public string? CountryCode { get; set; }

    /// <summary>The country name. Filled in by Siigo in responses.</summary>
    public string? CountryName { get; set; }

    /// <summary>The DANE department code, e.g. <c>"11"</c> for Bogotá D.C.</summary>
    public string? StateCode { get; set; }

    /// <summary>The department name. Filled in by Siigo in responses.</summary>
    public string? StateName { get; set; }

    /// <summary>The DANE municipality code, e.g. <c>"11001"</c> for Bogotá.</summary>
    public string? CityCode { get; set; }

    /// <summary>The city name. Filled in by Siigo in responses.</summary>
    public string? CityName { get; set; }

    /// <summary>A Colombian city from its DANE codes, e.g. <c>City.Colombia("05", "05001")</c> for Medellín.</summary>
    public static City Colombia(string stateCode, string cityCode) =>
        new() { CountryCode = "Co", StateCode = stateCode, CityCode = cityCode };
}

/// <summary>A phone number.</summary>
public sealed class Phone
{
    /// <summary>The country calling code, e.g. <c>"57"</c>.</summary>
    public string? Indicative { get; set; }

    /// <summary>The number.</summary>
    public string? Number { get; set; }

    /// <summary>The extension, if any.</summary>
    public string? Extension { get; set; }
}

/// <summary>A contact person of a customer.</summary>
public sealed class Contact
{
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>The last name.</summary>
    public string? LastName { get; set; }

    /// <summary>The e-mail address. Siigo sends electronic invoices to it.</summary>
    public string? Email { get; set; }

    /// <summary>The phone number.</summary>
    public Phone? Phone { get; set; }
}
