using System.Text.Json;
using System.Text.Json.Serialization;
using SiigoSharp.Internal;

namespace SiigoSharp;

internal sealed class CatalogsClient(SiigoConnection connection) : ICatalogsClient
{
    public Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(string type, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return GetListAsync<DocumentType>(new QueryStringBuilder().Add("type", type).AppendTo("v1/document-types"), cancellationToken);
    }

    public Task<IReadOnlyList<Tax>> GetTaxesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<Tax>("v1/taxes", cancellationToken);

    public Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync(string documentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);
        return GetListAsync<PaymentType>(
            new QueryStringBuilder().Add("document_type", documentType).AppendTo("v1/payment-types"), cancellationToken);
    }

    public async Task<IReadOnlyList<SiigoUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<SiigoUser>();
        for (var page = 1; ; page++)
        {
            var uri = new QueryStringBuilder().Add("page", page == 1 ? (int?)null : page).AppendTo("v1/users");
            var result = await connection.GetAsync<ListOrPage<SiigoUser>>(uri, cancellationToken).ConfigureAwait(false);
            users.AddRange(result.Items);

            if (result.Page is null || Paginator.IsLastPage(page, result.Page))
            {
                return users;
            }
        }
    }

    public Task<IReadOnlyList<AccountGroup>> GetAccountGroupsAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<AccountGroup>("v1/account-groups", cancellationToken);

    public Task<IReadOnlyList<CostCenter>> GetCostCentersAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<CostCenter>("v1/cost-centers", cancellationToken);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string uri, CancellationToken cancellationToken) =>
        await connection.GetAsync<List<T>>(uri, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// A list that Siigo documents both as a plain array and as a paginated object; both are accepted.
    /// </summary>
    [JsonConverter(typeof(ListOrPageConverterFactory))]
    private sealed class ListOrPage<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];

        /// <summary>The paging information, when Siigo answered with a paginated object.</summary>
        public SiigoPage<T>? Page { get; init; }
    }

    private sealed class ListOrPageConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ListOrPage<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

        private sealed class Converter<T> : JsonConverter<ListOrPage<T>>
        {
            public override ListOrPage<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    return new ListOrPage<T> { Items = JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? [] };
                }

                var page = JsonSerializer.Deserialize<SiigoPage<T>>(ref reader, options);
                return new ListOrPage<T> { Items = page?.Results ?? [], Page = page };
            }

            public override void Write(Utf8JsonWriter writer, ListOrPage<T> value, JsonSerializerOptions options) =>
                throw new NotSupportedException();
        }
    }
}
