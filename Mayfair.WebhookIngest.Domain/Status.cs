namespace Mayfair.WebhookIngest.Api.Persistence
{
    public enum Status
    {
        Received,
        Processing,
        Failed,
        Processed,
        Ignored
    }
}
