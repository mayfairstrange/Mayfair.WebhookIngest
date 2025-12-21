using System.Security.Cryptography;
using System.Text;
using Mayfair.WebhookIngest.Infrastructure.Webhooks.Stripe;
using Microsoft.Extensions.Options;
using Xunit;

namespace Mayfair.WebhookIngest.UnitTests;

public sealed class StripeWebhookProviderVerifierTests
{
    [Fact]
    public void Verify_ValidSignature_ReturnsValidAndExtractsFields()
    {
        var secret = "whsec_test_secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var payload = """
        {
          "id": "evt_123",
          "type": "payment_intent.succeeded",
          "object": "event"
        }
        """;

        var signedPayload = $"{timestamp}.{payload}";
        var signature = ComputeHmacSha256Hex(secret, signedPayload);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Stripe-Signature"] = $"t={timestamp},v1={signature}"
        };

        var options = Options.Create(new StripeWebhookOptions
        {
            SigningSecret = secret,
            ToleranceSeconds = 300
        });

        var sut = new StripeWebhookProviderVerifier(options);

        var result = sut.Verify(headers, payload);

        Assert.True(result.IsValid);
        Assert.Equal("evt_123", result.ProviderEventId);
        Assert.Equal("payment_intent.succeeded", result.EventType);
        Assert.Null(result.Error);
    }

    private static string ComputeHmacSha256Hex(string secret, string message)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var msg = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(msg);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}