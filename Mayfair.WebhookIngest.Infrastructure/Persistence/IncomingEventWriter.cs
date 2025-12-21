using Mayfair.WebhookIngest.Api.Persistence;
using Mayfair.WebhookIngest.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure.Persistence
{
    public sealed class IncomingEventWriter : IIncomingEventWriter
    {
        private readonly AppDbContext _db;

        public IncomingEventWriter(AppDbContext db)
        {
            _db = db;
        }

        public async Task InsertIgnoringDuplicatesAsync(IncomingEvent incoming, CancellationToken cancellationToken)
        {
            _db.IncomingEvents.Add(incoming);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                _db.ChangeTracker.Clear();
            }
        }
    }
}
