using Mayfair.WebhookIngest.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<IncomingEvent> IncomingEvents => Set<IncomingEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IncomingEvent>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
                e.Property(x => x.Provider).HasMaxLength(50);
                e.Property(x => x.ProviderEventId).HasMaxLength(200);
                e.Property(x => x.EventType).HasMaxLength(200);
            });
        }
    }
}
