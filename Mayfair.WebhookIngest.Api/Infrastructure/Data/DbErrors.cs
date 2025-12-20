using Microsoft.EntityFrameworkCore;

namespace Mayfair.WebhookIngest.Api.Infrastructure.Data
{
    public static class DbErrors
    {
        public static bool IsUniqueViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("23505", StringComparison.OrdinalIgnoreCase);
        }
    }
}
