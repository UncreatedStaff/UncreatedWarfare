using Microsoft.EntityFrameworkCore;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;

public delegate void KitAccessCallback(Kit kit, ulong player, bool newAccess, KitAccessType newType);
public delegate void KitChanged(UCPlayer player, Kit? kit, Kit? oldKit);

public static class KitEx
{
    public static readonly int BranchMaxCharLimit = 16;
    public static readonly int ClothingMaxCharLimit = 16;
    public static readonly int ClassMaxCharLimit = 20;
    public static readonly int TypeMaxCharLimit = 16;
    public static readonly int RedirectTypeCharLimit = 20;
    public static readonly int SquadLevelMaxCharLimit = 16;
    public static readonly int KitNameMaxCharLimit = 25;
    public static readonly int WeaponTextMaxCharLimit = 128;
    public static readonly int SignTextMaxCharLimit = 50;
    public static readonly int MaxStateArrayLimit = 18;
    public static async Task WriteKitLocalization(LanguageInfo language, string path, bool writeMising, CancellationToken token = default)
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
            return;

        List<Kit> kits;
        await using (IKitsDbContext context = new WarfareDbContext())
        {
            kits = await context.Kits.Where(x => x.Type != KitType.Loadout).ToListAsync(token);
        }

        await using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
        writer.WriteLine("# Kit Name Translations");
        writer.WriteLine("#  <br> = new line on signs");
        writer.WriteLine();
        for (int i = 0; i < kits.Count; i++)
        {
            Kit kit = kits[i];
            if (kit is not { Class: > Class.Unarmed })
                continue;
            if (WriteKitIntl(kit, language, writer, writeMising) && i != kits.Count - 1)
                writer.WriteLine();
        }
    }
    private static bool WriteKitIntl(Kit kit, LanguageInfo language, TextWriter writer, bool writeMising)
    {
        bool isDefaultValue = false;
        string? value = null;
        LanguageInfo def = Localization.GetDefaultLanguage();
        if (kit.Translations != null)
        {
            KitTranslation? translation = kit.Translations.FirstOrDefault(x => x.LanguageId == language.Key);
            value = translation?.Value;
            isDefaultValue = translation == null;
            if (isDefaultValue && !language.IsDefault && writeMising)
            {
                value = kit.Translations.FirstOrDefault(x => x.LanguageId == def.Key)?.Value;
            }
        }
        if (value == null)
        {
            if (!writeMising)
                return false;
            isDefaultValue = true;
            value = kit.InternalName;
        }

        string? @default = kit.Translations?.FirstOrDefault(x => x.LanguageId == def.Key)?.Value;
        if (@default != null)
            @default = @default.Replace("\r", string.Empty).Replace("\n", "<br>");
        value = value.Replace("\r", string.Empty).Replace("\n", "<br>");
        writer.WriteLine("# " + kit.GetDisplayName(Localization.GetDefaultLanguage()) + " (ID: " + kit.InternalName + ")");
        if (kit.WeaponText != null)
            writer.WriteLine("#  Weapons: " + kit.WeaponText);
        writer.WriteLine("#  Class:   " + Localization.TranslateEnum(kit.Class, Localization.GetDefaultLanguage()));
        writer.WriteLine("#  Type:    " + Localization.TranslateEnum(kit.Type, Localization.GetDefaultLanguage()));
        FactionInfo? factionInfo = kit.FactionInfo;
        if (factionInfo != null)
            writer.WriteLine("#  Faction: " + factionInfo.GetName(Localization.GetDefaultLanguage()));
        if (!isDefaultValue && @default != null)
        {
            writer.WriteLine("# Default: \"" + @default + "\".");
        }
        writer.WriteLine(kit.InternalName + ": " + value);
        return true;
    }
    public static void UpdateLastEdited(this Kit kit, ulong player)
    {
        if (Util.IsValidSteam64Id(player))
        {
            kit.LastEditor = player;
            kit.LastEditedTimestamp = DateTimeOffset.UtcNow;
        }
    }
    public static bool ContainsItem(this Kit kit, Guid guid, ulong team, bool checkClothes = false)
    {
        FactionInfo? faction = TeamManager.GetFactionSafe(team);
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            IKitItem itm = kit.Items[i];
            if (itm is ISpecificKitItem item)
            {
                if (item.Item.Equals(guid))
                    return true;
            }
            else if (checkClothes && itm is ISpecificKitItem clothing)
            {
                if (clothing.Item.Equals(guid))
                    return true;
            }
            else if (itm is IAssetRedirectKitItem && (checkClothes || itm is not IClothingKitItem))
            {
                ItemAsset? asset = itm.GetItem(kit, faction, out _, out _);
                if (asset != null && asset.GUID == guid)
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
            if (kit.Items[i] is ISpecificKitItem item)
            {
                if (item.Item.Equals(guid))
                    count++;
            }
            else if (checkClothes && kit.Items[i] is ISpecificKitItem clothing)
            {
                if (clothing.Item.Equals(guid))
                    count++;
            }
        }
        return count;
    }

    /// <summary>Indexed from 1.</summary>
    /// <returns>-1 if operation results in an overflow, the string is too short, or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int ParseStandardLoadoutId(ReadOnlySpan<char> kitId)
    {
        if (kitId.Length > 18)
        {
            return GetLoadoutId(kitId[18..]);
        }

        return -1;
    }

    /// <summary>Indexed from 1.</summary>
    /// <returns>-1 if operation results in an overflow, the string is too short, or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int ParseStandardLoadoutId(ReadOnlySpan<char> kitId, out ulong player)
    {
        int ld = ParseStandardLoadoutId(kitId);
        player = 0;
        if (kitId.Length <= 17 || !ulong.TryParse(kitId[..17], NumberStyles.Number, Data.AdminLocale, out player))
            return -1;

        return ld;
    }
    /// <summary>Indexed from 1.</summary>
    /// <returns>-1 if operation results in an overflow or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int GetLoadoutId(ReadOnlySpan<char> chars)
    {
        int id = 0;
        for (int i = chars.Length - 1; i >= 0; --i)
        {
            int c = chars[i];
            if (c is > 96 and < 123)
                id += (c - 96) * (int)Math.Pow(26, chars.Length - i - 1);
            else if (c is > 64 and < 91)
                id += (c - 64) * (int)Math.Pow(26, chars.Length - i - 1);
            else return -1;
        }

        if (id < 0) // overflow
            return -1;

        return id;
    }

    public static string GetLoadoutName(ulong player, int id) => player.ToString("D17", Data.AdminLocale) + "_" + GetLoadoutLetter(id);
    /// <summary>Indexed from 1.</summary>
    public static unsafe string GetLoadoutLetter(int id)
    {
        if (id <= 0) id = 1;
        int len = (int)Math.Ceiling(Math.Log(id == 1 ? 2 : id, 26));
        char* ptr = stackalloc char[len];
        ptr += len - 1;
        while (true)
        {
            *ptr = (char)(((id - 1) % 26) + 97);
            int rem = (id - 1) / 26;
            if (rem <= 0)
                break;

            --ptr;
            id = rem;
        }
        return new string(ptr, 0, len);
    }
    public static byte GetKitItemTypeId(IKitItem item)
    {
        if (item is SpecificPageKitItem)
            return 1;
        if (item is SpecificClothingKitItem)
            return 2;
        if (item is AssetRedirectPageKitItem)
            return 3;
        if (item is AssetRedirectClothingKitItem)
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
            1 => typeof(SpecificPageKitItem),
            2 => typeof(SpecificClothingKitItem),
            3 => typeof(AssetRedirectPageKitItem),
            4 => typeof(AssetRedirectClothingKitItem),
        });
    }
    public static string GetFlagIcon(this Faction? faction)
    {
        if (faction is not { SpriteIndex: not null })
            return "<sprite index=0/>";
        return "<sprite index=" + faction.SpriteIndex.Value.ToString(Data.AdminLocale) + "/>";
    }
    public static string GetFlagIcon(this FactionInfo? faction)
    {
        if (faction is not { TMProSpriteIndex: not null })
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
    public static float GetTeamLimit(this Kit kit) => kit.TeamLimit ?? KitDefaults<WarfareDbContext>.GetDefaultTeamLimit(kit.Class);
    public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.FactionInfo);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && kit.TeamLimit >= 1f)
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.GetTeamLimit() * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.ActiveKit.HasValue && k.ActiveKit.Value == kit.PrimaryKey);
        if (kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static bool IsClassLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.FactionInfo);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && (kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.GetTeamLimit() * friendlyPlayers.Count());
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
    /// <summary>
    /// Replaces newline constants like '/n', '\n', '&lt;br&gt;', etc with the actual newline character.
    /// </summary>
    [return: NotNullIfNotNull("str")]
    public static string? ReplaceNewLineSubstrings(string? str)
    {
        return str?.Replace("\\n", "\n").Replace("/n", "\n").Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
    }

    public static async Task<bool> OpenUpgradeTicket(string displayName, Class @class, int id, ulong player, ulong discordId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (UCWarfare.CanUseNetCall)
        {
            RequestResponse response = await PlayerManager.NetCalls.RequestOpenTicket.RequestAck(UCWarfare.I.NetClient!, player, discordId, TicketType.ModifyLoadout, id.ToString(Data.AdminLocale) + "_" + @class + "_" + displayName, 10000);
            return response.Responded && response.ErrorCode.HasValue && (StandardErrorCode)response.ErrorCode.Value == StandardErrorCode.Success;
        }

        return false;
    }
    public static class NetCalls
    {
        public const int PlayerHasAccessCode = -4;
        public const int PlayerHasNoAccessCode = -3;

        public static readonly NetCall<ulong, ulong, string, KitAccessType, bool> RequestSetKitAccess = new NetCall<ulong, ulong, string, KitAccessType, bool>(ReceiveSetKitAccess);
        public static readonly NetCall<ulong, ulong, string[], KitAccessType, bool> RequestSetKitsAccess = new NetCall<ulong, ulong, string[], KitAccessType, bool>(ReceiveSetKitsAccess);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<ulong, ulong, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<ulong, ulong, Class, string> RequestUpgradeLoadout = new NetCall<ulong, ulong, Class, string>(ReceiveUpgradeLoadoutRequest);
        public static readonly NetCall<ulong, ulong, string> RequestUnlockLoadout = new NetCall<ulong, ulong, string>(ReceiveUnlockLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);
        public static readonly NetCall<ulong[]> RequestIsNitroBoosting = new NetCall<ulong[]>(1138, capacity: sizeof(ulong) * 48 + sizeof(ushort));
        public static readonly NetCall<ulong, int> RequestIsModifyLoadoutTicketOpen = new NetCall<ulong, int>(1038);

        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(1114);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<int[]> SendAckSetKitsAccess = new NetCall<int[]>(1133);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);
        public static readonly NetCall<byte[]> RespondIsNitroBoosting = new NetCall<byte[]>(1139, 50);
        public static readonly NetCall<ulong[], byte[]> SendNitroBoostingUpdated = new NetCall<ulong[], byte[]>(ReceiveIsNitroBoosting);


        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static async Task<StandardErrorCode> ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kitId, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                return StandardErrorCode.GenericError;

            Kit? kit = await manager.FindKit(kitId, set: x => x.Kits.Include(y => y.Translations)).ConfigureAwait(false);
            if (kit == null)
                return StandardErrorCode.NotFound;

            bool alreadySet = !await (state ? manager.GiveAccess(kit, player, type) : manager.RemoveAccess(kit, player)).ConfigureAwait(false);
            if (alreadySet)
                return StandardErrorCode.InvalidData;

            ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                                                         (state ? (" GIVEN ACCESS TO " + kitId + ", REASON: " + type) :
                                                             (" DENIED ACCESS TO " + kitId + ".")), admin);

            KitSync.OnAccessChanged(player);

            UCPlayer? onlinePlayer = UCPlayer.FromID(player);

            if (onlinePlayer != null && onlinePlayer.IsOnline)
                manager.Signs.UpdateSigns(kit, onlinePlayer);

            return StandardErrorCode.Success;

        }
        [NetCall(ENetCall.FROM_SERVER, 1132)]
        internal static async Task ReceiveSetKitsAccess(MessageContext context, ulong admin, ulong player, string[] kits, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            int[] successes = new int[kits.Length];
            if (manager == null)
            {
                for (int i = 0; i < successes.Length; ++i)
                    successes[i] = (int)StandardErrorCode.ModuleNotLoaded;
                context.Reply(SendAckSetKitsAccess, successes);
                return;
            }


            for (int i = 0; i < kits.Length; ++i)
            {
                string kitId = kits[i];
                Kit? kit = await manager.FindKit(kitId, set: x => x.Kits.Include(y => y.Translations)).ConfigureAwait(false);
                if (kit != null)
                {
                    bool alreadySet = !await (state ? manager.GiveAccess(kit, player, type) : manager.RemoveAccess(kit, player)).ConfigureAwait(false);
                    if (alreadySet)
                    {
                        successes[i] = (int)StandardErrorCode.InvalidData;
                        continue;
                    }
                    ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                        (state ? (" GIVEN ACCESS TO " + kitId + ", REASON: " + type) :
                            (" DENIED ACCESS TO " + kitId + ".")), admin);
                    KitSync.OnAccessChanged(player);
                    UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                    if (onlinePlayer != null && onlinePlayer.IsOnline)
                        manager.Signs.UpdateSigns(kit, onlinePlayer);
                    successes[i] = (int)StandardErrorCode.Success;
                    continue;
                }

                successes[i] = (int)StandardErrorCode.NotFound;
            }

            context.Reply(SendAckSetKitsAccess, successes);
        }
        /// <returns><see cref="PlayerHasAccessCode"/> if the player has access to the kit, <see cref="PlayerHasNoAccessCode"/> if they don't,<br/>
        /// <see cref="KitNotFoundErrorCode"/> if the kit isn't found, and <see cref="MessageContext.CODE_GENERIC_FAILURE"/> if <see cref="KitManager"/> isn't loaded.</returns>
        [NetCall(ENetCall.FROM_SERVER, 1134)]
        private static async Task<int> ReceiveKitAccessRequest(MessageContext context, string kitId, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            if (manager == null)
                return (int)StandardErrorCode.GenericError;
            Kit? kit = await manager.FindKit(kitId, set: x => x.Kits).ConfigureAwait(false);
            if (kit == null)
                return (int)StandardErrorCode.NotFound;

            return await manager.HasAccess(kit, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode;
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
                Kit? kit = await manager.FindKit(kits[i], set: x => x.Kits).ConfigureAwait(false);
                if (kit == null)
                    outp[i] = (int)StandardErrorCode.NotFound;
                else outp[i] = (byte)(await manager.HasAccess(kit, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode);
            }
            context.Reply(SendKitsAccess, (byte)StandardErrorCode.Success, outp);
        }
        

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static async Task ReceiveRequestKitClass(MessageContext context, string kitId)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                Kit? kit = await manager.FindKit(kitId, set: x => x.Kits.Include(y => y.Translations)).ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                if (kit != null)
                {
                    string signtext = kit.GetDisplayName();
                    context.Reply(SendKitClass, kitId, kit.Class, signtext);
                    return;
                }
            }

            context.Reply(SendKitClass, kitId, Class.None, kitId);
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, Class @class, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (Kit kit, StandardErrorCode code) = await manager.Loadouts.CreateLoadout(fromPlayer, player, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit.InternalName, (int)code);
            }
            else
            {
                context.Reply(SendAckCreateLoadout, string.Empty, (int)StandardErrorCode.ModuleNotLoaded);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1141)]
        private static async Task ReceiveUpgradeLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, Class @class, string loadoutId)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (_, StandardErrorCode code) = await manager.Loadouts.UpgradeLoadout(fromPlayer, player, @class, loadoutId).ConfigureAwait(false);

                context.Acknowledge(code);
            }
            else
            {
                context.Acknowledge(StandardErrorCode.ModuleNotLoaded);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1142)]
        private static async Task ReceiveUnlockLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (_, StandardErrorCode code) = await manager.Loadouts.UnlockLoadout(fromPlayer, displayName).ConfigureAwait(false);

                context.Acknowledge(code);
            }
            else
            {
                context.Acknowledge(StandardErrorCode.ModuleNotLoaded);
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
                        manager.Boosting.OnNitroBoostingUpdated(players[i], codes[i]);
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