using System.Text.Json;
using System.Text.Json.Serialization;

namespace Test
{
    public class BaseMessage<T>
    {
        [JsonConverter(typeof(IntToStringConverter))]
        public string? DocumentNumber { get; set; }
        public string? SyncName { get; set; }
        public T? SyncData { get; set; }
        public string? LastModified { get; set; }

        public BaseMessage() { }

        public BaseMessage(string? DocumentNumber, T SyncData, string SyncName)
        {
            this.DocumentNumber = DocumentNumber;
            this.SyncData = SyncData;
            this.SyncName = SyncName;
        }
    }

    public class ErrorMessageEvent
    {
        public string Key { get; set; }
        public string Reason { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Message { get; set; }
    }
    public class BaseEvent
    {
        [JsonPropertyName("SyncName")]
        public string SyncName { get; }

        [JsonPropertyName("DocumentNumber")]
        public string DocumentNumber { get; }

        public BaseEvent(string syncName, string documentNumber)
        {
            SyncName = syncName;
            DocumentNumber = documentNumber;
        }
    }

    public class FailedThresholdCallbackMessage
    {
        public string? MessageName { get; set; }
        public string? MessageType { get; set; }
        public string? MessageGroup { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProducerOrConsumer { get; set; }
        public object? MessageContent { get; set; }
    }

    public class IntToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var number))
                return number.ToString();
            return reader.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
