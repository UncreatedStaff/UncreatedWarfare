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
public class UserPermissionStore : IDisposable
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

    [RpcReceive]
    public void ClearCachedPermissions(ulong steam64)
    {
        _individualPermissionCache.TryRemove(steam64, out _);
        _permissionGroupCache.TryRemove(steam64, out _);
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
            .ToListAsync(token);

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

        PermissionGroupConfig config = JsonSerializer.Deserialize<PermissionGroupConfig>(stream.ReadAllBytes(), JsonSettings.SerializerSettings);
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

    void IDisposable.Dispose()
    {
        _permissionGroupFileWatcher?.Dispose();
    }
}
