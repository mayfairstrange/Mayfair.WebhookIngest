namespace Mayfair.WebhookIngest.Api.Persistence
{
    public sealed class IncomingEvent
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = default!;
        public string ProviderEventId { get; set; } = default!;
        public string EventType { get; set; } = default!;
        public DateTimeOffset ReceivedAt { get; set; }

        public string PayloadJson { get; set; } = default!;
        public Status Status { get; set; } = Status.Received;
        public int Attempts { get; set; }
        public string? LastError { get; set; }

        private IncomingEvent() { }

        public static IncomingEvent Create(
            string provider,
            string providerEventId,
            string eventType,
            string payloadJson,
            bool signatureValid,
            string? error)
        {
            return new IncomingEvent
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                ProviderEventId = string.IsNullOrWhiteSpace(providerEventId) ? "unknown" : providerEventId,
                EventType = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType,
                ReceivedAt = DateTimeOffset.UtcNow,
                PayloadJson = payloadJson,
                Status = signatureValid ? Status.Received : Status.Failed,
                Attempts = 0,
                LastError = signatureValid ? null : error
            };
        }
    }
}