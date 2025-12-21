using Mayfair.WebhookIngest.Api.Persistence;
using Mayfair.WebhookIngest.Infrastructure;
using Mayfair.WebhookIngest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Mayfair.WebhookIngest.IntegrationTests;

public sealed class IncomingEventWriterIdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("webhookingest")
        .WithUsername("app")
        .WithPassword("app_pw")
        .Build();

    public Task InitializeAsync() => _db.StartAsync();

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    [Fact]
    public async Task InsertIgnoringDuplicatesAsync_SameProviderAndEventId_InsertsOnce()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_db.GetConnectionString())
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        var writer = new IncomingEventWriter(context);

        var provider = "stripe";
        var providerEventId = $"evt_test_{Guid.NewGuid():N}";

        var first = IncomingEvent.Create(
            provider,
            providerEventId,
            "payment_intent.succeeded",
            """{"id":"evt","type":"payment_intent.succeeded"}""",
            signatureValid: true,
            error: null);

        var second = IncomingEvent.Create(
            provider,
            providerEventId,
            "payment_intent.succeeded",
            """{"id":"evt","type":"payment_intent.succeeded"}""",
            signatureValid: true,
            error: null);

        await writer.InsertIgnoringDuplicatesAsync(first, CancellationToken.None);
        await writer.InsertIgnoringDuplicatesAsync(second, CancellationToken.None);

        var count = await context.IncomingEvents.CountAsync(x =>
            x.Provider == provider && x.ProviderEventId == providerEventId);

        Assert.Equal(1, count);
    }
}