using Npgsql;

namespace Paintings.Data;

public static class PostgresConnectionString
{
    public static string Normalize(string configuredString)
    {
        if (!Uri.TryCreate(configuredString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return new NpgsqlConnectionStringBuilder(configuredString).ConnectionString;
        }

        var userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.UriEscaped);
        var separator = userInfo.IndexOf(':');
        if (separator < 0)
        {
            throw new InvalidOperationException("The PostgreSQL URL must include a username and password.");
        }

        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("The PostgreSQL URL must include a database name.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = Uri.UnescapeDataString(userInfo[..separator]),
            Password = Uri.UnescapeDataString(userInfo[(separator + 1)..]),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
