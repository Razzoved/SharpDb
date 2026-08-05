using System.Runtime.CompilerServices;

namespace SharpDb;

public static class StringExtensions
{
    /// <summary>
    /// Tries to extract a single SQL query from a string of queries, identified by a specific tag.
    /// </summary>
    /// <param name="queries">Character span that contains one or more identifiable queries</param>
    /// <param name="querySelectorTag">Tag to search for</param>
    /// <returns>Extracted query(ies) or exception</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ReadOnlySpan<char> GetSingleQuery(this ReadOnlySpan<char> queries, ReadOnlySpan<char> querySelectorTag)
    {
        int startIdx = queries.IndexOf(querySelectorTag);
        if (startIdx < 0)
            throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_TagNotFound, querySelectorTag.ToString()));

        // skip tag
        startIdx += querySelectorTag.Length;

        // trim whitespace at start of query string
        while (startIdx < queries.Length)
        {
            if (!char.IsWhiteSpace(queries[startIdx])) break;
            startIdx++;
        }

        int currIdx = startIdx;
        int endIdx = -1;
        QueryParseContext ctx = QueryParseContext.None;

        while (currIdx < queries.Length)
        {
            switch (ctx, queries[currIdx])
            {
                // NONE
                case (QueryParseContext.None, ';'):
                    endIdx = currIdx;
                    currIdx = queries.Length; // breaks loop
                    break;
                case (QueryParseContext.None, '\''):
                    ctx = QueryParseContext.String;
                    endIdx = currIdx;
                    break;
                case (QueryParseContext.None, '-') when NextChar(queries, currIdx) == '-':
                    ctx = QueryParseContext.SingleLineComment;
                    currIdx++;
                    break;
                case (QueryParseContext.None, '/') when NextChar(queries, currIdx) == '*':
                    ctx = QueryParseContext.MultiLineComment;
                    currIdx++;
                    break;
                case (QueryParseContext.None, var c):
                    if (!char.IsWhiteSpace(c))
                        endIdx = currIdx;
                    break;

                // STRING
                case (QueryParseContext.String, '\'') when NextChar(queries, currIdx) == '\'':
                    endIdx = ++currIdx; // skip escaped single quote
                    break;
                case (QueryParseContext.String, '\''):
                    ctx = QueryParseContext.None; // end of string
                    endIdx = currIdx;
                    break;
                case (QueryParseContext.String, _):
                    endIdx = currIdx;
                    break;

                // SINGLE LINE COMMENT
                case (QueryParseContext.SingleLineComment, '\n'):
                    ctx = QueryParseContext.None; // EOL
                    break;
                case (QueryParseContext.SingleLineComment, _):
                    break;

                // MULTI LINE COMMENT
                case (QueryParseContext.MultiLineComment, '*') when NextChar(queries, currIdx) == '/':
                    ctx = QueryParseContext.None; // end of comment
                    currIdx++; // skip closing slash
                    break;
                case (QueryParseContext.MultiLineComment, _):
                    break;
            }
            currIdx++;
        }

        if (ctx != QueryParseContext.None && ctx != QueryParseContext.SingleLineComment)
            throw new InvalidOperationException(string.Format(Resources.Text_Error_QueryString_InvalidFormat, ctx));
        if (startIdx > endIdx || (startIdx == endIdx && queries[startIdx] == ';'))
            throw new InvalidOperationException(Resources.Text_Error_QueryString_Empty);

        return queries[startIdx..(endIdx + 1)];
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char NextChar(in ReadOnlySpan<char> src, int currentIndex)
        => currentIndex + 1 < src.Length ? src[currentIndex + 1] : '\0';

    private enum QueryParseContext
    {
        None,
        String,
        SingleLineComment,
        MultiLineComment
    }
}
