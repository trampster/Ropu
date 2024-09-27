using System.Data;

namespace WebApi.Services;

public interface IDatabase
{
    void InTransaction(Func<IDbConnection, IDbTransaction, bool> action);

    Task InTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action);

    Task<T> InTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> func);

    Task<T> InTransactionAsync<T, TIn>(TIn tin, Func<TIn, IDbConnection, IDbTransaction, Task<T>> func);
}