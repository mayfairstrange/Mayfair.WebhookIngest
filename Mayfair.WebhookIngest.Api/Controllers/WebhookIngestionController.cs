using Mayfair.WebhookIngest.Application.Webhooks;
using Mayfair.WebhookIngest.Infrastructure.Http;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mayfair.WebhookIngest.Api.Controllers;

[ApiController]
[Route("webhooks")]
public sealed class WebhookIngestionController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhookIngestionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Ingest(string provider, CancellationToken cancellationToken)
    {
        var payload = await RequestBodyReader.ReadRawBodyAsync(Request, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
            return BadRequest(new { error = "Empty body" });

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var result = await _mediator.Send(
            new IngestWebhookCommand(provider, headers, payload),
            cancellationToken);

        if (result == IngestWebhookResult.InvalidSignature)
            return BadRequest(new { error = "Invalid signature" });

        return Ok();
    }
}
