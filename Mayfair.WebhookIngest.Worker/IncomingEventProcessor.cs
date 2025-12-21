using Mayfair.WebhookIngest.Api.Persistence;
using Mayfair.WebhookIngest.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Worker
{
    public sealed class IncomingEventProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<IncomingEventProcessor> _logger;

        public IncomingEventProcessor(IServiceScopeFactory scopeFactory, ILogger<IncomingEventProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTimeOffset.UtcNow;
            var lease = now.AddSeconds(30);
            var lockId = Guid.NewGuid().ToString("N");

            var batchSize = 20;

            var candidates = await db.IncomingEvents
                .Where(x =>
                    x.Status == Status.Received &&
                    (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                    (x.LockedUntil == null || x.LockedUntil <= now))
                .OrderBy(x => x.ReceivedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
                return;

            foreach (var ev in candidates)
            {
                ev.Status = Status.Processing;
                ev.LockedUntil = lease;
                ev.LockId = lockId;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var ev in candidates)
            {
                await ProcessOneAsync(db, ev.Id, lockId, cancellationToken);
            }
        }

        private static TimeSpan Backoff(int attempts)
        {
            var seconds = attempts switch
            {
                <= 1 => 5,
                2 => 15,
                3 => 60,
                _ => 300
            };

            return TimeSpan.FromSeconds(seconds);
        }

        private async Task ProcessOneAsync(AppDbContext db, Guid id, string lockId, CancellationToken cancellationToken)
        {
            var ev = await db.IncomingEvents.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (ev == null)
                return;

            if (ev.LockId != lockId)
                return;

            try
            {
                if (ev.Status != Status.Processing)
                    return;

                ev.LastAttemptAt = DateTimeOffset.UtcNow;

                await SimulateProcessingAsync(ev, cancellationToken);

                ev.Status = Status.Processed;
                ev.ProcessedAt = DateTimeOffset.UtcNow;
                ev.LastError = null;
                ev.LockedUntil = null;
                ev.LockId = null;
                ev.NextAttemptAt = null;

                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ev.Attempts += 1;
                ev.LastError = ex.ToString();
                _logger.LogError(ex, "Failed processing IncomingEvent {Id}", id);

                if (ev.Attempts >= 5)
                {
                    ev.Status = Status.DeadLettered;
                    ev.LockedUntil = null;
                    ev.LockId = null;
                    ev.NextAttemptAt = null;
                }
                else
                {
                    ev.Status = Status.Received;
                    ev.LockedUntil = null;
                    ev.LockId = null;
                    ev.NextAttemptAt = DateTimeOffset.UtcNow.Add(Backoff(ev.Attempts));
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private static Task SimulateProcessingAsync(IncomingEvent ev, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
