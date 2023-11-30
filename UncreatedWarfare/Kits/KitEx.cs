using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
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
    public static void WriteKitLocalization(LanguageInfo language, string path, bool writeMising)
    {
        KitManager? manager = KitManager.GetSingletonQuick();
        if (manager == null)
            return;

        manager.WriteWait();
        try
        {
            using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
            writer.WriteLine("# Kit Name Translations");
            writer.WriteLine("#  <br> = new line on signs");
            writer.WriteLine();
            for (int i = 0; i < manager.Items.Count; i++)
            {
                Kit kit = manager.Items[i];
                if (kit == null || kit.Type == KitType.Loadout || kit.Class <= Class.Unarmed)
                    continue;
                if (WriteKitIntl(kit, language, writer, writeMising) && i != manager.Items.Count - 1)
                    writer.WriteLine();
            }
        }
        finally
        {
            manager.WriteRelease();
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
        FactionInfo? factionInfo = TeamManager.GetFactionInfo(kit.Faction);
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
    public static int ParseStandardLoadoutId(string kitId)
    {
        if (kitId.Length > 18)
        {
            return GetLoadoutId(kitId, 18);
        }

        return -1;
    }
    public static int ParseStandardLoadoutId(string kitId, out ulong player)
    {
        int ld = ParseStandardLoadoutId(kitId);
        player = 0;
        if (kitId.Length <= 17 || !ulong.TryParse(kitId.Substring(0, 17), NumberStyles.Number, Data.AdminLocale, out player))
            return -1;

        return ld;
    }
    /// <summary>Indexed from 1.</summary>
    /// <returns>-1 if operation results in an overflow or invalid characters are found, otherwise, the id of the loadout.</returns>
    public static int GetLoadoutId(string chars, int start = 0, int len = -1)
    {
        if (len == -1)
            len = chars.Length - start;
        if (len > 9) // overflow
            return -1;
        int id = 0;
        for (int i = len - 1; i >= 0; --i)
        {
            int c = chars[start + i];
            if (c is > 96 and < 123)
                id += (c - 96) * (int)Math.Pow(26, len - i - 1);
            else if (c is > 64 and < 91)
                id += (c - 64) * (int)Math.Pow(26, len - i - 1);
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
    public static float GetTeamLimit(this Kit kit) => kit.TeamLimit ?? KitManager.GetDefaultTeamLimit(kit.Class);
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
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.Faction);
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
    public static IKitItem[] GetDefaultLoadoutItems(Class @class)
    {
        List<IKitItem> items = new List<IKitItem>(32)
        {
            // do not reorder these
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Shirt, ClothingType.Shirt, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Pants, ClothingType.Pants, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Vest, ClothingType.Vest, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Hat, ClothingType.Hat, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Mask, ClothingType.Mask, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Backpack, ClothingType.Backpack, null),
            new AssetRedirectClothingKitItem(PrimaryKey.NotAssigned, RedirectType.Glasses, ClothingType.Glasses, null)
        };
        switch (@class)
        {
            case Class.Squadleader:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 0, 0, Page.Backpack, RedirectType.LaserDesignator, null));
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 6, 1, 0, Page.Backpack, RedirectType.EntrenchingTool, null));
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 2, 0, Page.Backpack, RedirectType.Radio, null));
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 3, 2, 0, Page.Backpack, RedirectType.Radio, null));
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 0, 1, Page.Shirt, RedirectType.RallyPoint, null));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Red Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Yellow Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("18713c6d9b8f4980bdee830ca9d667ef"), 4, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Rifleman:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 2, 0, 0, Page.Backpack, RedirectType.AmmoBag, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 4, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Medic:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Bloodbags
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 0, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 1, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 2, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 3, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 5, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 4, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 5, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 6, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Breacher:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 3, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // 12ga 00 Buckshot
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 0, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 0, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 1, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 1, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));

                // 12ga Rifled Slugs
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("d053c04af59b4985b463d160a92af331"), 2, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("d053c04af59b4985b463d160a92af331"), 2, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));

                // C-4 4-Pack Charge
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 0, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 2, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 3, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 4, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 4, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.AutomaticRifleman:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 3, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 1, 1, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Grenadier:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 3, 3, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.MachineGunner:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 1, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 3, 1, 1, Page.Hands, 1, Array.Empty<byte>()));
                break;
            case Class.LAT:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                break;
            case Class.HAT:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 4, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 5, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 3, 3, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Marksman:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 0, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Sniper:
                // Backpack
                items.RemoveAt(5);
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 0, 0, Page.Vest, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Violet Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("1344161ee08e4297b64b4dc068c5935e"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 2, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Vest, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 2, Page.Vest, 1, Array.Empty<byte>()));

                // Laser Rangefinder
                if (Assets.find(new Guid("010de9d7d1fd49d897dc41249a22d436")) is ItemAsset rgf)
                    items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference(rgf.GUID), 1, 0, 0, Page.Hands, 1, rgf.getState(EItemOrigin.ADMIN)));
                break;
            case Class.APRifleman:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Yellow Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("18713c6d9b8f4980bdee830ca9d667ef"), 4, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Remote-Detonated Claymore
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 3, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 3, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 5, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 6, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.CombatEngineer:
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 2, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Anti-Tank Mine
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 0, 3, 3, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 3, 3, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 4, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 2, 4, 4, Page.Backpack, 1, Array.Empty<byte>()));

                // Razorwire
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("a2a8a01a58454816a6c9a047df0558ad"), 6, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("a2a8a01a58454816a6c9a047df0558ad"), 7, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Sandbag Lines
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 1, 1, Page.Shirt, 1, Array.Empty<byte>()));

                // Sandbag Pillboxes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 1, 1, Page.Shirt, 1, Array.Empty<byte>()));
                break;
            case Class.Crewman:
                items.RemoveAt(3); // hat
                items.RemoveRange(5 - 1, 2); // backpack, glasses

                // Crewman Helmet
                items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("3ee3c7292ce340489b9afacda209e138"), ClothingType.Hat, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 2, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // Portable Gas Can
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Page.Vest, 1, Array.Empty<byte>()));

                // Carjack
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Page.Vest, 1, Array.Empty<byte>()));
                break;
            case Class.Pilot:
                items.RemoveRange(2, 2); // vest, hat
                items.RemoveRange(5 - 2, 2); // backpack, glasses

                // Pilot Helmet
                items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("78656047d47a4ff1ad7aa8a2e4d070a0"), ClothingType.Hat, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 1, 1, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Portable Gas Can
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));

                // Carjack
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                break;
            case Class.SpecOps:
                items.RemoveAt(6); // glasses
                items.Add(new AssetRedirectPageKitItem(PrimaryKey.NotAssigned, 4, 0, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Military Nightvision
                items.Add(new SpecificClothingKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("cca8301927e049149fcee2b157a59da1"), ClothingType.Glasses, new byte[1]));

                // Dressings
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 1, 1, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenade
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 2, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 2, 2, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 0, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // C-4 4-Pack Charge
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(PrimaryKey.NotAssigned, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 5, 2, 2, Page.Backpack, 1, Array.Empty<byte>()));
                break;

        }
        return items.ToArray();
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

            Kit? kit = await manager.FindKit(kitId).ConfigureAwait(false);
            if (kit != null)
            {
                await manager.WaitAsync().ConfigureAwait(false);
                try
                {
                    await (state ? KitManager.GiveAccess(kit, player, type) : KitManager.RemoveAccess(kit, player)).ConfigureAwait(false);
                    ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                                                                       (state ? (" GIVEN ACCESS TO " + kitId + ", REASON: " + type) :
                                                                       (" DENIED ACCESS TO " + kitId + ".")), admin);
                    KitSync.OnAccessChanged(player);
                    UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                    if (onlinePlayer != null && onlinePlayer.IsOnline)
                        KitManager.UpdateSigns(kit, onlinePlayer);
                    return StandardErrorCode.Success;
                }
                finally
                {
                    manager.Release();
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
                    successes[i] = (int)StandardErrorCode.ModuleNotLoaded;
                context.Reply(SendAckSetKitsAccess, successes);
                return;
            }

            await manager.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < kits.Length; ++i)
                {
                    string kitId = kits[i];
                    Kit? kit = await manager.FindKit(kitId).ConfigureAwait(false);
                    if (kit != null)
                    {
                        await (state ? KitManager.GiveAccess(kit, player, type) : KitManager.RemoveAccess(kit, player)).ConfigureAwait(false);
                        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                            (state ? (" GIVEN ACCESS TO " + kitId + ", REASON: " + type) :
                                (" DENIED ACCESS TO " + kitId + ".")), admin);
                        KitSync.OnAccessChanged(player);
                        UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                        if (onlinePlayer != null && onlinePlayer.IsOnline)
                            KitManager.UpdateSigns(kit, onlinePlayer);
                        successes[i] = (int)StandardErrorCode.Success;
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
        private static async Task<int> ReceiveKitAccessRequest(MessageContext context, string kitId, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            if (manager == null)
                return (int)StandardErrorCode.GenericError;
            Kit? kit = await manager.FindKit(kitId).ConfigureAwait(false);
            if (kit == null)
                return (int)StandardErrorCode.NotFound;

            return await KitManager.HasAccess(kit, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode;
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
                Kit? kit = await manager.FindKit(kits[i]).ConfigureAwait(false);
                if (kit == null)
                    outp[i] = (int)StandardErrorCode.NotFound;
                else outp[i] = (byte)(await KitManager.HasAccess(kit, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode);
            }
            context.Reply(SendKitsAccess, (byte)StandardErrorCode.Success, outp);
        }
        

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static async Task ReceiveRequestKitClass(MessageContext context, string kitId)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                goto bad;
            Kit? kit = await manager.FindKit(kitId).ConfigureAwait(false);
            if (kit == null)
                goto bad;
            await manager.WaitAsync().ConfigureAwait(false);
            try
            {
                string signtext = kit.GetDisplayName();
                context.Reply(SendKitClass, kitId, kit.Class, signtext);
                return;
            }
            finally
            {
                manager.Release();
            }
            bad:
            context.Reply(SendKitClass, kitId, Class.None, kitId);
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, Class @class, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (Kit kit, StandardErrorCode code) = await manager.CreateLoadout(fromPlayer, player, @class, displayName);

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
                (_, StandardErrorCode code) = await manager.UpgradeLoadout(fromPlayer, player, @class, loadoutId).ConfigureAwait(false);

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
                (_, StandardErrorCode code) = await manager.UnlockLoadout(fromPlayer, displayName).ConfigureAwait(false);

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