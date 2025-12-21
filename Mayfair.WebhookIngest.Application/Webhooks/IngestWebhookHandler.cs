using Mayfair.WebhookIngest.Api.Persistence;
using Mayfair.WebhookIngest.Application.Abstractions;
using MediatR;

namespace Mayfair.WebhookIngest.Application.Webhooks
{
    public sealed class IngestWebhookHandler : IRequestHandler<IngestWebhookCommand, IngestWebhookResult>
    {
        private readonly IWebhookSignatureVerifier _verifier;
        private readonly IIncomingEventWriter _writer;

        public IngestWebhookHandler(IWebhookSignatureVerifier verifier, IIncomingEventWriter writer)
        {
            _verifier = verifier;
            _writer = writer;
        }

        public async Task<IngestWebhookResult> Handle(IngestWebhookCommand request, CancellationToken cancellationToken)
        {
            var verify = await _verifier.VerifyAsync(
                request.Provider,
                request.Headers,
                request.Payload,
                cancellationToken);

            var incoming = IncomingEvent.Create(
                request.Provider,
                verify.ProviderEventId,
                verify.EventType,
                request.Payload,
                verify.IsValid,
                verify.Error);

            await _writer.InsertIgnoringDuplicatesAsync(incoming, cancellationToken);

            return verify.IsValid ? IngestWebhookResult.Accepted : IngestWebhookResult.InvalidSignature;
        }
    }
}
