using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Application.Abstractions
{
    public interface IWebhookSignatureVerifier
    {
        Task<WebhookVerificationResult> VerifyAsync(
            string provider,
            IReadOnlyDictionary<string, string> headers,
            string payload,
            CancellationToken cancellationToken);
    }
}
