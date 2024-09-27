using Dapper;
using Microsoft.AspNetCore.Identity;
using WebApi.Services;

namespace WebApi.Identity;

public class UserStore : IUserStore<IdentityUser>, IUserRoleStore<IdentityUser>, IUserPasswordStore<IdentityUser>, IUserEmailStore<IdentityUser>
{
    readonly IDatabase _database;

    public UserStore(IDatabase database)
    {
        _database = database;
    }

    public Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        return _database.InTransactionAsync(async (database, transaction) =>
        {
            var roles = await database.QueryAsync<IdentityRole>("SELECT * FROM IdentityRole WHERE Name = @Name", new { Name = roleName }, transaction);
            var role = roles.Single();
            await database.ExecuteAsync("INSERT INTO UserInRole VALUES (@UserId, @RoleId)", new { UserId = user.Id, RoleId = role.Id }, transaction);
        });
    }

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await _database.InTransactionAsync(async (database, transaction) =>
        {
            user.Id = Guid.NewGuid().ToString();
            await database.ExecuteAsync("INSERT INTO IdentityUser VALUES (@Id, @Email, @EmailConfirmed, @UserName)", user, transaction);
        });
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await _database.InTransactionAsync(async (database, transaction) =>
        {
            await database.ExecuteAsync("DELETE FROM IdentityUser WHERE Id = @Id", user, transaction);
        });
        return IdentityResult.Success;
    }

    public void Dispose()
    {
    }

    public Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _database.InTransactionAsync(normalizedEmail, (email, database, transaction) =>
        {
            return database.QueryFirstOrDefaultAsync<IdentityUser>(
                "SELECT * FROM IdentityUser WHERE Email ILIKE @Email",
                new { Email = email },
                transaction);
        });
    }

    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return _database.InTransactionAsync(userId, (id, database, transaction) =>
        {
            return database.QueryFirstOrDefaultAsync<IdentityUser>(
                "SELECT * FROM IdentityUser WHERE Id = @UserId",
                new { UserId = id },
                transaction);
        });
    }

    public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return _database.InTransactionAsync(normalizedUserName, (userName, database, transaction) =>
        {
            return database.QueryFirstOrDefaultAsync<IdentityUser>(
                "SELECT * FROM IdentityUser WHERE UserName ILIKE @UserName",
                new { UserName = userName },
                transaction);
        });
    }

    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<bool>(user.EmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.UserName.ToUpperInvariant());
    }

    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.PasswordHash);
    }

    public Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return _database.InTransactionAsync(user, async (identityUser, database, transaction) =>
        {
            var roles = await database.QueryAsync<IdentityRole>(
                @"SELECT IdentityRole.Name
                 FROM IdentityUser
                 JOIN UserInRole ON IdentityUser.Id = UserInRole.UserId
                 JOIN IdentityRole ON UserInRole.RoleId = IdentityRole.Id
                 WHERE IdentityUser.Id = @Id",
                user,
                transaction);
            return (IList<string>)roles.Select(role => role.Name).ToList();
        });
    }

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.UserName);
    }

    public Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}