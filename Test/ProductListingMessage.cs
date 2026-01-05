using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wolverine.Attributes;

namespace Test
{
    [Topic("product-listing")]
    [MessageIdentity(nameof(ProductListingEventRequest))]
    public class ProductListingEventRequest : BaseEvent
    {
        public ProductListingEventRequest(string syncName, string documentNumber) : base(syncName, documentNumber)
        {
        }

        [JsonPropertyName("LastModified")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LastModified { get; set; }

        [JsonPropertyName("SyncData")]
        public ProductListingMessage? SyncData { get; set; }
    }

    public class ProductListingMessage
    {
        [JsonConverter(typeof(NumberToStringConverter))]
        [JsonPropertyName("article")]
        public string Article { get; set; }

        [JsonPropertyName("site")]
        public string? Site { get; set; }

        [JsonPropertyName("valid_from")]
        public string? ValidFrom { get; set; }

        [JsonPropertyName("valid_to")]
        public string? ValidTo { get; set; }

        [JsonConverter(typeof(NumberToStringConverter))]
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        public ProductListingMessage()
        {

        }

        private static string? FormatDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate) || rawDate.Length != 8)
                return null;

            try
            {
                var date = DateTime.ParseExact(rawDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                return date.ToString("dd.MM.yyyy");
            }
            catch
            {
                return null;
            }
        }
    }

    public class NumberToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64().ToString();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString()!;
            }

            throw new JsonException($"Invalid token type for {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
