namespace Mayfair.WebhookIngest.Api.Webhooks.Abstractions
{
    public interface IWebhookProviderVerifier
    {
        string Provider { get; }
        WebhookVerificationResult Verify(IHeaderDictionary headers, string payload);
    }
}
