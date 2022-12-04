using SDG.NetTransport;
using SDG.Unturned;
using System;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare;
public static class Signs
{
    public const string PREFIX = "sign_";
    public const string VBS_PREFIX = "vbs_";
    public const string TRAIT_PREFIX = "trait_";
    public const string LOADOUT_PREFIX = "loadout_";
    public static void BroadcastSignUpdate(InteractableSign sign)
    {
        if (sign == null) return;
        if (Regions.tryGetCoordinate(sign.transform.position, out byte x, out byte y))
            BroadcastSign(sign.text, sign, x, y);
    }
    public static void BroadcastSignUpdate(BarricadeDrop drop)
    {
        if (drop != null && drop.interactable is InteractableSign sign && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
            BroadcastSign(sign.text, sign, x, y);
    }
    public static void SendSignUpdate(InteractableSign sign, UCPlayer player)
    {
        if (sign != null && player.IsOnline)
        {
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, player.Connection, GetClientText(sign.text, player, sign));
        }
    }
    public static void SendSignUpdate(BarricadeDrop drop, UCPlayer player)
    {
        if (drop != null && drop.interactable is InteractableSign sign && player.IsOnline)
        {
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, player.Connection, GetClientText(sign.text, player, sign));
        }
    }
    public static void BroadcastSign(string serverText, InteractableSign sign, byte x, byte y)
    {
        if (serverText.Length <= PREFIX.Length || !serverText.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            BroadcastClientSign(serverText, sign, x, y);
            return;
        }
        string key2 = serverText.Substring(PREFIX.Length);
        if (key2.StartsWith(VBS_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            BroadcastClientVBS(key2.Substring(VBS_PREFIX.Length), sign, x, y);
        }
        else if (key2.StartsWith(TRAIT_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            BroadcastClientTrait(key2.Substring(TRAIT_PREFIX.Length), sign, x, y);
        }
        else
        {
            BroadcastClientKitSign(key2, sign, x, y);
        }
    }
    public static string GetClientText(string serverText, UCPlayer player, InteractableSign sign)
    {
        if (serverText.Length <= PREFIX.Length || !serverText.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
            return GetClientTextSign(player, serverText);

        string key2 = serverText.Substring(PREFIX.Length);
        if (key2.StartsWith(VBS_PREFIX, StringComparison.OrdinalIgnoreCase))
            return GetClientTextVBS(player, key2.Substring(VBS_PREFIX.Length), sign);
        
        if (key2.StartsWith(TRAIT_PREFIX, StringComparison.OrdinalIgnoreCase))
            return GetClientTextTrait(player, key2.Substring(TRAIT_PREFIX.Length));

        return GetClientTextKitSign(player, key2);
    }

    private static void BroadcastClientVBS(string vbKey, InteractableSign sign, byte x, byte y)
    {
        VehicleBay? vb = VehicleBay.GetSingletonQuick();
        if (VehicleSigns.Loaded && VehicleSpawner.Loaded && vb != null &&
            VehicleSpawner.TryGetSpawnFromSign(sign, out Vehicles.VehicleSpawn spawn)
            && spawn.Data?.Item != null)
        {
            NetId id = sign.GetNetId();
            VehicleData data = spawn.Data.Item;
            foreach (LanguageSet set in LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS))
            {
                string fmt = Localization.TranslateVBS(spawn, data, set.Language, data.Team);
                while (set.MoveNext())
                    Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Connection, QuickFormat(fmt, data.GetCostLine(set.Next)));
            }
        }
        else
            BroadcastClientSign(vbKey, sign, x, y);
    }
    private static string GetClientTextVBS(UCPlayer player, string vbKey, InteractableSign sign)
    {
        VehicleBay? vb = VehicleBay.GetSingletonQuick();
        if (VehicleSigns.Loaded && VehicleSpawner.Loaded && vb != null &&
            VehicleSpawner.TryGetSpawnFromSign(sign, out Vehicles.VehicleSpawn spawn)
            && spawn.Data?.Item != null)
        {
            string lang = Localization.GetLang(player.Steam64);
            return QuickFormat(Localization.TranslateVBS(spawn, spawn.Data.Item, lang, spawn.Data.Item.Team), spawn.Data.Item.GetCostLine(player));
        }
        
        return GetClientTextSign(player, vbKey);
    }

    private static string GetClientTextSign(UCPlayer player, string key)
    {
        Translation? t = Translation.FromSignId(key);
        return t == null ? key : t.Translate(player);
    }
    private static void BroadcastClientTrait(string typeName, InteractableSign sign, byte x, byte y)
    {
        TraitData? d;
        if (!TraitManager.Loaded || (d = TraitManager.GetData(typeName)) == null)
        {
            BroadcastClientSign(typeName, sign, x, y);
            return;
        }
        NetId id = sign.GetNetId();
        bool t = d.Team is 1 or 2;
        foreach (LanguageSet set in t
                     ? LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS)
                     : LanguageSet.InRegionsByTeam(x, y, BarricadeManager.BARRICADE_REGIONS))
        {
            ulong team = t ? d.Team : set.Team;
            string fmt = TraitSigns.TranslateTraitSign(d, set.Language, team, out bool shouldFormat);
            while (set.MoveNext())
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Connection, shouldFormat ? TraitSigns.FormatTraitSign(d, fmt, set.Next, team) : fmt);
        }
    }
    private static string GetClientTextTrait(UCPlayer player, string typeName)
    {
        TraitData? d;
        if (!TraitManager.Loaded || (d = TraitManager.GetData(typeName)) == null)
            return GetClientTextSign(player, typeName);

        ulong team = d.Team is 1 or 2 ? d.Team : player.GetTeam();
        string txt = TraitSigns.TranslateTraitSign(d, Localization.GetLang(player.Steam64), team, out bool fmt);
        return fmt ? TraitSigns.FormatTraitSign(d, txt, player, team) : txt;
    }
    private static void BroadcastClientKitSign(string kitname, InteractableSign sign, byte x, byte y)
    {
        bool ld = kitname.StartsWith(LOADOUT_PREFIX, StringComparison.OrdinalIgnoreCase);
        KitOld kit = null!;
        if (!KitManager.Loaded || (!ld && !KitManager.KitExists(kitname, out kit)))
        {
            BroadcastClientSign(kitname, sign, x, y);
            return;
        }
        NetId id = sign.GetNetId();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                continue;
            string lang = Localization.GetLang(pl.Steam64);
            Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl.Connection, ld ? Localization.TranslateLoadoutSign(kitname, lang, pl) : Localization.TranslateKitSign(lang, kit, pl));
        }
    }
    private static string GetClientTextKitSign(UCPlayer player, string kitname)
    {
        bool ld = kitname.StartsWith(LOADOUT_PREFIX, StringComparison.OrdinalIgnoreCase);
        KitOld kit = null!;
        if (!KitManager.Loaded || (!ld && !KitManager.KitExists(kitname, out kit)))
        {
            return GetClientTextSign(player, kitname);
        }

        string lang = Localization.GetLang(player.Steam64);
        return ld ? Localization.TranslateLoadoutSign(kitname, lang, player) : Localization.TranslateKitSign(lang, kit, player);
    }
    private static void BroadcastClientSign(string signKey, InteractableSign sign, byte x, byte y)
    {
        NetId id = sign.GetNetId();
        Translation? t = Translation.FromSignId(signKey);
        if (t == null)
        {
            foreach (ITransportConnection pl in BarricadeManager.EnumerateClients(x, y, ushort.MaxValue))
            {
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl, signKey);
            }
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS))
            {
                string fmt = t.Translate(set.Language);
                while (set.MoveNext())
                    Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Connection, fmt);
            }
        }
    }
    public static string QuickFormat(string input, string val)
    {
        int ind = input.IndexOf("{0}", StringComparison.Ordinal);
        if (ind != -1)
        {
            if (string.IsNullOrEmpty(val))
                return input.Substring(0, ind) + input.Substring(ind + 3, input.Length - ind - 3);
            return input.Substring(0, ind) + val + input.Substring(ind + 3, input.Length - ind - 3);
        }
        return input;
    }
}
