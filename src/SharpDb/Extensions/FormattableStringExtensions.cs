using System.Text;

namespace SharpDb;

public static class FormattableStringExtensions
{
    public static string GetSqlCommandText(this FormattableString sql)
    {
        object?[] args = sql.GetArguments();
        if (args.Length == 0) return sql.Format;

        ReadOnlySpan<char> sqlSpan = sql.Format.AsSpan();
        StringBuilder sqlBuilder = new(sqlSpan.Length + 16);
        int parameterIndex = 0;

        while (!sqlSpan.IsEmpty)
        {
            int index = sqlSpan.IndexOf('{');

            // Add everything before the next '{'
            if (index < 0)
            {
                sqlBuilder.Append(sqlSpan);
                sqlSpan = [];
                continue;
            }
            sqlBuilder.Append(sqlSpan[..index]);
            sqlSpan = sqlSpan[index..];

            string parameterIndexString = parameterIndex.ToString();

            // Format parameter (or just add string if it's not parameter)
            if (sqlSpan.Length < 2 + parameterIndexString.Length
                || sqlSpan[0] != '{'
                || sqlSpan[parameterIndexString.Length + 1] != '}'
                || !sqlSpan[1..(parameterIndexString.Length + 1)].SequenceEqual(parameterIndexString))
            {
                sqlBuilder.Append('{');
                sqlSpan = sqlSpan[1..];
                continue;
            }
            sqlBuilder.Append($"@p{parameterIndex}");
            sqlSpan = sqlSpan[(2 + parameterIndexString.Length)..];
            parameterIndex++;
        }

        return sqlBuilder.ToString();
    }

    public static DbParameter[] GetSqlCommandParameters(this FormattableString sql)
    {
        object?[] args = sql.GetArguments();
        DbParameter[] parameters = new DbParameter[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            parameters[i] = new DbParameter($"@p{i}", args[i] ?? DBNull.Value);
        }
        return parameters;
    }
}
