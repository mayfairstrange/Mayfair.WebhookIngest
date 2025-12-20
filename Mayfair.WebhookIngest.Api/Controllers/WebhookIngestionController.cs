using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mayfair.WebhookIngest.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mayfair.WebhookIngest.Api.Controllers;

[ApiController]
[Route("webhooks")]
public sealed class WebhookIngestionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public WebhookIngestionController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken)
    {
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return BadRequest(new { error = "Missing Stripe-Signature header" });

        var payload = await ReadRawBodyAsync(Request, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
            return BadRequest(new { error = "Empty body" });

        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Stripe webhook secret not configured" });

        if (!TryVerifyStripeSignature(payload, signatureHeader, webhookSecret, toleranceSeconds: 300, out var providerEventId, out var eventType, out var signatureError))
        {
            var invalidEvent = new IncomingEvent
            {
                Id = Guid.NewGuid(),
                Provider = "stripe",
                ProviderEventId = providerEventId ?? "unknown",
                EventType = eventType ?? "unknown",
                ReceivedAt = DateTimeOffset.UtcNow,
                PayloadJson = payload,
                Status = "invalid_signature",
                Attempts = 0,
                LastError = signatureError
            };

            await TryInsertIgnoringDuplicatesAsync(invalidEvent, cancellationToken);

            return BadRequest(new { error = "Invalid signature" });
        }

        var incoming = new IncomingEvent
        {
            Id = Guid.NewGuid(),
            Provider = "stripe",
            ProviderEventId = providerEventId ?? "unknown",
            EventType = eventType ?? "unknown",
            ReceivedAt = DateTimeOffset.UtcNow,
            PayloadJson = payload,
            Status = "received",
            Attempts = 0
        };

        await TryInsertIgnoringDuplicatesAsync(incoming, cancellationToken);

        return Ok();
    }

    private static async Task<string> ReadRawBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);

        request.Body.Position = 0;
        return body;
    }

    private async Task TryInsertIgnoringDuplicatesAsync(IncomingEvent incoming, CancellationToken cancellationToken)
    {
        _db.IncomingEvents.Add(incoming);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _db.ChangeTracker.Clear();
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
               || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
               || message.Contains("23505", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryVerifyStripeSignature(
        string payload,
        string stripeSignatureHeader,
        string webhookSecret,
        int toleranceSeconds,
        out string? providerEventId,
        out string? eventType,
        out string? error)
    {
        providerEventId = null;
        eventType = null;
        error = null;

        if (!TryParseStripeSignatureHeader(stripeSignatureHeader, out var timestamp, out var v1))
        {
            error = "Invalid Stripe-Signature format";
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > toleranceSeconds)
        {
            error = "Signature timestamp outside tolerance";
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var expected = ComputeHmacSha256Hex(webhookSecret, signedPayload);

        if (!FixedTimeEqualsHex(expected, v1))
        {
            error = "Signature mismatch";
            TryExtractStripeFields(payload, out providerEventId, out eventType);
            return false;
        }

        TryExtractStripeFields(payload, out providerEventId, out eventType);
        return true;
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

    private static void TryExtractStripeFields(string payload, out string? id, out string? type)
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
