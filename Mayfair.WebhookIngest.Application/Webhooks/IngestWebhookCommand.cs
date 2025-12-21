using MediatR;

namespace Mayfair.WebhookIngest.Application.Webhooks;

public sealed record IngestWebhookCommand(
    string Provider,
    IReadOnlyDictionary<string, string> Headers,
    string Payload) : IRequest<IngestWebhookResult>;
