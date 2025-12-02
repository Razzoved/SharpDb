namespace SharpDb.Extensions;

public static class StringExtensions
{
    private const char BeginSlComment = '-';
    private const char BeginMlComment = '/';
    private const char BeginString = '\'';

    /// <summary>
    /// Tries to extract a single SQL query from a string of queries, identified by a specific tag.
    /// </summary>
    /// <param name="queries">Character span that contains one or more identifiable queries</param>
    /// <param name="querySelectorTag">Tag to search for</param>
    /// <returns>Extracted query(ies) or exception</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ReadOnlySpan<char> GetSingleQuery(this ReadOnlySpan<char> queries, ReadOnlySpan<char> querySelectorTag)
    {
        int readPos = queries.IndexOf(querySelectorTag);
        if (readPos < 0)
            throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_TagNotFound, querySelectorTag.ToString()));

        // trim everything before the index including the tag
        readPos += querySelectorTag.Length;
        while (queries.Length > readPos && char.IsWhiteSpace(queries[readPos]))
            readPos++;
        var query = queries[readPos..];

        bool hasAnyNonWhitespace = false;
        char context = '\0'; // '\0' = none, '\'' = string, '-' = single-line comment, '*' = multi-line comment

        for (readPos = 0; readPos < query.Length; readPos++)
        {
            char c = query[readPos];
            switch (context)
            {
                case BeginString:
                    if (c == '\'')
                    {
                        if (readPos + 1 < query.Length && query[readPos + 1] == '\'')
                            readPos++; // escaped single quote
                        else
                            context = '\0'; // end of string
                    }
                    continue;
                case BeginSlComment:
                    if (c == '\n')
                        context = '\0';
                    continue;
                case BeginMlComment:
                    if (c == '*' && readPos + 1 < query.Length && query[readPos + 1] == '/')
                    {
                        context = '\0';
                        readPos++;
                    }
                    continue;
            }

            switch (c)
            {
                case BeginString:
                    hasAnyNonWhitespace |= true;
                    context = BeginString;
                    break;
                case BeginSlComment when readPos + 1 < query.Length && query[readPos + 1] == '-':
                    context = BeginSlComment;
                    readPos++;
                    break;
                case BeginMlComment when readPos + 1 < query.Length && query[readPos + 1] == '*':
                    context = BeginMlComment;
                    readPos++;
                    break;
                case ';' when context == '\0':
                    goto EndQuery;
                default:
                    hasAnyNonWhitespace |= !char.IsWhiteSpace(c);
                    break;
            }
        }

    EndQuery:
        if (readPos < query.Length)
            query = query[..readPos];
        query = query.Trim();

        if (context != '\0' && context != BeginSlComment)
            throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_InvalidFormat, context));
        if (query.Length == 0 || !hasAnyNonWhitespace)
            throw new InvalidOperationException(Resources.Text_Error_QueryString_Empty);

        return query;
    }

    /// <param name="queries">String that contains one or more identifiable queries</param>
    /// <inheritdoc cref="GetSingleQuery(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    public static ReadOnlySpan<char> GetSingleQuery(this string queries, ReadOnlySpan<char> querySelectorTag)
        => queries.AsSpan().GetSingleQuery(querySelectorTag);

    /// <param name="queries">Span that contains one or more identifiable queries</param>
    /// <param name="requiredParameter">A parameter that must exist in the extracted query</param>
    /// <inheritdoc cref="GetSingleQuery(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    public static ReadOnlySpan<char> GetSingleQuery(this ReadOnlySpan<char> queries, ReadOnlySpan<char> querySelectorTag, ReadOnlySpan<char> requiredParameter)
    {
        var query = queries.GetSingleQuery(querySelectorTag);

        // verify that the required parameter exists in the query
        int index = query.IndexOf(requiredParameter);
        if (index < 0)
            throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_TagNotFound, requiredParameter.ToString()));

        // verify that the required parameter is not commented out
        while (--index > 0 && query[index] != '\n')
        {
            var commentSlice = query.Slice(index, 2);
            if (commentSlice.StartsWith("--") || commentSlice.StartsWith("/*"))
                throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_TagCommentedOut, requiredParameter.ToString()));
        }

        return query;
    }

    /// <param name="queries">String that contains one or more identifiable queries</param>
    /// <inheritdoc cref="GetSingleQuery(ReadOnlySpan{char}, ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    public static ReadOnlySpan<char> GetSingleQuery(this string queries, ReadOnlySpan<char> querySelectorTag, ReadOnlySpan<char> requiredParameter)
        => queries.AsSpan().GetSingleQuery(querySelectorTag, requiredParameter);
}
