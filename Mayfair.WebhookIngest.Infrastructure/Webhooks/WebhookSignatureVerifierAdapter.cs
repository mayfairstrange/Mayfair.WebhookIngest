using Mayfair.WebhookIngest.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure.Webhooks
{
    public sealed class WebhookSignatureVerifierAdapter : IWebhookSignatureVerifier
    {
        private readonly IReadOnlyDictionary<string, IWebhookProviderVerifier> _verifiersByProvider;

        public WebhookSignatureVerifierAdapter(IEnumerable<IWebhookProviderVerifier> verifiers)
        {
            _verifiersByProvider = verifiers.ToDictionary(v => v.Provider, StringComparer.OrdinalIgnoreCase);
        }

        public Task<WebhookVerificationResult> VerifyAsync(
            string provider,
            IReadOnlyDictionary<string, string> headers,
            string payload,
            CancellationToken cancellationToken)
        {
            if (!_verifiersByProvider.TryGetValue(provider, out var verifier))
            {
                return Task.FromResult(new WebhookVerificationResult
                {
                    IsValid = false,
                    Error = "Unknown provider"
                });
            }

            var result = verifier.Verify(headers, payload);
            return Task.FromResult(result);
        }
    }
}
