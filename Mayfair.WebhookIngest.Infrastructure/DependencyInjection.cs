using Mayfair.WebhookIngest.Application.Abstractions;
using Mayfair.WebhookIngest.Infrastructure.Persistence;
using Mayfair.WebhookIngest.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mayfair.WebhookIngest.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Missing connection string: ConnectionStrings:Default");

            // PostgreSQL
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(cs));

            services.AddScoped<IIncomingEventWriter, IncomingEventWriter>();
            services.AddScoped<IWebhookSignatureVerifier, WebhookSignatureVerifierAdapter>();

            services.Scan(scan => scan
                .FromAssemblyOf<WebhookSignatureVerifierAdapter>()
                .AddClasses(classes => classes.AssignableTo<IWebhookProviderVerifier>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }
    }
}
