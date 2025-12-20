namespace Mayfair.WebhookIngest.Api.Webhooks
{
    public sealed record WebhookVerificationResult(bool IsValid, string? ProviderEventId, string? EventType, string? Error);
}
