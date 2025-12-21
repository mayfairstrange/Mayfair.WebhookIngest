using Mayfair.WebhookIngest.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure.Webhooks
{
    public interface IWebhookProviderVerifier
    {
        string Provider { get; }
        WebhookVerificationResult Verify(IReadOnlyDictionary<string, string> headers, string payload);
    }
}
