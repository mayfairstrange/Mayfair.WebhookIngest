using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Application.Abstractions
{
    public sealed class WebhookVerificationResult
    {
        public bool IsValid { get; init; }
        public string? ProviderEventId { get; init; }
        public string? EventType { get; init; }
        public string? Error { get; init; }
    }
}
