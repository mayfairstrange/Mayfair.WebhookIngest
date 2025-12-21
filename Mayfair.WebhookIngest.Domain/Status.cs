namespace Mayfair.WebhookIngest.Api.Persistence
{
    public enum Status
    {
        Received = 0,
        Processing = 1,
        Processed = 2,
        Failed = 3,
        DeadLettered = 4
    }

}
