using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Infrastructure.Persistence
{
    public static class DbErrors
    {
        public static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
        }
    }
}
