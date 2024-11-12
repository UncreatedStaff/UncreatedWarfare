using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public delegate void KitAccessCallback(Kit kit, ulong player, bool newAccess, KitAccessType newType);
public delegate void KitChanged(WarfarePlayer player, Kit? kit, Kit? oldKit);

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
    // public static async Task WriteKitLocalization(LanguageInfo language, ITranslationService translationService, string path, bool writeMising, CancellationToken token = default)
    // {
    //     List<Kit> kits;
    //     await using (IKitsDbContext context = new WarfareDbContext())
    //     {
    //         kits = await context.Kits.Where(x => x.Type != KitType.Loadout).ToListAsync(token);
    //     }
    // 
    //     await using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
    //     await using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
    //     writer.WriteLine("# Kit Name Translations");
    //     writer.WriteLine("#  <br> = new line on signs");
    //     writer.WriteLine();
    //     for (int i = 0; i < kits.Count; i++)
    //     {
    //         Kit kit = kits[i];
    //         if (kit is not { Class: > Class.Unarmed })
    //             continue;
    //         if (WriteKitIntl(kit, language, translationService, writer, writeMising) && i != kits.Count - 1)
    //             writer.WriteLine();
    //     }
    // }

    // private static bool WriteKitIntl(Kit kit, LanguageInfo language, ITranslationService translationService, TextWriter writer, bool writeMising)
    // {
    //     bool isDefaultValue = false;
    //     string? value = null;
    //     LanguageInfo def = translationService.LanguageService.GetDefaultLanguage();
    //     if (kit.Translations != null)
    //     {
    //         KitTranslation? translation = kit.Translations.FirstOrDefault(x => x.LanguageId == language.Key);
    //         value = translation?.Value;
    //         isDefaultValue = translation == null;
    //         if (isDefaultValue && !language.IsDefault && writeMising)
    //         {
    //             value = kit.Translations.FirstOrDefault(x => x.LanguageId == def.Key)?.Value;
    //         }
    //     }
    // 
    //     if (value == null)
    //     {
    //         if (!writeMising)
    //             return false;
    //         isDefaultValue = true;
    //         value = kit.InternalName;
    //     }
    // 
    //     string? @default = kit.Translations?.FirstOrDefault(x => x.LanguageId == def.Key)?.Value;
    //     if (@default != null)
    //         @default = @default.Replace("\r", string.Empty).Replace("\n", "<br>");
    // 
    //     value = value.Replace("\r", string.Empty).Replace("\n", "<br>");
    //     writer.WriteLine("# " + kit.GetDisplayName(def) + " (ID: " + kit.InternalName + ")");
    //     if (kit.WeaponText != null)
    //         writer.WriteLine("#  Weapons: " + kit.WeaponText);
    // 
    //     writer.WriteLine("#  Class:   " + translationService.ValueFormatter.FormatEnum(kit.Class, def));
    //     writer.WriteLine("#  Type:    " + translationService.ValueFormatter.FormatEnum(kit.Type, def));
    // 
    //     FactionInfo? factionInfo = kit.FactionInfo;
    //     if (factionInfo != null)
    //         writer.WriteLine("#  Faction: " + factionInfo.GetName(def));
    // 
    //     if (!isDefaultValue && @default != null)
    //     {
    //         writer.WriteLine("# Default: \"" + @default + "\".");
    //     }
    // 
    //     writer.WriteLine(kit.InternalName + ": " + value);
    //     return true;
    // }

    public static void UpdateLastEdited(this Kit kit, CSteamID player)
    {
        if (player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return;
        
        kit.LastEditor = player.m_SteamID;
        kit.LastEditedTimestamp = DateTimeOffset.UtcNow;
    }

    public static bool ContainsItem(this Kit kit, Guid guid, Team team, AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore, bool checkClothes = false)
    {
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
                ItemAsset? asset = itm.GetItem(kit, team, out _, out _, assetRedirectService, factionDataStore);
                if (asset != null && asset.GUID == guid)
                    return true;
            }
        }

        return false;
    }

    public static bool ContainsItem(this Kit kit, IAssetLink<ItemAsset>? assetLink, Team team, AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore, bool checkClothes = false)
    {
        if (assetLink == null)
            return false;

        Guid guid = assetLink.Guid;
        return kit.ContainsItem(guid, team, assetRedirectService, factionDataStore, checkClothes);
    }

    public static int CountItems(this Kit kit, IAssetLink<ItemAsset>? assetLink, bool checkClothes = false)
    {
        if (assetLink == null)
            return 0;

        Guid guid = assetLink.Guid;
        return kit.CountItems(guid, checkClothes);
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
        return "<sprite index=" + faction.SpriteIndex.Value.ToString(CultureInfo.InvariantCulture) + "/>";
    }

    public static string GetFlagIcon(this FactionInfo? faction)
    {
        if (faction is not { TMProSpriteIndex: not null })
            return "<sprite index=0/>";
        return "<sprite index=" + faction.TMProSpriteIndex.Value.ToString(CultureInfo.InvariantCulture) + "/>";
    }

    public static char GetIcon(this Class @class)
    {
        // if (SquadManager.Config is { Classes: { Length: > 0 } arr })
        // {
        //     int i = (int)@class;
        //     if (arr.Length > i && arr[i].Class == @class)
        //         return arr[i].Icon;
        //     for (i = 0; i < arr.Length; ++i)
        //     {
        //         if (arr[i].Class == @class)
        //             return arr[i].Icon;
        //     }
        // }

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

    public static float GetTeamLimit(this Kit kit) => kit.TeamLimit ?? KitDefaults.GetDefaultTeamLimit(kit.Class);

    public static bool IsLimited(this Kit kit, IPlayerService playerService, out int currentPlayers, out int allowedPlayers, Team team, bool requireCounts = false)
    {
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;

        if (!requireCounts && kit.TeamLimit >= 1f)
        {
            return false;
        }

        IEnumerable<WarfarePlayer> friendlyPlayers = team == null
            ? playerService.OnlinePlayers
            : playerService.OnlinePlayersOnTeam(team);

        int ttl = 0;
        foreach (WarfarePlayer player in friendlyPlayers)
        {
            ++ttl;
            if (player.Component<KitPlayerComponent>().ActiveKitKey is { } pk && pk == kit.PrimaryKey)
                ++currentPlayers;
        }
        
        allowedPlayers = Mathf.CeilToInt(kit.GetTeamLimit() * ttl);
        if (kit.TeamLimit >= 1f)
        {
            return false;
        }

        return currentPlayers + 1 > allowedPlayers;
    }

    public static bool IsClassLimited(this Kit kit, IPlayerService playerService, out int currentPlayers, out int allowedPlayers, Team team, bool requireCounts = false)
    {
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && (kit.TeamLimit >= 1f))
            return false;

        IEnumerable<WarfarePlayer> friendlyPlayers = team == null
            ? playerService.OnlinePlayers
            : playerService.OnlinePlayersOnTeam(team);

        int ttl = 0;
        foreach (WarfarePlayer player in friendlyPlayers)
        {
            ++ttl;
            if (player.Component<KitPlayerComponent>().ActiveClass == kit.Class)
                ++currentPlayers;
        }

        allowedPlayers = Mathf.CeilToInt(kit.GetTeamLimit() * ttl);

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
        // if (UCWarfare.CanUseNetCall)
        // {
        //     RequestResponse response = await PlayerManager.NetCalls.RequestOpenTicket.RequestAck(UCWarfare.I.NetClient!, player, discordId, TicketType.ModifyLoadout, id.ToString(CultureInfo.InvariantCulture) + "_" + @class + "_" + displayName, 10000);
        //     return response.Responded && response.ErrorCode.HasValue && (StandardErrorCode)response.ErrorCode.Value == StandardErrorCode.Success;
        // }

        return false;
    }
    public static bool ValidSlot(byte slot) => slot == 0 || slot > PlayerInventory.SLOTS && slot <= 10;
    public static byte GetHotkeyIndex(byte slot)
    {
        if (!ValidSlot(slot)) return byte.MaxValue;
        // 0 should be counted as slot 10, nelson removes the first two from hotkeys because slots.
        return slot == 0 ? (byte)(9 - PlayerInventory.SLOTS) : (byte)(slot - PlayerInventory.SLOTS - 1);
    }

    public static bool CanBindHotkeyTo(ItemAsset asset, Page page) => (byte)page >= PlayerInventory.SLOTS && asset.canPlayerEquip && asset.slot.canEquipInPage((byte)page);
    public static class NetCalls
    {
#if false
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
        public static readonly NetCall<ulong[]> RequestIsNitroBoosting = new NetCall<ulong[]>(KnownNetMessage.RequestIsNitroBoosting, capacity: sizeof(ulong) * 48 + sizeof(ushort));
        public static readonly NetCall<ulong, int> RequestIsModifyLoadoutTicketOpen = new NetCall<ulong, int>(KnownNetMessage.RequestIsModifyLoadoutTicketOpen);

        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(KnownNetMessage.SendKitClass);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(KnownNetMessage.SendAckCreateLoadout);
        public static readonly NetCall<int[]> SendAckSetKitsAccess = new NetCall<int[]>(KnownNetMessage.SendAckSetKitsAccess);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(KnownNetMessage.SendKitsAccess);
        public static readonly NetCall<byte[]> RespondIsNitroBoosting = new NetCall<byte[]>(KnownNetMessage.RespondIsNitroBoosting, 50);
        public static readonly NetCall<ulong[], byte[]> SendNitroBoostingUpdated = new NetCall<ulong[], byte[]>(ReceiveIsNitroBoosting);


        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestSetKitAccess)]
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

            ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) +
                                                         (state ? (" GIVEN ACCESS TO " + kitId + ", REASON: " + type) :
                                                             (" DENIED ACCESS TO " + kitId + ".")), admin);

            KitSync.OnAccessChanged(player);

            UCPlayer? onlinePlayer = UCPlayer.FromID(player);

            if (onlinePlayer != null && onlinePlayer.IsOnline)
                manager.Signs.UpdateSigns(kit, onlinePlayer);

            return StandardErrorCode.Success;

        }
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestSetKitsAccess)]
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
                    ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) +
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
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestKitAccess)]
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

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestKitsAccess)]
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
        

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestKitClass)]
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
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestCreateLoadout)]
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
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestUpgradeLoadout)]
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
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestUnlockLoadout)]
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
        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendNitroBoostingUpdated)]
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
#endif
    }
}