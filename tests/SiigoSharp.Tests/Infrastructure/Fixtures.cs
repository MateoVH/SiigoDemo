namespace SiigoSharp.Tests.Infrastructure;

/// <summary>Requests and responses taken from the examples in Siigo's API documentation.</summary>
internal static class Fixtures
{
    public const string CustomerJson = """
        {
          "id": "6b6ceb28-0e58-4f0d-bd6e-e2f3a3f0b7b1",
          "type": "Customer",
          "person_type": "Person",
          "id_type": { "code": "13", "name": "Cédula de ciudadanía" },
          "identification": "13832081",
          "branch_office": 0,
          "check_digit": "4",
          "name": ["Marcos", "Castillo"],
          "commercial_name": "Siigo",
          "active": true,
          "vat_responsible": false,
          "fiscal_responsibilities": [{ "code": "R-99-PN", "name": "No responsable" }],
          "address": {
            "address": "Cra. 18 #79A - 42",
            "city": { "country_code": "Co", "country_name": "Colombia", "state_code": "19", "state_name": "Cauca", "city_code": "19001", "city_name": "Popayán" },
            "postal_code": "110911"
          },
          "phones": [{ "indicative": "57", "number": "3006003345", "extension": "132" }],
          "contacts": [{
            "first_name": "Marcos", "last_name": "Castillo", "email": "marcos.castillo@contacto.com",
            "phone": { "indicative": "57", "number": "3006003345", "extension": "132" }
          }],
          "comments": "Comentarios",
          "related_users": { "seller_id": 629, "collector_id": 629 },
          "custom_fields": [{ "key": "CUCON", "value": "X" }],
          "metadata": { "created": "2020-06-15T03:33:17.0000000+00:00", "last_updated": null }
        }
        """;

    public const string ProductJson = """
        {
          "id": "c7b9b2a4-6f1e-4b3a-9d55-0d0b8d3f1a10",
          "code": "Item-1",
          "name": "Camiseta de algodón",
          "account_group": { "id": 1253, "name": "Productos" },
          "type": "Product",
          "stock_control": false,
          "active": true,
          "tax_classification": "Taxed",
          "tax_included": false,
          "tax_consumption_value": 0,
          "taxes": [{ "id": 13156, "name": "IVA 19%", "type": "IVA", "percentage": 19.00 }],
          "prices": [{ "currency_code": "COP", "price_list": [{ "position": 1, "name": "Precio de venta 1", "value": 12000 }] }],
          "unit": { "code": "94", "name": "unidad" },
          "unit_label": "unidad",
          "reference": "REF1",
          "description": "Camiseta de algodón blanca",
          "additional_fields": { "barcode": "B0123", "brand": "Gef", "tariff": "151612", "model": "Loiry" },
          "available_quantity": 0,
          "warehouses": [{ "id": 1270, "name": "Bodega principal", "quantity": 1 }],
          "metadata": { "created": "2020-06-15T03:33:17.208Z", "last_updated": null, "stock_updated": null }
        }
        """;

    public const string InvoiceJson = """
        {
          "id": "63f918c2-1c4d-4b9a-a3f0-6c2f8f3c1e55",
          "document": { "id": 24446 },
          "prefix": "FV",
          "number": 22,
          "name": "FV-2-22",
          "date": "2023-12-15",
          "customer": { "id": "6b6ceb28-0e58-4f0d-bd6e-e2f3a3f0b7b1", "identification": "13832081", "branch_office": 0 },
          "cost_center": 235,
          "currency": { "code": "USD", "exchange_rate": 3825.03 },
          "total": 2546.05,
          "balance": 0,
          "seller": 629,
          "retentions": [{ "id": 13156, "name": "Retención", "type": "Retefuente", "percentage": 19, "value": 5 }],
          "advance_payment": 33.3,
          "stamp": { "status": "Accepted", "cufe": "7eb8f1c2", "cude": null, "observations": "", "errors": "" },
          "mail": { "status": "sent", "observations": "" },
          "observations": "Observaciones",
          "items": [{
            "id": "e0c7a1b2-5d4f-4e3a-8b2c-1f0e9d8c7b6a",
            "code": "Item-1",
            "description": "Camiseta de algodón",
            "quantity": 2,
            "price": 1069.77,
            "seller": 629,
            "discount": { "percentage": 13, "value": 130 },
            "taxes": [{ "id": 13156, "name": "IVA 19%", "type": "IVA", "percentage": 19, "value": 406.51, "base_value": 2000 }],
            "warehouse": { "id": 15, "name": "Main Warehouse" },
            "total": 2546.05
          }],
          "payments": [{ "id": 5636, "name": "Crédito", "value": 1273.03, "due_date": "2021-03-19" }],
          "global_charges": [],
          "additional_fields": {},
          "public_url": "https://documentview.siigo.com/document?data=abc",
          "metadata": { "created": "2020-06-15T03:33:17.208Z", "last_updated": null, "stock_updated": null },
          "annulled": false
        }
        """;

    public static CustomerRequest CustomerRequest() => new()
    {
        Type = CustomerType.Customer,
        PersonType = PersonType.Person,
        IdType = IdTypes.CedulaCiudadania,
        Identification = "13832081",
        CheckDigit = "4",
        Name = ["Marcos", "Castillo"],
        CommercialName = "Siigo",
        BranchOffice = 0,
        Active = true,
        VatResponsible = false,
        FiscalResponsibilities = [FiscalResponsibility.NoResponsable],
        Address = new Address
        {
            Street = "Cra. 18 #79A - 42",
            City = City.Colombia("19", "19001"),
            PostalCode = "110911",
        },
        Phones = [new Phone { Indicative = "57", Number = "3006003345", Extension = "132" }],
        Contacts =
        [
            new Contact
            {
                FirstName = "Marcos",
                LastName = "Castillo",
                Email = "marcos.castillo@contacto.com",
                Phone = new Phone { Indicative = "57", Number = "3006003345", Extension = "132" },
            },
        ],
        Comments = "Comentarios",
        RelatedUsers = new RelatedUsers { SellerId = 629, CollectorId = 629 },
    };

    public static InvoiceRequest InvoiceRequest() => new()
    {
        Document = new IdReference(24446),
        Date = new DateOnly(2023, 12, 15),
        Customer = new InvoiceCustomerReference { Identification = "13832081", BranchOffice = 0 },
        Seller = 629,
        Items =
        [
            new InvoiceItemRequest
            {
                Code = "Item-1",
                Description = "Camiseta de algodón",
                Quantity = 1,
                Price = 1069.77m,
                Discount = 0,
                Taxes = [new IdReference(13156)],
            },
        ],
        Payments = [new InvoicePaymentRequest { Id = 5636, Value = 1273.03m, DueDate = new DateOnly(2021, 3, 19) }],
    };
}
