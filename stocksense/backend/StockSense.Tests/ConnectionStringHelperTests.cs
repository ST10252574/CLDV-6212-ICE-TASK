using Npgsql;
using StockSense.Api.Data;

namespace StockSense.Tests;

public class ConnectionStringHelperTests
{
    [Fact]
    public void KeyValueConnectionStrings_PassThroughUnchanged()
    {
        const string raw = "Host=db;Port=5432;Database=stocksense;Username=u;Password=p";
        Assert.Equal(raw, ConnectionStringHelper.Normalize(raw));
    }

    [Fact]
    public void PostgresUrl_IsConvertedToKeyValueForm()
    {
        var result = ConnectionStringHelper.Normalize(
            "postgresql://neon_user:s%40crt@ep-cool-123.eu-central-1.aws.neon.tech/stocksense?sslmode=require");

        var parsed = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("ep-cool-123.eu-central-1.aws.neon.tech", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("stocksense", parsed.Database);
        Assert.Equal("neon_user", parsed.Username);
        Assert.Equal("s@crt", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }
}
