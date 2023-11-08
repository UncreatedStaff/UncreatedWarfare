using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Uncreated.Warfare.API.Permissions;
using Uncreated.Warfare.API.Users;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Core.OpenMod;

[ServiceImplementation(Priority = Priority.High, Lifetime = ServiceLifetime.Singleton)]
public class MySqlUserDataStore : IUserDataStore
{
    private readonly IWarfareUserDataProvider _userData;
    private UserData? _console;
    private UserData? _rcon;

    public MySqlUserDataStore(IWarfareUserDataProvider userData)
    {
        _userData = userData;
    }

    public async Task<UserData?> GetUserDataAsync(string userId, string userType)
    {
        if (KnownActorTypes.Player.Equals(userType, StringComparison.Ordinal)
            && ulong.TryParse(userId, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
        {
            WarfareUserData? userData = await _userData.GetUserDataAsync(steam64);
            if (userData == null)
                return null;

            return new UserData(userData.Steam64.ToString(CultureInfo.InvariantCulture), KnownActorTypes.Player)
            {
                FirstSeen = userData.FirstJoined,
                LastSeen = userData.LastJoined,
#if NETSTANDARD
                Roles = new HashSet<string>(1) { userData.PermissionLevel.ToString() },
                Permissions = new HashSet<string>(0),
#else
                Roles = new HashSet<string> { userData.PermissionLevel.ToString() },
                Permissions = new HashSet<string>(),
#endif
                LastDisplayName = userData.CharacterName
            };
        }

        if (KnownActorTypes.Console.Equals(userType, StringComparison.Ordinal))
        {
            return _console ??= new UserData(userId, KnownActorTypes.Console)
            {
#if NETSTANDARD
                Roles = new HashSet<string>(1) { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(0),
#else
                Roles = new HashSet<string> { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(),
#endif
                LastDisplayName = "Console"
            };
        }

        if (KnownActorTypes.Rcon.Equals(userType, StringComparison.Ordinal))
        {
            return _rcon ??= new UserData(userId, KnownActorTypes.Rcon)
            {
#if NETSTANDARD
                Roles = new HashSet<string>(1) { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(0),
#else
                Roles = new HashSet<string> { nameof(PermissionLevel.Superuser) },
                Permissions = new HashSet<string>(),
#endif
                LastDisplayName = "RCON"
            };
        }

        return null;
    }
    public async Task SetUserDataAsync(UserData userData)
    {
        if (!KnownActorTypes.Player.Equals(userData.Type, StringComparison.Ordinal) ||
            !ulong.TryParse(userData.Id, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64))
            return;

        WarfareUserData? existingUserData = await _userData.GetUserDataAsync(steam64);

        if (existingUserData == null)
        {
            string names = userData.LastDisplayName ?? steam64.ToString("D17", CultureInfo.InvariantCulture);
            existingUserData = new WarfareUserData
            {
                Steam64 = steam64,
                CharacterName = names,
                NickName = names,
                PlayerName = names,
                PermissionLevel = PermissionLevel.Member,
                LastJoined = DateTime.UtcNow,
                FirstJoined = DateTime.UtcNow
            };
        }
        else
        {
            if (userData.LastSeen.HasValue && (!existingUserData.LastJoined.HasValue || userData.LastSeen.Value > existingUserData.LastJoined.Value))
            {
                existingUserData.LastJoined = userData.LastSeen.Value.ToUniversalTime();
            }
            if (userData.FirstSeen.HasValue && existingUserData.FirstJoined.HasValue && userData.FirstSeen.Value < existingUserData.FirstJoined.Value)
            {
                existingUserData.FirstJoined = userData.FirstSeen.Value.ToUniversalTime();
            }
        }

        await _userData.SaveUserDataAsync(existingUserData);
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
