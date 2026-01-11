using System.Data.Common;
using System.Text;

namespace SharpDb;

public readonly struct DbConnectionInfo
{
    public readonly string ServerName;
    public readonly string DatabaseName;
    public readonly string TablePrefix;

    private DbConnectionInfo(string serverName, string databaseName, string tablePrefix)
    {
        ServerName = serverName;
        DatabaseName = databaseName;
        TablePrefix = tablePrefix;
    }

    public static DbConnectionInfo FromConnectionString(string connectionString)
    {
        DbConnectionStringBuilder connBuilder = new(false)
        {
            ConnectionString = connectionString
        };

        string serverName = GetServerName(connBuilder);
        string databaseName = GetDatabaseName(connBuilder);
        string tablePrefix = GetTablePrefix(serverName, databaseName);

        return new(serverName: serverName, databaseName: databaseName, tablePrefix: tablePrefix);
    }

    private static string GetServerName(in DbConnectionStringBuilder csBuilder)
    {
        return GetValueCaseAndSpaceInsensitive(csBuilder, "Data Source;Server;Address");
    }

    private static string GetDatabaseName(in DbConnectionStringBuilder csBuilder)
    {
        return GetValueCaseAndSpaceInsensitive(csBuilder, "Initial Catalog;Database");
    }

    private static string GetTablePrefix(in string serverName, in string databaseName)
    {
        StringBuilder tablePrefixBuilder = new();
        if (!string.IsNullOrWhiteSpace(serverName))
        {
            if (!serverName.StartsWith('['))
                tablePrefixBuilder.Append('[');
            tablePrefixBuilder.Append(serverName);
            if (!serverName.EndsWith(']'))
                tablePrefixBuilder.Append(']');
            tablePrefixBuilder.Append('.');
        }
        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            if (!databaseName.StartsWith('['))
                tablePrefixBuilder.Append('[');
            tablePrefixBuilder.Append(databaseName);
            if (!databaseName.EndsWith(']'))
                tablePrefixBuilder.Append(']');
            tablePrefixBuilder.Append(".[dbo].");
        }
        return tablePrefixBuilder.ToString();
    }

    private static string GetValueCaseAndSpaceInsensitive(in DbConnectionStringBuilder csBuilder, ReadOnlySpan<char> keyAndSynonyms)
    {
        Dictionary<string, string> builderKeys = csBuilder.Keys.Cast<string>().ToDictionary(x => x.Trim(), StringComparer.OrdinalIgnoreCase);
        while (!keyAndSynonyms.IsEmpty)
        {
            int separatorIndex = keyAndSynonyms.IndexOf(';');
            ReadOnlySpan<char> currentKey = separatorIndex == -1
                ? keyAndSynonyms
                : keyAndSynonyms[..separatorIndex];
            keyAndSynonyms = currentKey.Length + 1 >= keyAndSynonyms.Length
                ? ReadOnlySpan<char>.Empty
                : keyAndSynonyms[(currentKey.Length + 1)..];
            currentKey = currentKey.Trim();
            if (currentKey.Length == 0)
                continue;
            if (!builderKeys.TryGetValue(currentKey.ToString(), out string? actualKey))
                continue;
            if (!csBuilder.TryGetValue(actualKey, out object? actualValue))
                continue;
            if (actualValue is not string actualStringValue)
                continue;
            return actualStringValue.Trim();
        }
        return string.Empty;
    }
}
