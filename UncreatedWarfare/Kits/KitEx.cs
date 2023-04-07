using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
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
    public const int WeaponTextMaxCharLimit = 128;
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
    public static bool ContainsItem(this Kit kit, Guid guid, ulong team, bool checkClothes = false)
    {
        FactionInfo? faction = TeamManager.GetFactionSafe(team);
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            IKitItem itm = kit.Items[i];
            if (itm is IItem item)
            {
                if (item.Item == guid)
                    return true;
            }
            else if (checkClothes && itm is IBaseItem clothing)
            {
                if (clothing.Item == guid)
                    return true;
            }
            else if (itm is IAssetRedirect && (checkClothes || itm is not IClothingJar))
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
    public static int ParseStandardLoadoutId(string kitId)
    {
        if (kitId.Length > 18)
        {
            return GetLoadoutId(kitId, 18);
        }

        return -1;
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
    public static IKitItem[] GetDefaultLoadoutItems(Class @class)
    {
        List<IKitItem> items = new List<IKitItem>(32)
        {
            // do not reorder these
            new AssetRedirectClothing(RedirectType.Shirt, ClothingType.Shirt),
            new AssetRedirectClothing(RedirectType.Pants, ClothingType.Pants),
            new AssetRedirectClothing(RedirectType.Vest, ClothingType.Vest),
            new AssetRedirectClothing(RedirectType.Hat, ClothingType.Hat),
            new AssetRedirectClothing(RedirectType.Mask, ClothingType.Mask),
            new AssetRedirectClothing(RedirectType.Backpack, ClothingType.Backpack),
            new AssetRedirectClothing(RedirectType.Glasses, ClothingType.Glasses)
        };
        switch (@class)
        {
            case Class.Squadleader:
                items.Add(new AssetRedirectItem(RedirectType.LaserDesignator, 0, 0, 0, Page.Backpack));
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 6, 1, 0, Page.Backpack));
                items.Add(new AssetRedirectItem(RedirectType.Radio, 0, 2, 0, Page.Backpack));
                items.Add(new AssetRedirectItem(RedirectType.Radio, 3, 2, 0, Page.Backpack));
                items.Add(new AssetRedirectItem(RedirectType.RallyPoint, 0, 0, 1, Page.Shirt));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 5, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Red Smokes
                items.Add(new PageItem(new Guid("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Yellow Smoke
                items.Add(new PageItem(new Guid("18713c6d9b8f4980bdee830ca9d667ef"), 4, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Rifleman:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 2, 1, Page.Backpack));
                items.Add(new AssetRedirectItem(RedirectType.AmmoBag, 2, 0, 0, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 5, 1, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 0, 4, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Medic:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 3, 1, Page.Backpack));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Bloodbags
                items.Add(new PageItem(new Guid("5e1d521ecb7f4075aaebd344e838c2ca"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("5e1d521ecb7f4075aaebd344e838c2ca"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("5e1d521ecb7f4075aaebd344e838c2ca"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("5e1d521ecb7f4075aaebd344e838c2ca"), 3, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 5, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 6, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 4, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 5, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 4, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 6, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Breacher:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 3, 3, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // 12ga 00 Buckshot
                items.Add(new PageItem(new Guid("6089c30d75b247259673d9cdaa513cbb"), 0, 0, 0, Array.Empty<byte>(), 6, Page.Backpack));
                items.Add(new PageItem(new Guid("6089c30d75b247259673d9cdaa513cbb"), 0, 1, 0, Array.Empty<byte>(), 6, Page.Backpack));
                items.Add(new PageItem(new Guid("6089c30d75b247259673d9cdaa513cbb"), 1, 1, 0, Array.Empty<byte>(), 6, Page.Backpack));
                items.Add(new PageItem(new Guid("6089c30d75b247259673d9cdaa513cbb"), 1, 0, 0, Array.Empty<byte>(), 6, Page.Backpack));

                // 12ga Rifled Slugs
                items.Add(new PageItem(new Guid("d053c04af59b4985b463d160a92af331"), 2, 1, 0, Array.Empty<byte>(), 6, Page.Backpack));
                items.Add(new PageItem(new Guid("d053c04af59b4985b463d160a92af331"), 2, 0, 0, Array.Empty<byte>(), 6, Page.Backpack));

                // C-4 4-Pack Charge
                items.Add(new PageItem(new Guid("85bcbd5ee63d49c19c3c86b4e0d115d6"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Detonator
                items.Add(new PageItem(new Guid("618d0402c0724f1582fffd69f4cc0868"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 4, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 0, 4, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.AutomaticRifleman:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 3, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 3, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Grenadier:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 2, 1, Page.Backpack));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 4, 3, 1, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.MachineGunner:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 2, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 3, 1, 1, Array.Empty<byte>(), 1, Page.Hands));
                break;
            case Class.LAT:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 3, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                break;
            case Class.HAT:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 4, 3, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 5, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 0, 3, 1, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Marksman:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 0, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 4, 0, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 6, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.Sniper:
                // Backpack
                items.RemoveAt(5);
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 0, 0, Page.Vest));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Red Smoke
                items.Add(new PageItem(new Guid("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Violet Smoke
                items.Add(new PageItem(new Guid("1344161ee08e4297b64b4dc068c5935e"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Vest));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Vest));

                // Laser Rangefinder
                if (Assets.find(new Guid("010de9d7d1fd49d897dc41249a22d436")) is ItemAsset rgf)
                    items.Add(new PageItem(rgf.GUID, 1, 0, 0, rgf.getState(EItemOrigin.ADMIN), 1, Page.Backpack));
                break;
            case Class.APRifleman:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 0, 3, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Red Smoke
                items.Add(new PageItem(new Guid("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Yellow Smoke
                items.Add(new PageItem(new Guid("18713c6d9b8f4980bdee830ca9d667ef"), 4, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Detonator
                items.Add(new PageItem(new Guid("618d0402c0724f1582fffd69f4cc0868"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Remote-Detonated Claymore
                items.Add(new PageItem(new Guid("6d5980d658c9449c941928bcc738f210"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("6d5980d658c9449c941928bcc738f210"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("6d5980d658c9449c941928bcc738f210"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("6d5980d658c9449c941928bcc738f210"), 3, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("6d5980d658c9449c941928bcc738f210"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 5, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 6, 1, 0, Array.Empty<byte>(), 1, Page.Backpack));
                break;
            case Class.CombatEngineer:
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 2, 2, 1, Page.Backpack));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Anti-Tank Mine
                items.Add(new PageItem(new Guid("92df865d6d534bc1b20b7885fddb8af3"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("92df865d6d534bc1b20b7885fddb8af3"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("92df865d6d534bc1b20b7885fddb8af3"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("92df865d6d534bc1b20b7885fddb8af3"), 6, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Frag Grenades
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 0, 3, 0, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 1, 3, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 0, 4, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Military Knife
                items.Add(new PageItem(new Guid("47097f72d56c4bfb83bb8947e66396d5"), 2, 4, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Razorwire
                items.Add(new PageItem(new Guid("a2a8a01a58454816a6c9a047df0558ad"), 6, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));
                items.Add(new PageItem(new Guid("a2a8a01a58454816a6c9a047df0558ad"), 7, 2, 1, Array.Empty<byte>(), 1, Page.Backpack));

                // Sandbag Lines
                items.Add(new PageItem(new Guid("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Shirt));
                items.Add(new PageItem(new Guid("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Shirt));

                // Sandbag Pillboxes
                items.Add(new PageItem(new Guid("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Shirt));
                items.Add(new PageItem(new Guid("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 1, 0, Array.Empty<byte>(), 1, Page.Shirt));
                break;
            case Class.Crewman:
                items.RemoveAt(3); // hat
                items.RemoveRange(5 - 1, 2); // backpack, glasses

                // Crewman Helmet
                items.Add(new ClothingItem(new Guid("3ee3c7292ce340489b9afacda209e138"), ClothingType.Hat, Array.Empty<byte>()));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Portable Gas Can
                items.Add(new PageItem(new Guid("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Vest));

                // Carjack
                items.Add(new PageItem(new Guid("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Vest));
                break;
            case Class.Pilot:
                items.RemoveRange(2, 2); // vest, hat
                items.RemoveRange(5 - 2, 2); // backpack, glasses

                // Pilot Helmet
                items.Add(new ClothingItem(new Guid("78656047d47a4ff1ad7aa8a2e4d070a0"), ClothingType.Hat, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new PageItem(new Guid("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 3, 0, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Portable Gas Can
                items.Add(new PageItem(new Guid("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Shirt));

                // Carjack
                items.Add(new PageItem(new Guid("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Shirt));
                break;
            case Class.SpecOps:
                items.RemoveAt(6); // glasses
                items.Add(new AssetRedirectItem(RedirectType.EntrenchingTool, 4, 0, 1, Page.Backpack));

                // Military Nightvision
                items.Add(new ClothingItem(new Guid("cca8301927e049149fcee2b157a59da1"), ClothingType.Glasses, new byte[1]));

                // Dressings
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 0, 1, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("ae46254cfa3b437e9d74a5963e161da4"), 1, 1, 0, Array.Empty<byte>(), 1, Page.Hands));

                // Frag Grenade
                items.Add(new PageItem(new Guid("b01e414db03747509e87ebc515744216"), 2, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // White Smokes
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Array.Empty<byte>(), 1, Page.Hands));
                items.Add(new PageItem(new Guid("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 2, 0, Array.Empty<byte>(), 1, Page.Hands));

                // MRE
                items.Add(new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Detonator
                items.Add(new PageItem(new Guid("618d0402c0724f1582fffd69f4cc0868"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // C-4 4-Pack Charge
                items.Add(new PageItem(new Guid("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));

                // Binoculars
                items.Add(new PageItem(new Guid("f260c581cf504098956f424d62345982"), 5, 2, 0, Array.Empty<byte>(), 1, Page.Backpack));
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
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Util.ReadIReadWriteObjectNullable<Kit>, Util.WriteIReadWriteObjectNullable);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<ulong, ulong, Class, string> RequestUpgradeLoadout = new NetCall<ulong, ulong, Class, string>(ReceiveUpgradeLoadoutRequest);
        public static readonly NetCall<ulong, ulong, string> RequestUnlockLoadout = new NetCall<ulong, ulong, string>(ReceiveUnlockLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);
        public static readonly NetCall<ulong[]> RequestIsNitroBoosting = new NetCall<ulong[]>(1138, capacity: sizeof(ulong) * 48 + sizeof(ushort));
        public static readonly NetCall<ulong, int> RequestIsModifyLoadoutTicketOpen = new NetCall<ulong, int>(1038);

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
                    successes[i] = (int)StandardErrorCode.ModuleNotLoaded;
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
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, Class @class, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (SqlItem<Kit> kit, StandardErrorCode code) = await manager.CreateLoadout(fromPlayer, player, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit.Item is null ? string.Empty : kit.Item.Id, (int)code);
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