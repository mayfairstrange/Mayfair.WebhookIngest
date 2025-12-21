using Mayfair.WebhookIngest.Api.Persistence;

namespace Mayfair.WebhookIngest.Application.Abstractions
{
    public interface IIncomingEventWriter
    {
        Task InsertIgnoringDuplicatesAsync(IncomingEvent incoming, CancellationToken cancellationToken);
    }
}
