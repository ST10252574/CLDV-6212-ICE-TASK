using Npgsql;

namespace StockSense.Api.Data;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Hosted Postgres providers (Neon, Supabase, Render) hand out URLs such as
    /// postgresql://user:pass@host/db?sslmode=require. Npgsql wants key=value pairs,
    /// so convert the URL form and pass key=value strings through untouched.
    /// </summary>
    public static string Normalize(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
