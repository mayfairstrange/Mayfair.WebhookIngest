using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure.Webhooks.Stripe
{
    public sealed class StripeWebhookOptions
    {
        public string SigningSecret { get; set; } = "";
        public int ToleranceSeconds { get; set; } = 300;
    }
}
