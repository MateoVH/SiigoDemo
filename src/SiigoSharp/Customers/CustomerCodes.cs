namespace SiigoSharp;

/// <summary>The role of a third party in Siigo (field <c>type</c>).</summary>
public enum CustomerType
{
    /// <summary>A customer (the default).</summary>
    Customer,

    /// <summary>A supplier.</summary>
    Supplier,

    /// <summary>Another kind of third party.</summary>
    Other,
}

/// <summary>Whether a third party is a natural person or a company (field <c>person_type</c>).</summary>
public enum PersonType
{
    /// <summary>A natural person; <c>name</c> holds first names and last names.</summary>
    Person,

    /// <summary>A company; <c>name</c> holds the company name.</summary>
    Company,
}

/// <summary>
/// DIAN identification type codes accepted by Siigo (field <c>id_type</c>). Names are kept in Spanish
/// because they are Colombian legal documents.
/// </summary>
public static class IdTypes
{
    /// <summary>Registro civil (11).</summary>
    public const string RegistroCivil = "11";

    /// <summary>Tarjeta de identidad (12).</summary>
    public const string TarjetaIdentidad = "12";

    /// <summary>Cédula de ciudadanía (13).</summary>
    public const string CedulaCiudadania = "13";

    /// <summary>Tarjeta de extranjería (21).</summary>
    public const string TarjetaExtranjeria = "21";

    /// <summary>Cédula de extranjería (22).</summary>
    public const string CedulaExtranjeria = "22";

    /// <summary>NIT (31).</summary>
    public const string Nit = "31";

    /// <summary>Pasaporte (41).</summary>
    public const string Pasaporte = "41";

    /// <summary>Documento de identificación extranjero (42).</summary>
    public const string DocumentoExtranjero = "42";

    /// <summary>Sin identificación del exterior o para uso definido por la DIAN (43).</summary>
    public const string SinIdentificacionDelExterior = "43";

    /// <summary>Permiso especial de permanencia, PEP (47).</summary>
    public const string Pep = "47";

    /// <summary>Permiso por protección temporal, PPT (48).</summary>
    public const string Ppt = "48";

    /// <summary>Salvoconducto de permanencia (89).</summary>
    public const string Salvoconducto = "89";

    /// <summary>NIT de otro país (50).</summary>
    public const string NitOtroPais = "50";

    /// <summary>NUIP (91).</summary>
    public const string Nuip = "91";
}

/// <summary>
/// A DIAN fiscal responsibility of a third party (field <c>fiscal_responsibilities</c>).
/// Use the predefined values, e.g. <c>FiscalResponsibility.NoResponsable</c>.
/// </summary>
public sealed class FiscalResponsibility
{
    /// <summary>The DIAN code, e.g. <c>"R-99-PN"</c>.</summary>
    public string? Code { get; init; }

    /// <summary>The description. Filled in by Siigo in responses.</summary>
    public string? Name { get; init; }

    /// <summary>R-99-PN: no responsibility applies ("No aplica – Otros"), Siigo's default.</summary>
    public static FiscalResponsibility NoResponsable => new() { Code = "R-99-PN" };

    /// <summary>O-13: gran contribuyente.</summary>
    public static FiscalResponsibility GranContribuyente => new() { Code = "O-13" };

    /// <summary>O-15: autorretenedor.</summary>
    public static FiscalResponsibility Autorretenedor => new() { Code = "O-15" };

    /// <summary>O-23: agente de retención IVA.</summary>
    public static FiscalResponsibility AgenteRetencionIva => new() { Code = "O-23" };

    /// <summary>O-47: régimen simple de tributación.</summary>
    public static FiscalResponsibility RegimenSimple => new() { Code = "O-47" };

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Name}".Trim();
}
