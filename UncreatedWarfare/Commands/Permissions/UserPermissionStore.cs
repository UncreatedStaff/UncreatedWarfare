using DanielWillett.ModularRpcs.Annotations;
using Microsoft.EntityFrameworkCore;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Commands.Permissions;

[RpcClass]
public class UserPermissionStore : IAsyncDisposable
{
    private readonly ConcurrentDictionary<ulong, ReadOnlyCollection<PermissionBranch>> _individualPermissionCache = new ConcurrentDictionary<ulong, ReadOnlyCollection<PermissionBranch>>();
    private readonly ConcurrentDictionary<ulong, ReadOnlyCollection<PermissionGroup>> _permissionGroupCache = new ConcurrentDictionary<ulong, ReadOnlyCollection<PermissionGroup>>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private readonly string _permissionGroupFilePath;
    private readonly IDisposable? _permissionGroupFileWatcher;

    private readonly WarfareDbContext _dbContext;

    /// <summary>
    /// List of all permission groups from config.
    /// </summary>
    public IReadOnlyList<PermissionGroup> PermissionGroups { get; private set; }

    public UserPermissionStore(WarfareDbContext dbContext)
    {
        _dbContext = dbContext;
        PermissionGroups = null!;
        _permissionGroupFilePath = Path.Join(".", "Permission Groups.json");
        ReadPermissionGroups();
        _permissionGroupFileWatcher = ConfigurationHelper.ListenForFileUpdate(_permissionGroupFilePath, ReadPermissionGroups);
    }

    /// <summary>
    /// Clear all cached permissions for a player. If <paramref name="steam64"/> is 0, all cached permissions will be cleared.
    /// </summary>
    [RpcReceive]
    public void ClearCachedPermissions(ulong steam64)
    {
        if (steam64 == 0)
        {
            _individualPermissionCache.Clear();
            _permissionGroupCache.Clear();
        }
        else
        {
            _individualPermissionCache.TryRemove(steam64, out _);
            _permissionGroupCache.TryRemove(steam64, out _);
        }
    }

    /// <summary>
    /// Remove a permission group from <paramref name="player"/>.
    /// </summary>
    /// <returns><see langword="true"/> if more than zero groups were removed.</returns>
    public virtual async Task<bool> RemovePermissionGroupAsync(CSteamID player, string permissionGroupId, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && perm.IsGroup && perm.PermissionOrGroup == permissionGroupId)
                .ToListAsync(token)
                .ConfigureAwait(false);

            _dbContext.Permissions.RemoveRange(dbPerms);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return dbPerms.Count > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Remove an individual permission from <paramref name="player"/>.
    /// </summary>
    /// <returns><see langword="true"/> if more than zero permissions were removed.</returns>
    public virtual async Task<bool> RemovePermissionAsync(CSteamID player, PermissionBranch permission, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string permissionStr = permission.ToString();
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && !perm.IsGroup && perm.PermissionOrGroup == permissionStr)
                .ToListAsync(token)
                .ConfigureAwait(false);

            _dbContext.Permissions.RemoveRange(dbPerms);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return dbPerms.Count > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Remove multiple permission groups from <paramref name="player"/>.
    /// </summary>
    /// <returns>Number of groups that were removed.</returns>
    public virtual async Task<int> RemovePermissionGroupsAsync(CSteamID player, IEnumerable<string> permissionGroupIds, CancellationToken token = default)
    {
        List<string> permGroups = permissionGroupIds.ToList();
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && perm.IsGroup && permGroups.Contains(perm.PermissionOrGroup))
                .ToListAsync(token)
                .ConfigureAwait(false);

            _dbContext.Permissions.RemoveRange(dbPerms);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return dbPerms.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Remove multiple individual permissions from <paramref name="player"/>.
    /// </summary>
    /// <returns>Number of permissions that were removed.</returns>
    public virtual async Task<int> RemovePermissionsAsync(CSteamID player, IEnumerable<PermissionBranch> permissions, CancellationToken token = default)
    {
        List<string> perms = permissions.Select(perm => perm.ToString()).ToList();
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && !perm.IsGroup && perms.Contains(perm.PermissionOrGroup))
                .ToListAsync(token)
                .ConfigureAwait(false);

            _dbContext.Permissions.RemoveRange(dbPerms);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return dbPerms.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Add a permission group to <paramref name="player"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the group wasn't already there.</returns>
    public virtual async Task<bool> AddPermissionGroupAsync(CSteamID player, string permissionGroupId, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && perm.IsGroup && perm.PermissionOrGroup == permissionGroupId)
                .ToListAsync(token)
                .ConfigureAwait(false);

            if (dbPerms.Count > 0)
            {
                return false;
            }

            _dbContext.Permissions.Add(new Permission
            {
                IsGroup = true,
                PermissionOrGroup = permissionGroupId,
                Steam64 = player.m_SteamID
            });

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Add an individual permission to <paramref name="player"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the permission wasn't already there.</returns>
    public virtual async Task<bool> AddPermissionAsync(CSteamID player, PermissionBranch permission, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string permissionStr = permission.ToString();
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && !perm.IsGroup && perm.PermissionOrGroup == permissionStr)
                .ToListAsync(token)
                .ConfigureAwait(false);

            if (dbPerms.Count > 0)
            {
                return false;
            }

            _dbContext.Permissions.Add(new Permission
            {
                IsGroup = false,
                PermissionOrGroup = permissionStr,
                Steam64 = player.m_SteamID
            });

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Add multiple permission groups to <paramref name="player"/>.
    /// </summary>
    /// <returns>Number of groups that weren't already present.</returns>
    public virtual async Task<int> AddPermissionGroupsAsync(CSteamID player, IEnumerable<string> permissionGroupIds, CancellationToken token = default)
    {
        List<string> permGroups = permissionGroupIds.ToList();
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && perm.IsGroup && permGroups.Contains(perm.PermissionOrGroup))
                .ToListAsync(token)
                .ConfigureAwait(false);

            int ct = 0;
            foreach (string permGroup in permGroups)
            {
                if (dbPerms.Any(perm => perm.PermissionOrGroup.Equals(permGroup, StringComparison.Ordinal)))
                    continue;

                ++ct;
                _dbContext.Permissions.Add(new Permission
                {
                    IsGroup = true,
                    PermissionOrGroup = permGroup,
                    Steam64 = player.m_SteamID
                });
            }

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return ct;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Add multiple individual permissions to <paramref name="player"/>.
    /// </summary>
    /// <returns>Number of permissions that weren't already present.</returns>
    public virtual async Task<int> AddPermissionsAsync(CSteamID player, IEnumerable<PermissionBranch> permissions, CancellationToken token = default)
    {
        List<string> perms = permissions.Select(perm => perm.ToString()).ToList();
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<Permission> dbPerms = await _dbContext.Permissions
                .Where(perm => perm.Steam64 == player.m_SteamID && !perm.IsGroup && perms.Contains(perm.PermissionOrGroup))
                .ToListAsync(token)
                .ConfigureAwait(false);

            int ct = 0;
            foreach (string permGroup in perms)
            {
                if (dbPerms.Any(perm => perm.PermissionOrGroup.Equals(permGroup, StringComparison.Ordinal)))
                    continue;

                ++ct;
                _dbContext.Permissions.Add(new Permission
                {
                    IsGroup = false,
                    PermissionOrGroup = permGroup,
                    Steam64 = player.m_SteamID
                });
            }

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            ClearCachedPermissions(player.m_SteamID);
            return ct;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Get a list of all individual permissions assigned to a player.
    /// </summary>
    public virtual ValueTask<IReadOnlyList<PermissionBranch>> GetPermissionsAsync(CSteamID player, bool forceRedownload = false, CancellationToken token = default)
    {
        if (!forceRedownload && _individualPermissionCache.TryGetValue(player.m_SteamID, out ReadOnlyCollection<PermissionBranch> permissions))
        {
            return new ValueTask<IReadOnlyList<PermissionBranch>>(permissions);
        }

        return new ValueTask<IReadOnlyList<PermissionBranch>>(CachePlayerIndividual(player, token));
    }

    /// <summary>
    /// Get a list of all permission groups assigned to a player already in order of priority.
    /// </summary>
    public virtual ValueTask<IReadOnlyList<PermissionGroup>> GetPermissionGroupsAsync(CSteamID player, bool forceRedownload = false, CancellationToken token = default)
    {
        if (!forceRedownload && _permissionGroupCache.TryGetValue(player.m_SteamID, out ReadOnlyCollection<PermissionGroup> groups))
        {
            return new ValueTask<IReadOnlyList<PermissionGroup>>(groups);
        }

        return new ValueTask<IReadOnlyList<PermissionGroup>>(CachePlayerGroups(player, token));
    }

    /// <summary>
    /// Check if a user has <paramref name="permission"/>, or the <see cref="PermissionBranch.Superuser"/> permission.
    /// </summary>
    public async ValueTask<bool> HasPermissionAsync(ICommandUser user, PermissionLeaf permission, CancellationToken token = default)
    {
        if (user.IsSuperUser)
            return true;

        IReadOnlyList<PermissionBranch> individualPerms = await GetPermissionsAsync(user.Steam64, token: token).ConfigureAwait(false);

        bool valid = permission.Valid;

        bool hasRemovedSuperuser = false;
        bool hasSubbed = false, hasAdded = false;

        bool? result = CheckPermissionList(valid, permission, individualPerms, ref hasRemovedSuperuser, ref hasSubbed, ref hasAdded);
        if (result.HasValue)
            return result.Value;

        IReadOnlyList<PermissionGroup> permGroups = await GetPermissionGroupsAsync(user.Steam64, token: token).ConfigureAwait(false);

        foreach (PermissionGroup group in permGroups)
        {
            result = CheckPermissionList(valid, permission, group.Permissions, ref hasRemovedSuperuser, ref hasSubbed, ref hasAdded);
            if (result.HasValue)
                return result.Value;
        }

        return false;
    }

    private static bool? CheckPermissionList(bool valid, PermissionLeaf permission, IReadOnlyList<PermissionBranch> permissions, ref bool hasRemovedSuperuser, ref bool hasSubbed, ref bool hasAdded)
    {
        for (int i = permissions.Count - 1; i >= 0; i--)
        {
            PermissionBranch branch = permissions[i];
            if (!hasRemovedSuperuser && branch.IsSuperuser)
            {
                if (branch.Mode == PermissionMode.Subtractive)
                {
                    hasRemovedSuperuser = true;
                }
                else
                {
                    return true;
                }
            }

            if (!valid || !branch.Contains(permission))
                continue;

            if (branch.Mode == PermissionMode.Subtractive)
            {
                hasSubbed = true;
            }
            else if (branch.Mode == PermissionMode.Additive)
            {
                hasAdded = true;
                hasSubbed = false;
            }
        }

        if (hasAdded && !hasSubbed)
            return true;

        if (hasSubbed)
            return false;

        return null;
    }

    private async Task<IReadOnlyList<PermissionBranch>> CachePlayerIndividual(CSteamID player, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            (_, PermissionBranch[] branches) = await CachePlayerIntl(player, token).ConfigureAwait(false);
            return branches;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IReadOnlyList<PermissionGroup>> CachePlayerGroups(CSteamID player, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            (PermissionGroup[] groups, _) = await CachePlayerIntl(player, token).ConfigureAwait(false);
            return groups;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<(PermissionGroup[], PermissionBranch[])> CachePlayerIntl(CSteamID player, CancellationToken token = default)
    {
        List<Permission> dbPerms = await _dbContext.Permissions
            .Where(perm => perm.Steam64 == player.m_SteamID)
            .ToListAsync(token)
            .ConfigureAwait(false);

        int groupCt = dbPerms.Count(x => x.IsGroup);

        PermissionGroup[] groups = new PermissionGroup[groupCt];
        PermissionBranch[] branches = new PermissionBranch[dbPerms.Count - groupCt];

        return (groups, branches);
    }

    private void ReadPermissionGroups()
    {
        ThreadUtil.assertIsGameThread();

        if (!File.Exists(_permissionGroupFilePath))
        {
            PermissionGroups = Array.Empty<PermissionGroup>();
            return;
        }

        using Utf8JsonPreProcessingStream stream = new Utf8JsonPreProcessingStream(_permissionGroupFilePath);

        PermissionGroupConfig config = JsonSerializer.Deserialize<PermissionGroupConfig>(stream.ReadAllBytes(), ConfigurationSettings.JsonSerializerSettings);
        config.Groups.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        if (PermissionGroups != null)
        {
            // use existing instances in case there are references somewhere
            for (int i = 0; i < PermissionGroups.Count; ++i)
            {
                PermissionGroup group = PermissionGroups[i];
                int newInd = config.Groups.FindIndex(x => x.Id.Equals(group.Id, StringComparison.Ordinal));
                if (newInd == -1)
                    continue;

                group.UpdateFrom(config.Groups[newInd]);
                config.Groups[newInd] = group;
            }
        }

        PermissionGroups = new ReadOnlyCollection<PermissionGroup>(config.Groups);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        _permissionGroupFileWatcher?.Dispose();

        try
        {
            await _semaphore.WaitAsync(TimeSpan.FromSeconds(3d));
        }
        catch { /* ignored */ }
        finally
        {
            _semaphore.Dispose();
        }
    }
}
