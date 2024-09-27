using System.Data;
using Dapper;
using Npgsql;

namespace WebApi.Services;

public class Database : IDatabase
{
    public Database()
    {
        InTransaction((connection, transaction) =>
        {
            var exists = connection.ExecuteScalar<bool>(@"SELECT EXISTS ( 
                            SELECT FROM information_schema.tables 
                            WHERE table_name   = 'settings'
                            );", transaction);

            if (exists)
            {
                return false; //rollback
            }

            connection.Execute("CREATE TABLE Settings (version INTEGER)", transaction);
            connection.Execute("INSERT INTO Settings VALUES (1)", transaction);

            connection.Execute("CREATE TABLE RopuUser (name TEXT, email TEXT)", transaction);

            connection.Execute("CREATE TABLE IdentityUser (Id TEXT NOT NULL PRIMARY KEY, Email TEXT, EmailConfirmed BOOL, UserName TEXT, PasswordHash TEXT");

            connection.Execute("CREATE TABLE IdentityRole (Id TEXT NOT NULL PRIMARY KEY, Name TEXT");

            connection.Execute(
                @"CREATE TABLE UserInRole (
                    UserId INT FOREIGN KEY REFERENCES IdentityUser(Id), 
                    RoleId INT FOREIGN KEY REFERENCES IdentityRole(Id))");


            return true;
        });
    }

    public void InTransaction(Func<IDbConnection, IDbTransaction, bool> action)
    {
        using var connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=example;Database=Ropu");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            if (action(connection, transaction))
            {
                transaction.Commit();
                return;
            }
            transaction.Rollback();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task InTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action)
    {
        using var connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=example;Database=Ropu");
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await action(connection, transaction);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<T> InTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> func)
    {
        using var connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=example;Database=Ropu");
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            return await func(connection, transaction);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<T> InTransactionAsync<T, TIn>(TIn tin, Func<TIn, IDbConnection, IDbTransaction, Task<T>> func)
    {
        using var connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=example;Database=Ropu");
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            return await func(tin, connection, transaction);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}