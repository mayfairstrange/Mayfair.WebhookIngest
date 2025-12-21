using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Application.Webhooks
{
    public enum IngestWebhookResult
    {
        Accepted,
        InvalidSignature
    }
}
