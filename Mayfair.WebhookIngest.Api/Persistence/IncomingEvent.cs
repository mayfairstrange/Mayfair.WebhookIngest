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
        public string Status { get; set; } = "received";
        public int Attempts { get; set; }
        public string? LastError { get; set; }
    }
}
