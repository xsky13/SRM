using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SRM.Api.Utils
{
    public static class DbErrors
    {
        public static bool IsOverlap(this DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation };
    }
}
