namespace Mayfair.WebhookIngest.Api.Webhooks.Abstractions
{
    public interface IWebhookSignatureVerifier
    {
        Task<WebhookVerificationResult> VerifyAsync(string provider, IHeaderDictionary headers, string payload, CancellationToken cancellationToken);
    }
}
