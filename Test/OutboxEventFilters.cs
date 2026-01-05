using Confluent.Kafka;
using System.Text;
using System.Text.Json;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Util;

namespace Test
{
    public class OutboxEventFilters : IKafkaEnvelopeMapper
    {
        public IEnumerable<string> AllHeaders()
        {
            yield break;
        }
        public void MapEnvelopeToOutgoing(Envelope envelope, Message<string, string> outgoing)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (outgoing == null) throw new ArgumentNullException(nameof(outgoing));

            outgoing.Key = envelope.GroupId;

            try
            {
                if (envelope.Data is { Length: > 0 })
                {
                    outgoing.Value = Encoding.Default.GetString(envelope.Data);
                }
                else if (envelope.Message != null)
                {
                    outgoing.Value = JsonSerializer.Serialize(envelope.Message, new JsonSerializerOptions());
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Envelope {envelope.Id} does not contain valid data or message.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to map envelope {envelope.Id} to outgoing message. Reason: {ex.Message}", ex);
            }
        }

        public void MapIncomingToEnvelope(Envelope envelope, Message<string, string> incoming)
        {
            try
            {
                var doc = JsonSerializer.Deserialize<BaseEvent>(incoming.Value)!;
                if (doc is null)
                    return;

                switch (envelope.TopicName)
                {
                    case "product-listing":
                        envelope.MessageType = typeof(ProductListingEventRequest).ToMessageTypeName();
                        envelope.Message = JsonSerializer.Deserialize<ProductListingEventRequest>(incoming.Value);
                        break;
                }

                envelope.Id = Guid.TryParse(incoming.Key, out Guid id) ? id : Guid.NewGuid();
                envelope.Data = Encoding.Default.GetBytes(incoming.Value);
            }
            catch (JsonException ex)
            {
                return;
            }
            catch (NotSupportedException ex)
            {
                return;
            }

        }
    }
}
