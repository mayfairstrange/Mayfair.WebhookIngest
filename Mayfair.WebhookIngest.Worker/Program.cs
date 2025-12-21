using Mayfair.WebhookIngest.Infrastructure;
using Mayfair.WebhookIngest.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.SetBasePath(AppContext.BaseDirectory);
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        cfg.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddInfrastructure(ctx.Configuration);
        services.AddHostedService<IncomingEventProcessor>();
    })
    .Build();

await host.RunAsync();
