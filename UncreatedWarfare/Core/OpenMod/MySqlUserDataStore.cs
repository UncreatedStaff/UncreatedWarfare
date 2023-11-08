using Microsoft.EntityFrameworkCore;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Uncreated.Warfare.API.Permissions;
using Uncreated.Warfare.Database.DbContexts;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Core.OpenMod;

[ServiceImplementation(Priority = Priority.High)]
public class MySqlUserDataStore : IUserDataStore
{
    private readonly UserDataStoreDbContext _userDataStoreContext;
    private UserData? _console;

    public MySqlUserDataStore(UserDataStoreDbContext userDataStoreContext)
    {
        _userDataStoreContext = userDataStoreContext;
    }

    public async Task<UserData?> GetUserDataAsync(string userId, string userType)
    {
        if (KnownActorTypes.Player.Equals(userType, StringComparison.Ordinal)
            && ulong.TryParse(userId, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
        {
            WarfareUserData? userData = await _userDataStoreContext.UserData.FirstOrDefaultAsync(x => x.Steam64 == steam64);
            if (userData == null)
                return null;

            return new UserData(userData.Steam64.ToString(CultureInfo.InvariantCulture), KnownActorTypes.Player)
            {
                FirstSeen = userData.FirstJoined,
                LastSeen = userData.LastJoined,
                Roles = new HashSet<string>(1) { userData.PermissionLevel.ToString() },
                Permissions = new HashSet<string>(0),
                LastDisplayName = userData.CharacterName
            };
        }

        if (KnownActorTypes.Console.Equals(userType, StringComparison.Ordinal))
        {
            _console ??= new UserData(userId, KnownActorTypes.Console)
            {
                Roles = new HashSet<string>(1) { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(0),
                LastDisplayName = "Console"
            };
        }

        if (KnownActorTypes.Rcon.Equals(userType, StringComparison.Ordinal))
        {
            _console ??= new UserData(userId, KnownActorTypes.Rcon)
            {
                Roles = new HashSet<string>(1) { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(0),
                LastDisplayName = "RCON"
            };
        }

        return null;
    }
    public Task SetUserDataAsync(UserData userData)
    {
        throw new NotImplementedException();
    }

    public Task<T?> GetUserDataAsync<T>(string userId, string userType, string key)
    {
        throw new NotImplementedException();
    }

    public Task SetUserDataAsync<T>(string userId, string userType, string key, T? value)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<UserData>> GetUsersDataAsync(string type)
    {
        throw new NotImplementedException();
    }

}
