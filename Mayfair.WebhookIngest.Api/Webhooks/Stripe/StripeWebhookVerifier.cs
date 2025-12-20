using Mayfair.WebhookIngest.Api.Webhooks.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mayfair.WebhookIngest.Api.Webhooks.Stripe
{
    public sealed class StripeWebhookVerifier : IWebhookProviderVerifier
    {
        private readonly string _webhookSecret;
        private readonly int _toleranceSeconds;

        public StripeWebhookVerifier(IConfiguration configuration)
        {
            _webhookSecret = configuration["Stripe:WebhookSecret"] ?? "";
            _toleranceSeconds = 300;
        }

        public string Provider => "stripe";

        public WebhookVerificationResult Verify(IHeaderDictionary headers, string payload)
        {
            if (string.IsNullOrWhiteSpace(_webhookSecret))
                return new WebhookVerificationResult(false, TryGetEventId(payload), TryGetEventType(payload), "Stripe webhook secret not configured");

            var sigHeader = headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(sigHeader))
                return new WebhookVerificationResult(false, TryGetEventId(payload), TryGetEventType(payload), "Missing Stripe-Signature header");

            if (!TryParseStripeSignatureHeader(sigHeader, out var timestamp, out var v1))
                return new WebhookVerificationResult(false, TryGetEventId(payload), TryGetEventType(payload), "Invalid Stripe-Signature format");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - timestamp) > _toleranceSeconds)
                return new WebhookVerificationResult(false, TryGetEventId(payload), TryGetEventType(payload), "Signature timestamp outside tolerance");

            var signedPayload = $"{timestamp}.{payload}";
            var expected = ComputeHmacSha256Hex(_webhookSecret, signedPayload);

            if (!FixedTimeEqualsHex(expected, v1))
                return new WebhookVerificationResult(false, TryGetEventId(payload), TryGetEventType(payload), "Signature mismatch");

            return new WebhookVerificationResult(true, TryGetEventId(payload), TryGetEventType(payload), null);
        }

        private static bool TryParseStripeSignatureHeader(string header, out long timestamp, out string v1)
        {
            timestamp = 0;
            v1 = "";

            var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? t = null;
            string? sig = null;

            foreach (var p in parts)
            {
                var kv = p.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2) continue;

                if (kv[0] == "t") t = kv[1];
                if (kv[0] == "v1") sig = kv[1];
            }

            if (t is null || sig is null) return false;
            if (!long.TryParse(t, out timestamp)) return false;

            v1 = sig;
            return true;
        }

        private static string ComputeHmacSha256Hex(string secret, string message)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var data = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(data);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        private static bool FixedTimeEqualsHex(string hexA, string hexB)
        {
            if (hexA.Length != hexB.Length) return false;

            var diff = 0;
            for (var i = 0; i < hexA.Length; i++)
                diff |= hexA[i] ^ hexB[i];

            return diff == 0;
        }

        private static string? TryGetEventId(string payload)
        {
            TryExtract(payload, out var id, out _);
            return id;
        }

        private static string? TryGetEventType(string payload)
        {
            TryExtract(payload, out _, out var type);
            return type;
        }

        private static void TryExtract(string payload, out string? id, out string? type)
        {
            id = null;
            type = null;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                    id = idEl.GetString();

                if (root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                    type = typeEl.GetString();
            }
            catch
            {
            }
        }
    }
}
