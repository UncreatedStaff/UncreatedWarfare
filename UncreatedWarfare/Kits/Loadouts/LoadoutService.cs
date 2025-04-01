using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Loadouts;

public class LoadoutService
{
    private readonly IKitDataStore _kitSql;
    private readonly IConfiguration _systemConfig;
    private readonly EventDispatcher? _eventDispatcher;
    private readonly IPlayerService? _playerService;
    private readonly IRpcConnectionService? _rpcConnectionService;
    private readonly DefaultLoadoutItemsConfiguration? _loadoutItemsConfiguration;

    private readonly ILogger<LoadoutService> _logger;

    /// <summary>
    /// Cost of a loadout in USD.
    /// </summary>
    public decimal LoadoutCost => _systemConfig.GetValue("kits:loadout_cost_usd", 10m);

    public event EventHandler<LoadoutCreated>? LoadoutCreated;
    public event EventHandler<LoadoutLockChanged>? LoadoutLockChanged;
    public event EventHandler<LoadoutUpgradeStarted>? LoadoutUpgradeStarted;

    public LoadoutService(IServiceProvider serivceProvider, ILogger<LoadoutService> logger)
    {
        _kitSql = serivceProvider.GetRequiredService<IKitDataStore>();
        _systemConfig = serivceProvider.GetRequiredService<IConfiguration>();
        _loadoutItemsConfiguration = serivceProvider.GetService<DefaultLoadoutItemsConfiguration>();

        if (WarfareModule.IsActive)
        {
            _playerService = serivceProvider.GetRequiredService<IPlayerService>();
            _eventDispatcher = serivceProvider.GetRequiredService<EventDispatcher>();
        }
        else
        {
            _rpcConnectionService = serivceProvider.GetRequiredService<IRpcConnectionService>();
        }

        _logger = logger;
    }

    /// <summary>
    /// Attempts to open a ticket in the discord for the desired player to upgrade their loadout.
    /// </summary>
    [RpcSend, RpcTimeout(15 * Timeouts.Seconds)]
    public virtual RpcTask<OpenUpgradeTicketResult> TryOpenUpgradeTicket(ulong discordId, CSteamID steam64, int loadoutLetter, Class newClass, string newDisplayName)
    {
        return RpcTask<OpenUpgradeTicketResult>.NotImplemented;
    }

    /// <summary>
    /// Get the loadout represented by a specific number (usually this is a sign index, starting from 1).
    /// </summary>
    public async Task<Kit?> GetLoadoutFromNumber(CSteamID player, int number, KitInclude include, CancellationToken token = default)
    {
        if (number == 0)
            return null;

        ulong s64 = player.m_SteamID;

        Kit? kit = await _kitSql.QueryKitAsync(include, kits => 
            kits.Where(x => x.Type == KitType.Loadout && x.Access.Any(a => a.Steam64 == s64))
                .OrderByDescending(x => x.Favorites.Any(f => f.Steam64 == s64))
                .ThenBy(x => x.Id)
                .Skip(number - 1)
            , token
        );

        if (!WarfareModule.IsActive || (include & KitInclude.Cached) != KitInclude.Cached || kit == null)
            return kit;

        WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
        if (onlinePlayer != null)
        {
            KitPlayerComponent comp = onlinePlayer.Component<KitPlayerComponent>();
            comp.UpdateLoadout(kit);
        }

        return kit;
    }

    /// <summary>
    /// Get all loadouts a player owns in the order they would be displayed on signs.
    /// </summary>
    public async Task<IReadOnlyList<Kit>> GetLoadouts(CSteamID player, KitInclude include, CancellationToken token = default)
    {
        ulong s64 = player.m_SteamID;

        Kit[] loadouts = await _kitSql.QueryKitsAsync(include, kits => 
            kits.Where(x => x.Type == KitType.Loadout && x.Access.Any(a => a.Steam64 == s64))
                .OrderByDescending(x => x.Favorites.Any(f => f.Steam64 == s64))
                .ThenBy(x => x.Id)
            , token
        ).ConfigureAwait(false);

        if (!WarfareModule.IsActive || (include & KitInclude.Cached) != KitInclude.Cached)
            return loadouts;

        WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
        onlinePlayer?.Component<KitPlayerComponent>().UpdateLoadouts(loadouts);

        return new ReadOnlyCollection<Kit>(loadouts);
    }

    /// <summary>
    /// Get all loadouts a player owns in the order they would be displayed on signs.
    /// </summary>
    public async Task GetLoadouts(IList<Kit> output, CSteamID player, KitInclude include, CancellationToken token = default)
    {
        ulong s64 = player.m_SteamID;

        await _kitSql.QueryKitsAsync(include, output, kits => 
            kits.Where(x => x.Type == KitType.Loadout && x.Access.Any(a => a.Steam64 == s64))
                .OrderByDescending(x => x.Favorites.Any(f => f.Steam64 == s64))
                .ThenBy(x => x.Id)
            , token
        ).ConfigureAwait(false);

        if (!WarfareModule.IsActive || (include & KitInclude.Cached) != KitInclude.Cached)
            return;

        WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
        onlinePlayer?.Component<KitPlayerComponent>().UpdateLoadouts(output.ToList());
        
    }

    /// <summary>
    /// Enables a loadout, usually after creating or upgrading a kit.
    /// </summary>
    /// <param name="instigator">The admin handling the ticket.</param>
    /// <param name="primaryKey">The primary key of the existing loadout.</param>
    /// <returns>The kit, or <see langword="null"/> if its not found.</returns>
    public async Task<Kit?> UnlockLoadoutAsync(CSteamID instigator, uint primaryKey, KitInclude include = KitInclude.Default, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                if (!await SendUnlockLoadout(connection, instigator, primaryKey, token))
                {
                    return null;
                }

                Kit? remoteKit = await _kitSql.QueryKitAsync(primaryKey, include, token).ConfigureAwait(false);
                return remoteKit;
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to unlock loadout remotely (higher chance of concurrency issues).");
            }
        }

        if (instigator.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            instigator = default;

        bool wasDisabled = true;
        Kit? kit = await _kitSql.UpdateKitAsync(primaryKey, include, kit =>
        {
            wasDisabled = kit.Disabled;
            if (wasDisabled)
                // todo: ActionLog.Add(ActionLogType.UnlockLoadout, kit.Id, kit.LastEditor);
            kit.Disabled = false;
        }, instigator, token).ConfigureAwait(false);

        if (kit == null)
            return null;

        try
        {
            SendInvokeLoadoutLockStateUpdated(kit.Key, false);
        }
        catch (RpcNoConnectionsException) { }

        if (wasDisabled)
        {
            LoadoutIdHelper.Parse(kit.Id, out CSteamID id);
            await InvokeLoadoutLockChanged(kit, id).ConfigureAwait(false);
        }

        return kit;
    }

    /// <summary>
    /// Disables a loadout, usually to allow an admin to make changes to it.
    /// </summary>
    /// <param name="instigator">The admin handling the ticket.</param>
    /// <param name="primaryKey">The primary key of the existing loadout.</param>
    /// <returns>The kit, or <see langword="null"/> if its not found.</returns>
    public async Task<Kit?> LockLoadoutAsync(CSteamID instigator, uint primaryKey, KitInclude include = KitInclude.Default, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                if (!await SendLockLoadout(connection, instigator, primaryKey, token))
                {
                    return null;
                }

                Kit? remoteKit = await _kitSql.QueryKitAsync(primaryKey, include, token).ConfigureAwait(false);
                return remoteKit;
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to lock loadout remotely (higher chance of concurrency issues).");
            }
        }

        if (instigator.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            instigator = default;

        bool wasDisabled = false;
        Kit? kit = await _kitSql.UpdateKitAsync(primaryKey, include, kit =>
        {
            wasDisabled = kit.Disabled;
            if (!wasDisabled)
                // todo: ActionLog.Add(ActionLogType.LockLoadout, kit.Id, kit.LastEditor);
            kit.Disabled = true;
        }, instigator, token).ConfigureAwait(false);

        if (kit == null)
            return null;

        try
        {
            SendInvokeLoadoutLockStateUpdated(kit.Key, true);
        }
        catch (RpcNoConnectionsException) { }

        if (!wasDisabled)
        {
            LoadoutIdHelper.Parse(kit.Id, out CSteamID id);
            await InvokeLoadoutLockChanged(kit, id).ConfigureAwait(false);
        }

        return kit;
    }

    /// <summary>
    /// Create a new loadout for this season which will start locked.
    /// </summary>
    /// <param name="forPlayer">The player who will own the loadout.</param>
    /// <param name="creator">The admin who is handling the ticket, or default.</param>
    /// <param name="displayName">Optional sign text to use for the default language.</param>
    /// <returns>The created kit.</returns>
    public async Task<Kit> CreateLoadoutAsync(CSteamID forPlayer, CSteamID creator, Class @class, string? displayName, Func<KitModel, Task>? updateTask, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                uint pk = await SendCreateLoadout(connection, forPlayer, creator, @class, displayName, token);

                Kit? remoteKit = await _kitSql.QueryKitAsync(pk, KitInclude.All, token).ConfigureAwait(false);
                if (remoteKit != null)
                    return remoteKit;
                
                _logger.LogWarning($"Kit {pk} deleted before able to be used by local.");
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to create loadout remotely (higher chance of concurrency issues).");
            }
        }

        if (forPlayer.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            throw new ArgumentException("Expected valid Steam64 ID.", nameof(forPlayer));

        EnumUtility.AssertValidField(@class, nameof(@class), Class.None, Class.Unarmed);

        if (creator.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            creator = default;

        Kit kit;
        while (true)
        {
            int freeId = await GetFreeLoadoutIdAsync(forPlayer, token).ConfigureAwait(false);

            try
            {
                string thisDisplayName = displayName ?? LoadoutIdHelper.GetLoadoutDefaultKitDisplayText(freeId);
                kit = await _kitSql.AddKitAsync(LoadoutIdHelper.GetLoadoutName(forPlayer, freeId), @class, thisDisplayName, creator, async kit =>
                {
                    kit.Type = KitType.Loadout;
                    kit.PremiumCost = LoadoutCost;
                    kit.Disabled = true;
                    kit.Access.Add(new KitAccess
                    {
                        Steam64 = forPlayer.m_SteamID,
                        AccessType = KitAccessType.Purchase,
                        Kit = kit,
                        Timestamp = kit.CreatedAt
                    });

                    if (updateTask != null)
                    {
                        await updateTask(kit).ConfigureAwait(false);
                    }

                    if (kit.Items == null || kit.Items.Count == 0)
                    {
                        SetDefaultItems(kit);
                    }
                }, token).ConfigureAwait(false);
                
                _logger.LogInformation($"Loadout created: {kit.Id} ({kit.Key}): \"{thisDisplayName}\" by {kit.CreatingPlayer}.");

                try
                {
                    SendInvokeLoadoutCreated(forPlayer, freeId, kit.Key);
                }
                catch (RpcNoConnectionsException) { }
                await InvokeLoadoutCreated(kit, forPlayer, freeId).ConfigureAwait(false);
                break;
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, "kitId", StringComparison.Ordinal))
            {
                _logger.LogWarning("Duplicate loadout ID, likely caused by concurrency issue. Trying again.");
            }
        }

        return kit;
    }

    /// <summary>
    /// Start upgrading a loadout to the current season.
    /// </summary>
    /// <param name="admin">The admin that handled the ticket.</param>
    /// <param name="forPlayer">The owner of the loadout.</param>
    /// <param name="class">The new class to set for the loadout.</param>
    /// <returns>The upgraded kit.</returns>
    /// <exception cref="InvalidOperationException">Kit is already up to date.</exception>
    public async Task<Kit?> UpgradeLoadoutAsync(CSteamID forPlayer, CSteamID admin, Class @class, uint kitPk, KitInclude include = KitInclude.Default, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                if (!await SendUpgradeLoadout(connection, forPlayer, admin, @class, kitPk, token))
                    return null;

                Kit? remoteKit = await _kitSql.QueryKitAsync(kitPk, include, token).ConfigureAwait(false);
                return remoteKit;
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to upgrade loadout remotely (higher chance of concurrency issues).");
            }
        }

        if (forPlayer.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            throw new ArgumentException("Expected valid Steam64 ID.", nameof(forPlayer));

        Class oldClass = 0;
        Faction? oldFaction = null;
        bool accessAdded = false;
        Kit? kit = await _kitSql.UpdateKitAsync(
            kitPk,
            include | KitInclude.FactionFilter | (KitInclude)(1 << 10) | KitInclude.Items | KitInclude.UnlockRequirements | KitInclude.MapFilter | KitInclude.Delays | KitInclude.Skillsets | KitInclude.Access,
            kit =>
            {
                if (kit.Season >= WarfareModule.Season)
                    throw new InvalidOperationException("Kit is already up to date.");

                oldClass = kit.Class;
                oldFaction = kit.Faction;

                kit.FactionFilterIsWhitelist = false;
                kit.MapFilterIsWhitelist = false;
                kit.FactionFilter.Clear();
                kit.MapFilter.Clear();
                kit.UnlockRequirements.Clear();
                kit.Delays.Clear();
                kit.Skillsets.Clear();
                kit.Faction = null;
                kit.FactionId = null;
                kit.CreditCost = 0;
                kit.PremiumCost = LoadoutCost;
                kit.Type = KitType.Loadout;
                kit.Disabled = true;

                kit.Items.Clear();
                kit.Weapons = string.Empty;
                SetDefaultItems(kit);

                kit.Season = WarfareModule.Season;
                kit.Class = @class;
                kit.Branch = KitDefaults.GetDefaultBranch(@class);
                kit.MinRequiredSquadMembers = KitDefaults.GetDefaultMinRequiredSquadMembers(@class);
                kit.RequiresSquad = KitDefaults.GetDefaultRequiresSquad(@class);
                kit.SquadLevel = KitDefaults.GetDefaultSquadLevel(@class);
                kit.RequestCooldown = KitDefaults.GetDefaultRequestCooldown(@class);

                // ensure access
                if (kit.Access.Exists(x => x.Steam64 == forPlayer.m_SteamID))
                    return;

                kit.Access.Add(new KitAccess
                {
                    AccessType = KitAccessType.Purchase,
                    Kit = kit,
                    KitId = kit.PrimaryKey,
                    Steam64 = forPlayer.m_SteamID,
                    Timestamp = DateTimeOffset.UtcNow
                });

                accessAdded = true;
            }
        , admin, token);

        if (kit == null)
            return null;

        try
        {
            SendInvokeLoadoutUpgradeStarted(forPlayer, oldClass, oldFaction?.InternalName, accessAdded, kit.Key);
        }
        catch (RpcNoConnectionsException) { }
        await InvokeLoadoutUpgradeStarted(kit, forPlayer, oldClass, oldFaction?.InternalName, accessAdded).ConfigureAwait(false);

        return kit;
    }

    private void SetDefaultItems(KitModel kit)
    {
        if (_loadoutItemsConfiguration == null || kit.Class == Class.Unarmed)
            return;

        IReadOnlyList<IItem> items = _loadoutItemsConfiguration.GetDefaultsForClass(kit.Class);
        
        if (kit.Items == null)
            kit.Items = new List<KitItemModel>(items.Count);
        else if (kit.Items.Capacity < items.Count)
            kit.Items.Capacity = items.Count;

        foreach (IItem item in items)
        {
            KitItemModel model = new KitItemModel { KitId = kit.PrimaryKey };
            KitItemUtility.CreateKitItemModel(item, model);

            kit.Items.Add(model);
        }
    }

    /// <summary>
    /// Gets the next free loadout ID for a player. Use with <see cref="LoadoutIdHelper"/> methods.
    /// </summary>
    public async Task<int> GetFreeLoadoutIdAsync(CSteamID forPlayer, CancellationToken token = default)
    {
        ulong s64 = forPlayer.m_SteamID;
        string likeExpr = s64.ToString(CultureInfo.InvariantCulture) + "\\_%"; // s64_%

        List<string> loadouts = await _kitSql.QueryListAsync(
            x => x.Where(x => EF.Functions.Like(x.Id, likeExpr))
                  .Select(x => x.Id),
            token: token
        ).ConfigureAwait(false);

        List<int> taken = new List<int>(loadouts.Count);
        foreach (string kit in loadouts)
        {
            int id = LoadoutIdHelper.Parse(kit);
            if (id > 0)
                taken.Add(id);
        }

        // find first open number
        int maxId = 0;
        int lowestGap = int.MaxValue;
        int last = -1;
        taken.Sort();
        for (int i = 0; i < taken.Count; ++i)
        {
            int c = taken[i];
            if (i != 0)
            {
                if (last + 1 != c && lowestGap > last + 1)
                    lowestGap = last + 1;
            }

            last = c;

            if (maxId < c)
                maxId = c;
        }

        return lowestGap == int.MaxValue ? maxId + 1 : lowestGap;
    }

    [RpcReceive]
    protected async Task<bool> UpgradeLoadoutRpc(CSteamID forPlayer, CSteamID admin, Class @class, uint kitPk, CancellationToken token = default)
    {
        return await UpgradeLoadoutAsync(forPlayer, admin, @class, kitPk, KitInclude.Base, token).ConfigureAwait(false) != null;
    }

    [RpcReceive]
    protected async Task<uint> CreateLoadoutRpc(CSteamID forPlayer, CSteamID creator, Class @class, string? displayName, CancellationToken token = default)
    {
        return (await CreateLoadoutAsync(forPlayer, creator, @class, displayName, null, token).ConfigureAwait(false)).Key;
    }

    [RpcReceive]
    protected async Task<bool> UnlockLoadoutRpc(CSteamID instigator, uint primaryKey, CancellationToken token = default)
    {
        return await UnlockLoadoutAsync(instigator, primaryKey, KitInclude.Base, token).ConfigureAwait(false) != null;
    }

    [RpcReceive]
    protected async Task<bool> LockLoadoutRpc(CSteamID instigator, uint primaryKey, CancellationToken token = default)
    {
        return await LockLoadoutAsync(instigator, primaryKey, KitInclude.Base, token).ConfigureAwait(false) != null;
    }

    [RpcReceive]
    protected async Task ReceiveInvokeLoadoutCreated(CSteamID forPlayer, int loadoutId, uint kitPrimaryKey)
    {
        Kit? kit = await _kitSql.QueryKitAsync(kitPrimaryKey, KitInclude.All).ConfigureAwait(false);
        if (kit == null)
        {
            _logger.LogWarning($"Invoked loadout created with unknown kit {kitPrimaryKey}.");
            return;
        }

        await InvokeLoadoutCreated(kit, forPlayer, loadoutId).ConfigureAwait(false);
    }

    [RpcReceive]
    protected async Task ReceiveInvokeLoadoutUpgradeStarted(CSteamID forPlayer, Class oldClass, string? oldFaction, bool accessAdded, uint kitPrimaryKey)
    {
        Kit? kit = await _kitSql.QueryKitAsync(kitPrimaryKey, KitInclude.All).ConfigureAwait(false);
        if (kit == null)
        {
            _logger.LogWarning($"Invoked loadout upgrade started with unknown kit {kitPrimaryKey}.");
            return;
        }

        await InvokeLoadoutUpgradeStarted(kit, forPlayer, oldClass, oldFaction, accessAdded).ConfigureAwait(false);
    }

    [RpcReceive]
    protected async Task ReceiveInvokeLockStateUpdated(uint kitPrimaryKey, CSteamID forPlayer, bool expectedLockState)
    {
        Kit? kit = await _kitSql.QueryKitAsync(kitPrimaryKey, KitInclude.All).ConfigureAwait(false);
        if (kit == null)
        {
            _logger.LogWarning($"Invoked loadout created with unknown kit {kitPrimaryKey}.");
            return;
        }

        if (kit.IsLocked != expectedLockState)
        {
            _logger.LogWarning($"Skipped lock state update invocation for kit {kitPrimaryKey}, expected {expectedLockState} but was {kit.IsLocked}.");
            return;
        }

        await InvokeLoadoutLockChanged(kit, forPlayer).ConfigureAwait(false);
    }

    private async Task InvokeLoadoutLockChanged(Kit kit, CSteamID forPlayer)
    {
        LoadoutLockChanged args = new LoadoutLockChanged
        {
            Kit = kit,
            Admin = kit.LastEditingPlayer,
            Player = forPlayer
        };
        if (WarfareModule.IsActive)
        {
            // todo: ActionLog.Add(ActionLogType.LockLoadout, kit.Id, kit.CreatingPlayer);
            await _eventDispatcher!.DispatchEventAsync(args, CancellationToken.None);
            // todo: swap kit with free kit, message player 'Your loadout has been disabled while X is working on it.', update signs
        }

        try
        {
            LoadoutLockChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown by LoadoutLockChanged handler.");
        }
    }

    private async Task InvokeLoadoutCreated(Kit kit, CSteamID forPlayer, int loadoutId)
    {
        LoadoutCreated args = new LoadoutCreated
        {
            Kit = kit,
            LoadoutId = loadoutId,
            Player = forPlayer
        };

        if (WarfareModule.IsActive)
        {
            // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, $"{forPlayer.m_SteamID.ToString(CultureInfo.InvariantCulture)} GIVEN ACCESS TO {kit.Id}, REASON: {KitAccessType.Purchase}", kit.CreatingPlayer);

            await _eventDispatcher!.DispatchEventAsync(args, CancellationToken.None);
            // todo: message player 'X has started working on your loadout'.
        }

        try
        {
            LoadoutCreated?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error thrown by LoadoutCreated handler.");
        }
    }

    private async Task InvokeLoadoutUpgradeStarted(Kit kit, CSteamID forPlayer, Class oldClass, string? oldFaction, bool accessAdded)
    {
        LoadoutUpgradeStarted args = new LoadoutUpgradeStarted
        {
            Kit = kit,
            Admin = kit.LastEditingPlayer,
            Player = forPlayer
        };

        if (WarfareModule.IsActive)
        {
            // todo: ActionLog.Add(ActionLogType.UpgradeLoadout, $"ID: {kit.Id} (#{kit.Key}). Class: {oldClass} -> {kit.Class}. Old Faction: {oldFaction ?? "none"}", kit.LastEditingPlayer);
            if (accessAdded)
                // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, $"{forPlayer.m_SteamID.ToString(CultureInfo.InvariantCulture)} GIVEN ACCESS TO {kit.Id}, REASON: {KitAccessType.Purchase}", kit.LastEditingPlayer);

            await _eventDispatcher!.DispatchEventAsync(args, CancellationToken.None);
            // todo: message player 'X has started working on your loadout'.
        }

        try
        {
            LoadoutUpgradeStarted?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error thrown by LoadoutUpgradeStarted handler.");
        }
    }

    [RpcSend(nameof(CreateLoadoutRpc))]
    protected virtual RpcTask<uint> SendCreateLoadout(IModularRpcRemoteConnection connection, CSteamID forPlayer, CSteamID creator, Class @class, string? displayName, CancellationToken token = default) => RpcTask<uint>.NotImplemented;

    [RpcSend(nameof(UpgradeLoadoutRpc))]
    protected virtual RpcTask<bool> SendUpgradeLoadout(IModularRpcRemoteConnection connection, CSteamID forPlayer, CSteamID admin, Class @class, uint kitPk, CancellationToken token = default) => RpcTask<bool>.NotImplemented;

    [RpcSend(nameof(UnlockLoadoutRpc))]
    protected virtual RpcTask<bool> SendUnlockLoadout(IModularRpcRemoteConnection connection, CSteamID instigator, uint primaryKey, CancellationToken token = default) => RpcTask<bool>.NotImplemented;

    [RpcSend(nameof(LockLoadoutRpc))]
    protected virtual RpcTask<bool> SendLockLoadout(IModularRpcRemoteConnection connection, CSteamID instigator, uint primaryKey, CancellationToken token = default) => RpcTask<bool>.NotImplemented;

    [RpcSend(nameof(ReceiveInvokeLoadoutCreated)), RpcFireAndForget]
    protected virtual void SendInvokeLoadoutCreated(CSteamID forPlayer, int loadoutId, uint kitPrimaryKey) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveInvokeLoadoutUpgradeStarted)), RpcFireAndForget]
    protected virtual void SendInvokeLoadoutUpgradeStarted(CSteamID forPlayer, Class oldClass, string? oldFaction, bool accessAdded, uint kitPrimaryKey) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveInvokeLockStateUpdated)), RpcFireAndForget]
    protected virtual void SendInvokeLoadoutLockStateUpdated(uint kitPrimaryKey, bool expectedLockState) => _ = RpcTask.NotImplemented;
    public enum OpenUpgradeTicketResult : byte
    {
        Success,
        TooManyTickets,
        AlreadyOpen
    }
}