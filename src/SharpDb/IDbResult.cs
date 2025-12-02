namespace SharpDb;

public interface IDbResult
{
    bool IsSuccess { get; }
    IDbError Error { get; }
}

/// <summary>
/// Result to be returned by operations that execute operations on the database,
/// but do not return any data.
/// </summary>
public readonly struct DbExecResult : IDbResult
{
    private DbExecResult(bool isSuccess, int affectedRows, in IDbError error)
    {
        IsSuccess = isSuccess;
        Error = error;
        AffectedRows = affectedRows;
    }

    public bool IsSuccess { get; }
    public IDbError Error { get; }
    public int AffectedRows { get; }

    public static DbExecResult Success(int affectedRows = 0) => new(true, affectedRows, NoDbError.Instance);
    public static DbExecResult Failure(in IDbError error) => new(false, 0, error);
    public static DbExecResult Failure(int affectedRows, in IDbError error) => new(false, affectedRows, error);
}

/// <summary>
/// Result to be returned by operations that query data from the database.
/// </summary>
/// <typeparam name="T">Type of queried data</typeparam>
public readonly struct DbQueryResult<T> : IDbResult
{
    private DbQueryResult(bool isSuccess, T data, in IDbError error)
    {
        IsSuccess = isSuccess;
        Error = error;
        Data = data;
    }

    public bool IsSuccess { get; }
    public IDbError Error { get; }
    public T Data { get; }

    public static DbQueryResult<T> Success(T data) => new(true, data, NoDbError.Instance);
    public static DbQueryResult<T> Failure(in IDbError error) => new(false, GetDefaultData(), error);
    public static DbQueryResult<T> Failure(T data, in IDbError error) => new(false, data, error);

    private static T GetDefaultData()
    {
        Type typeOfT = typeof(T);
        if (!typeOfT.IsValueType && Nullable.GetUnderlyingType(typeOfT) is null)
        {
            if (typeOfT == typeof(string))
                return (T)(object)"";
            if (typeOfT.GetConstructor(Type.EmptyTypes) is not null)
            {
                if (typeOfT.IsArray || typeOfT.IsAssignableTo(typeof(System.Collections.IEnumerable)))
                    return (T)Activator.CreateInstance(typeOfT)!;
            }
        }
        return default!;
    }
}

/// <summary>
/// Result to be returned by transaction operations over the database.
/// </summary>
public readonly struct DbTransactionResult : IDbResult
{
    private DbTransactionResult(bool isSuccess, long affectedRows, in IDbError error)
    {
        IsSuccess = isSuccess;
        AffectedRows = affectedRows;
        Error = error;
    }

    public bool IsSuccess { get; }
    public long AffectedRows { get; }
    public IDbError Error { get; }

    public static DbTransactionResult Success(long affectedRows) => new(true, affectedRows, NoDbError.Instance);
    public static DbTransactionResult Failure(in IDbError error) => new(false, 0, error);
}

