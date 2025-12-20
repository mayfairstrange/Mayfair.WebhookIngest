using Mayfair.WebhookIngest.Api.Infrastructure.Data;
using Mayfair.WebhookIngest.Api.Infrastructure.Http;
using Mayfair.WebhookIngest.Api.Persistence;
using Mayfair.WebhookIngest.Api.Webhooks.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mayfair.WebhookIngest.Api.Controllers;

[ApiController]
[Route("webhooks")]
public sealed class WebhookIngestionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebhookSignatureVerifier _verifier;

    public WebhookIngestionController(AppDbContext db, IWebhookSignatureVerifier verifier)
    {
        _db = db;
        _verifier = verifier;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Ingest(string provider, CancellationToken cancellationToken)
    {
        var payload = await RequestBodyReader.ReadRawBodyAsync(Request, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
            return BadRequest(new { error = "Empty body" });

        var verify = await _verifier.VerifyAsync(provider, Request.Headers, payload, cancellationToken);

        var incoming = new IncomingEvent
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderEventId = verify.ProviderEventId ?? "unknown",
            EventType = verify.EventType ?? "unknown",
            ReceivedAt = DateTimeOffset.UtcNow,
            PayloadJson = payload,
            Status = verify.IsValid ? "received" : "invalid_signature",
            Attempts = 0,
            LastError = verify.IsValid ? null : verify.Error
        };

        await InsertIgnoringDuplicatesAsync(incoming, cancellationToken);

        if (!verify.IsValid)
            return BadRequest(new { error = "Invalid signature" });

        return Ok();
    }

    private async Task InsertIgnoringDuplicatesAsync(IncomingEvent incoming, CancellationToken cancellationToken)
    {
        _db.IncomingEvents.Add(incoming);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            _db.ChangeTracker.Clear();0
        }
    }
}
