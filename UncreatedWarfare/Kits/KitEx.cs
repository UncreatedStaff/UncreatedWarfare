using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;

public delegate void KitAccessCallback(SqlItem<Kit> kit, ulong player, bool newAccess, KitAccessType newType);
public delegate void KitChanged(UCPlayer player, SqlItem<Kit>? kit, SqlItem<Kit>? oldKit);

public static class KitEx
{
    public const int BranchMaxCharLimit = 16;
    public const int ClothingMaxCharLimit = 16;
    public const int ClassMaxCharLimit = 20;
    public const int TypeMaxCharLimit = 16;
    public const int RedirectTypeCharLimit = 20;
    public const int SquadLevelMaxCharLimit = 16;
    public const int KitNameMaxCharLimit = 25;
    public const int WeaponTextMaxCharLimit = 50;
    public const int SignTextMaxCharLimit = 50;
    public const int MaxStateArrayLimit = 18;
    public static void UpdateLastEdited(this Kit kit, ulong player)
    {
        if (Util.IsValidSteam64Id(player))
        {
            kit.LastEditor = player;
            kit.LastEditedTimestamp = DateTimeOffset.UtcNow;
        }
    }
    public static bool ContainsItem(this Kit kit, Guid guid, bool checkClothes = false)
    {
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            if (kit.Items[i] is IItem item)
            {
                if (item.Item == guid)
                    return true;
            }
            else if (checkClothes && kit.Items[i] is IBaseItem clothing)
            {
                if (clothing.Item == guid)
                    return true;
            }
        }
        return false;
    }
    public static int CountItems(this Kit kit, Guid guid, bool checkClothes = false)
    {
        int count = 0;
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            if (kit.Items[i] is IItem item)
            {
                if (item.Item == guid)
                    count++;
            }
            else if (checkClothes && kit.Items[i] is IBaseItem clothing)
            {
                if (clothing.Item == guid)
                    count++;
            }
        }
        return count;
    }

    public static byte GetKitItemTypeId(IKitItem item)
    {
        if (item is PageItem)
            return 1;
        if (item is ClothingItem)
            return 2;
        if (item is AssetRedirectItem)
            return 3;
        if (item is AssetRedirectClothing)
            return 4;

        return 0;
    }
    public static IKitItem? GetEmptyKitItem(byte id)
    {
        if (id is 0 or > 4)
            return null;
#pragma warning disable CS8509
        return (IKitItem)Activator.CreateInstance(id switch
#pragma warning restore CS8509
        {
            1 => typeof(PageItem),
            2 => typeof(ClothingItem),
            3 => typeof(AssetRedirectItem),
            4 => typeof(AssetRedirectClothing),
        });
    }
    public static string GetFlagIcon(this FactionInfo? faction)
    {
        if (faction is not { TMProSpriteIndex: { } })
            return "<sprite index=0/>";
        return "<sprite index=" + faction.TMProSpriteIndex.Value.ToString(Data.AdminLocale) + "/>";
    }
    public static char GetIcon(this Class @class)
    {
        if (SquadManager.Config is { Classes: { Length: > 0 } arr })
        {
            int i = (int)@class;
            if (arr.Length > i && arr[i].Class == @class)
                return arr[i].Icon;
            for (i = 0; i < arr.Length; ++i)
            {
                if (arr[i].Class == @class)
                    return arr[i].Icon;
            }
        }

        return @class switch
        {
            Class.Squadleader => '¦',
            Class.Rifleman => '¡',
            Class.Medic => '¢',
            Class.Breacher => '¤',
            Class.AutomaticRifleman => '¥',
            Class.Grenadier => '¬',
            Class.MachineGunner => '«',
            Class.LAT => '®',
            Class.HAT => '¯',
            Class.Marksman => '¨',
            Class.Sniper => '£',
            Class.APRifleman => '©',
            Class.CombatEngineer => 'ª',
            Class.Crewman => '§',
            Class.Pilot => '°',
            Class.SpecOps => '×',
            _ => '±'
        };
    }
    public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.Faction);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && kit.TeamLimit >= 1f)
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.ActiveKit == kit);
        if (kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static bool IsClassLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.Faction);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && (kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitClass == kit.Class);
        if (kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static bool TryParseClass(string val, out Class @class)
    {
        if (Enum.TryParse(val, true, out @class))
            return true;
        // checks old values for the enum before renaming.
        if (val.Equals("AUTOMATIC_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.AutomaticRifleman;
        else if (val.Equals("MACHINE_GUNNER", StringComparison.OrdinalIgnoreCase))
            @class = Class.MachineGunner;
        else if (val.Equals("AP_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.APRifleman;
        else if (val.Equals("COMBAT_ENGINEER", StringComparison.OrdinalIgnoreCase))
            @class = Class.CombatEngineer;
        else if (val.Equals("SPEC_OPS", StringComparison.OrdinalIgnoreCase))
            @class = Class.SpecOps;
        else
        {
            @class = default;
            return false;
        }

        return true;
    }
    public static class NetCalls
    {
        public const int PlayerHasAccessCode = -4;
        public const int PlayerHasNoAccessCode = -3;

        public static readonly NetCall<ulong, ulong, string, KitAccessType, bool> RequestSetKitAccess = new NetCall<ulong, ulong, string, KitAccessType, bool>(ReceiveSetKitAccess);
        public static readonly NetCall<ulong, ulong, string[], KitAccessType, bool> RequestSetKitsAccess = new NetCall<ulong, ulong, string[], KitAccessType, bool>(ReceiveSetKitsAccess);
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Util.ReadIReadWriteObjectNullable<Kit>, Util.WriteIReadWriteObjectNullable);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, byte, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, byte, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);
        public static readonly NetCall<ulong[]> RequestIsNitroBoosting = new NetCall<ulong[]>(1138, capacity: sizeof(ulong) * 48 + sizeof(ushort));

        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(1114);
        public static readonly NetCallRaw<Kit?> SendKit = new NetCallRaw<Kit?>(1117, Util.ReadIReadWriteObjectNullable<Kit>, Util.WriteIReadWriteObjectNullable);
        public static readonly NetCallRaw<Kit[]> SendKits = new NetCallRaw<Kit[]>(1118, Util.ReadIReadWriteArray<Kit>, Util.WriteIReadWriteArray);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<int[]> SendAckSetKitsAccess = new NetCall<int[]>(1133);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);
        public static readonly NetCall<byte[]> RespondIsNitroBoosting = new NetCall<byte[]>(1139, 50);
        public static readonly NetCall<ulong[], byte[]> SendNitroBoostingUpdated = new NetCall<ulong[], byte[]>(ReceiveIsNitroBoosting);


        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static async Task<StandardErrorCode> ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kit, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                return StandardErrorCode.GenericError;

            SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
            if (proxy?.Item != null)
            {
                await proxy.Enter().ConfigureAwait(false);
                try
                {
                    if (proxy.Item != null)
                    {
                        await (state ? KitManager.GiveAccess(proxy, player, type) : KitManager.RemoveAccess(proxy, player)).ConfigureAwait(false);
                        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                                                                           (state ? (" GIVEN ACCESS TO " + kit + ", REASON: " + type) :
                                                                           (" DENIED ACCESS TO " + kit + ".")), admin);
                        UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                        if (onlinePlayer != null && onlinePlayer.IsOnline)
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
                        return StandardErrorCode.Success;
                    }
                }
                finally
                {
                    proxy.Release();
                }
            }

            return StandardErrorCode.NotFound;
        }
        [NetCall(ENetCall.FROM_SERVER, 1132)]
        internal static async Task ReceiveSetKitsAccess(MessageContext context, ulong admin, ulong player, string[] kits, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            int[] successes = new int[kits.Length];
            if (manager == null)
            {
                for (int i = 0; i < successes.Length; ++i)
                    successes[i] = (int)StandardErrorCode.Success;
                context.Reply(SendAckSetKitsAccess, successes);
                return;
            }

            await manager.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < kits.Length; ++i)
                {
                    string kit = kits[i];
                    SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
                    if (proxy?.Item != null)
                    {
                        await (state ? KitManager.GiveAccess(proxy, player, type) : KitManager.RemoveAccess(proxy, player)).ConfigureAwait(false);
                        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                            (state ? (" GIVEN ACCESS TO " + kit + ", REASON: " + type) :
                                (" DENIED ACCESS TO " + kit + ".")), admin);
                        UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                        if (onlinePlayer != null && onlinePlayer.IsOnline)
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
                        continue;
                    }

                    successes[i] = (int)StandardErrorCode.NotFound;
                }
            }
            finally
            {
                manager.Release();
            }
            context.Reply(SendAckSetKitsAccess, successes);
        }
        /// <returns><see cref="PlayerHasAccessCode"/> if the player has access to the kit, <see cref="PlayerHasNoAccessCode"/> if they don't,<br/>
        /// <see cref="KitNotFoundErrorCode"/> if the kit isn't found, and <see cref="MessageContext.CODE_GENERIC_FAILURE"/> if <see cref="KitManager"/> isn't loaded.</returns>
        [NetCall(ENetCall.FROM_SERVER, 1134)]
        private static async Task<int> ReceiveKitAccessRequest(MessageContext context, string kit, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            if (manager == null)
                return (int)StandardErrorCode.GenericError;
            SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
            if (proxy?.Item == null)
                return (int)StandardErrorCode.NotFound;

            return await KitManager.HasAccess(proxy.LastPrimaryKey, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode;
        }

        [NetCall(ENetCall.FROM_SERVER, 1136)]
        private static async Task ReceiveKitsAccessRequest(MessageContext context, string[] kits, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            byte[] outp = new byte[kits.Length];
            if (manager == null)
            {
                for (int i = 0; i < outp.Length; ++i)
                    outp[i] = (int)StandardErrorCode.GenericError;
                context.Reply(SendKitsAccess, (byte)StandardErrorCode.GenericError, outp);
                return;
            }
            for (int i = 0; i < kits.Length; ++i)
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kits[i]).ConfigureAwait(false);
                if (proxy?.Item == null)
                    outp[i] = (int)StandardErrorCode.NotFound;
                else outp[i] = (byte)(await KitManager.HasAccess(proxy.LastPrimaryKey, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode);
            }
            context.Reply(SendKitsAccess, (byte)StandardErrorCode.Success, outp);
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static async Task<StandardErrorCode> ReceiveCreateKit(MessageContext context, Kit? kit)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null || kit == null)
                return StandardErrorCode.GenericError;
            await manager.AddOrUpdate(kit);
            return StandardErrorCode.Success;
        }

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static async Task ReceiveRequestKitClass(MessageContext context, string kitID)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                goto bad;
            SqlItem<Kit>? proxy = await manager.FindKit(kitID).ConfigureAwait(false);
            if (proxy?.Item == null)
                goto bad;
            await proxy.Enter().ConfigureAwait(false);
            try
            {
                if (proxy.Item == null)
                    goto bad;
                string signtext = proxy.Item.GetDisplayName();
                context.Reply(SendKitClass, kitID, proxy.Item.Class, signtext);
                return;
            }
            finally
            {
                proxy.Release();
            }
        bad:
            context.Reply(SendKitClass, kitID, Class.None, kitID);
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static async Task ReceiveKitRequest(MessageContext context, string kitID)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                goto bad;
            SqlItem<Kit>? proxy = await manager.FindKit(kitID).ConfigureAwait(false);
            if (proxy?.Item == null)
                goto bad;
            await proxy.Enter().ConfigureAwait(false);
            try
            {
                if (proxy.Item == null)
                    goto bad;
                context.Reply(SendKit, proxy.Item);
                return;
            }
            finally
            {
                proxy.Release();
            }
        bad:
            context.Reply(SendKit, null);
        }
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static async Task ReceiveKitsRequest(MessageContext context, string[] kitIDs)
        {
            List<Kit> kits = new List<Kit>(kitIDs.Length);
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                await manager.WaitAsync().ConfigureAwait(false);
                try
                {
                    for (int i = 0; i < kitIDs.Length; i++)
                    {
                        SqlItem<Kit>? proxy = manager.FindKitNoLock(kitIDs[i]);
                        if (proxy?.Item != null)
                        {
                            kits.Add(proxy.Item);
                        }
                    }
                }
                finally
                {
                    manager.Release();
                }
            }
            context.Reply(SendKits, kits.ToArray());
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, byte team, Class @class, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (SqlItem<Kit> kit, StandardErrorCode code) = await manager.CreateLoadout(fromPlayer, player, team, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit.Item is null ? string.Empty : kit.Item.Id, (int)code);
            }
            else
            {
                context.Reply(SendAckCreateLoadout, string.Empty, (int)StandardErrorCode.GenericError);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1140)]
        private static async Task ReceiveIsNitroBoosting(MessageContext context, ulong[] players, byte[] codes)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                await UCWarfare.ToUpdate();
                if (manager.IsLoaded)
                {
                    int len = Math.Min(players.Length, codes.Length);
                    for (int i = 0; i < len; ++i)
                    {
                        manager.OnNitroBoostingUpdated(players[i], codes[i]);
                    }
                    context.Acknowledge(StandardErrorCode.Success);
                    return;
                }
            }
            context.Acknowledge(StandardErrorCode.ModuleNotLoaded);
        }
    }

    public static bool ValidSlot(byte slot) => slot == 0 || slot > PlayerInventory.SLOTS && slot <= 10;
    public static byte GetHotkeyIndex(byte slot)
    {
        if (!ValidSlot(slot)) return byte.MaxValue;
        // 0 should be counted as slot 10, nelson removes the first two from hotkeys because slots.
        return slot == 0 ? (byte)(9 - PlayerInventory.SLOTS) : (byte)(slot - PlayerInventory.SLOTS - 1);
    }

    public static bool CanBindHotkeyTo(ItemAsset asset, Page page) => (byte)page >= PlayerInventory.SLOTS && asset.canPlayerEquip && asset.slot.canEquipInPage((byte)page);
}