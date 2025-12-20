using Mayfair.WebhookIngest.Api.Webhooks.Abstractions;

namespace Mayfair.WebhookIngest.Api.Webhooks
{
    public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
    {
        private readonly IReadOnlyDictionary<string, IWebhookProviderVerifier> _verifiers;

        public WebhookSignatureVerifier(IEnumerable<IWebhookProviderVerifier> verifiers)
        {
            _verifiers = verifiers.ToDictionary(v => v.Provider, StringComparer.OrdinalIgnoreCase);
        }

        public Task<WebhookVerificationResult> VerifyAsync(string provider, IHeaderDictionary headers, string payload, CancellationToken cancellationToken)
        {
            if (!_verifiers.TryGetValue(provider, out var verifier))
                return Task.FromResult(new WebhookVerificationResult(false, null, null, "Unknown provider"));

            return Task.FromResult(verifier.Verify(headers, payload));
        }
    }
}
