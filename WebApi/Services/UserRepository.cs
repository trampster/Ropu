using Dapper;
using WebApi.Models;

namespace WebApi.Services;

public class UserRepository
{
    readonly IDatabase _database;

    public UserRepository(IDatabase database)
    {
        _database = database;
    }

    public Task AddUser(User user)
    {
        return _database.InTransactionAsync((connection, transaction) =>
        {
            return connection.ExecuteAsync("INSERT INTO RopuUser VALUES (@Name, @Email)", user, transaction);
        });
    }

    public Task<IEnumerable<User>> All()
    {
        return _database.InTransactionAsync((connection, transaction) =>
        {
            return connection.QueryAsync<User>("SELECT * FROM RopuUser", transaction);
        })!;
    }
}