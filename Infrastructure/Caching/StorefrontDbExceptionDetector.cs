using Microsoft.Data.SqlClient;

namespace e_commerce_web_customer.Infrastructure.Caching;

internal static class StorefrontDbExceptionDetector
{
    private static readonly HashSet<int> TransientSqlErrorNumbers =
    [
        -2,
        53,
        64,
        233,
        4060,
        10053,
        10054,
        10060,
        10928,
        10929,
        40197,
        40501,
        40613
    ];

    public static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException)
        {
            return true;
        }

        if (exception is SqlException sqlException
            && IsTransient(sqlException))
        {
            return true;
        }

        return exception.InnerException is not null
            && IsTransient(exception.InnerException);
    }

    private static bool IsTransient(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (TransientSqlErrorNumbers.Contains(error.Number))
            {
                return true;
            }
        }

        return false;
    }
}
