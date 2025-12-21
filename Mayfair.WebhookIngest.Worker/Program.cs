using Mayfair.WebhookIngest.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mayfair.WebhookIngest.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddInfrastructure(ctx.Configuration);
        services.AddHostedService<IncomingEventProcessor>();
    })
    .Build();

await host.RunAsync();
