using Mayfair.WebhookIngest.Application.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mayfair.WebhookIngest.Infrastructure.Webhooks.Stripe
{
    public sealed class StripeWebhookProviderVerifier : IWebhookProviderVerifier
    {
        private const string SignatureHeaderName = "Stripe-Signature";

        private readonly StripeWebhookOptions _options;

        public StripeWebhookProviderVerifier(IOptions<StripeWebhookOptions> options)
        {
            _options = options.Value;
        }

        public string Provider => "stripe";

        public WebhookVerificationResult Verify(IReadOnlyDictionary<string, string> headers, string payload)
        {
            var (eventId, eventType) = TryExtractStripeEventFields(payload);

            if (!_options.SigningSecret.Any())
            {
                return new WebhookVerificationResult
                {
                    IsValid = false,
                    ProviderEventId = eventId,
                    EventType = eventType,
                    Error = "Stripe signing secret not configured"
                };
            }

            if (!TryGetHeader(headers, SignatureHeaderName, out var sigHeader) || string.IsNullOrWhiteSpace(sigHeader))
            {
                return new WebhookVerificationResult
                {
                    IsValid = false,
                    ProviderEventId = eventId,
                    EventType = eventType,
                    Error = "Missing Stripe-Signature header"
                };
            }

            if (!TryParseStripeSignatureHeader(sigHeader, out var timestamp, out var v1Signatures, out var parseError))
            {
                return new WebhookVerificationResult
                {
                    IsValid = false,
                    ProviderEventId = eventId,
                    EventType = eventType,
                    Error = parseError
                };
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var age = Math.Abs(now - timestamp);
            if (age > _options.ToleranceSeconds)
            {
                return new WebhookVerificationResult
                {
                    IsValid = false,
                    ProviderEventId = eventId,
                    EventType = eventType,
                    Error = "Signature timestamp outside tolerance"
                };
            }

            var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
            var expectedHex = ComputeHmacSha256Hex(_options.SigningSecret, signedPayload);

            var expectedBytes = HexToBytes(expectedHex);
            var valid = v1Signatures.Any(sig =>
            {
                var sigBytes = HexToBytes(sig);
                return sigBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(sigBytes, expectedBytes);
            });

            return new WebhookVerificationResult
            {
                IsValid = valid,
                ProviderEventId = eventId,
                EventType = eventType,
                Error = valid ? null : "Invalid signature"
            };
        }

        private static bool TryGetHeader(IReadOnlyDictionary<string, string> headers, string name, out string value)
        {
            foreach (var kvp in headers)
            {
                if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = "";
            return false;
        }

        private static bool TryParseStripeSignatureHeader(
            string header,
            out long timestamp,
            out List<string> v1Signatures,
            out string error)
        {
            timestamp = 0;
            v1Signatures = new List<string>();
            error = "";

            var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0 || idx == part.Length - 1)
                    continue;

                var key = part.Substring(0, idx);
                var value = part.Substring(idx + 1);

                if (key == "t")
                {
                    if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp))
                    {
                        error = "Invalid Stripe-Signature timestamp";
                        return false;
                    }
                }
                else if (key == "v1")
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        v1Signatures.Add(value);
                }
            }

            if (timestamp == 0)
            {
                error = "Missing Stripe-Signature timestamp";
                return false;
            }

            if (v1Signatures.Count == 0)
            {
                error = "Missing Stripe-Signature v1 signature";
                return false;
            }

            return true;
        }

        private static string ComputeHmacSha256Hex(string secret, string signedPayload)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var msg = Encoding.UTF8.GetBytes(signedPayload);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(msg);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                return Array.Empty<byte>();

            try
            {
                return Convert.FromHexString(hex);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static (string? id, string? type) TryExtractStripeEventFields(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                string? id = null;
                string? type = null;

                if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                    id = idProp.GetString();

                if (root.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                    type = typeProp.GetString();

                return (id, type);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
