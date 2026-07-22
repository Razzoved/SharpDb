using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SharpDb.Exceptions;

namespace SharpDb.EntityFrameworkCore;

public static partial class QueryableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DbContext? GetDbContext<T>(IQueryable<T> query)
    {
        if (query is IInfrastructure<IServiceProvider> directInfrastructure)
            return directInfrastructure.Instance.GetService<ICurrentDbContext>()?.Context;

        Expression expression = query.Expression;
        while (expression is MethodCallExpression methodCall)
            expression = methodCall.Arguments[0]; // arguments[0] is always the source IQueryable
        if (expression is UnaryExpression unary)
            expression = unary.Operand;
        if (expression is ConstantExpression { Value: IInfrastructure<IServiceProvider> rootInfrastructure })
            return rootInfrastructure.Instance.GetService<ICurrentDbContext>()?.Context;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfTransient<T>(IQueryable<T> query, Exception e)
    {
        if (e is TransactionTransientException)
            throw e;
        try
        {
            if (GetDbContext(query) is { } context
                && TransactionContext.GetCurrent(context.Database) is not null
                && e.HasTransientDbError())
                throw new TransactionTransientException(e);
        }
        catch { }
    }
}
