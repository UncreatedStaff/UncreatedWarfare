using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare;
public static class Signs
{
    public const string Prefix = "sign_";
    public const string VBSPrefix = "vbs_";
    public const string KitPrefix = "kit_";
    public const string TraitPrefix = "trait_";
    public const string LoadoutPrefix = "loadout_";
    public const string LongTranslationPrefix = "l_";
    private static readonly Dictionary<uint, CustomSignComponent> ActiveSigns = new Dictionary<uint, CustomSignComponent>(64);
    private static CustomSignComponent? GetComponent(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is not InteractableSign sign)
            return null;
        string text = sign.text;
        if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return null;
        text = text.Substring(Prefix.Length);
        if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
        {
            if (comp.isActiveAndEnabled)
                UnityEngine.Object.Destroy(comp);
            ActiveSigns.Remove(drop.instanceID);
        }
        if (drop.model.TryGetComponent(out comp))
            UnityEngine.Object.Destroy(comp);

        if (text.StartsWith(VBSPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<VehicleBaySignComponent>();
        }
        else if (text.StartsWith(KitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<KitSignComponent>();
        }
        else if (text.StartsWith(LoadoutPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<KitSignComponent>();
            ((KitSignComponent)comp).IsLoadout = true;
        }
        else if (text.StartsWith(TraitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<TraitSignComponent>();
        }
        else if (text.StartsWith(LongTranslationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            comp = drop.model.gameObject.AddComponent<TranlationSignComponent>();
            ((TranlationSignComponent)comp).IsLong = true;
        }
        else
        {
            comp = drop.model.gameObject.AddComponent<TranlationSignComponent>();
        }

        comp.Init(drop);
        ActiveSigns.Add(drop.instanceID, comp);
        return comp;
    }
    public static SqlItem<Kit>? GetKitFromSign(BarricadeDrop drop, out int loadoutId)
    {
        if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent kitsign)
        {
            loadoutId = kitsign.LoadoutIndex;
            return kitsign.IsLoadout ? null : kitsign.Kit;
        }

        loadoutId = -1;
        return null;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static bool CheckSign(BarricadeDrop drop)
    {
        if (drop.model == null)
            return false;
        CustomSignComponent? comp = GetComponent(drop);
        if (comp is not null)
        {
            BroadcastSignUpdate(drop, comp);
            return true;
        }
        return false;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void CheckAllSigns()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        foreach (uint id in ActiveSigns.Keys.ToList())
        {
            BarricadeDrop? drop = UCBarricadeManager.FindBarricade(id);
            if (drop == null && ActiveSigns.TryGetValue(id, out CustomSignComponent comp))
            {
                if (comp != null)
                    UnityEngine.Object.Destroy(comp);
                ActiveSigns.Remove(id);
            }
        }
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                        CheckSign(region.drops[i]);
                }
            }
        }
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                    CheckSign(region.drops[i]);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void UpdateAllSigns(bool updatePlainText = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                        BroadcastSignUpdate(region.drops[i], updatePlainText);
                }
            }
        }
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                if (region.drops[i].asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
                    BroadcastSignUpdate(region.drops[i], updatePlainText);
            }
        }
    }
    public static void UpdateLoadoutSigns(UCPlayer? player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        bool b = player is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline) return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                            SendSignUpdate(drop, player);
                    }
                }
            }
        }

        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && comp2.IsLoadout)
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void UpdateKitSigns(UCPlayer? player, string? kitName)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        bool a = kitName is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline)
            return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }

        bool b = player is null;
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is KitSignComponent comp2 && (a || kitName!.Equals(comp2.KitName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void BroadcastSignUpdate(BarricadeDrop drop, bool updatePlainText = false)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                BroadcastSignUpdate(drop, comp);
                return;
            }

            if (!updatePlainText) return;
            NetId id = sign.GetNetId();
            bool r = Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y);
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (r && !Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    continue;
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl.Connection, sign.text);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    private static void BroadcastSignUpdate(BarricadeDrop drop, CustomSignComponent comp)
    {
        NetId id = drop.interactable.GetNetId();
        bool r = Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y);
        if (comp.DropIsPlanted)
            r = false;
        if (comp.PerPlayer || !r)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (r && !Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    continue;
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, pl.Connection, comp.Translate(pl.Language, pl));
            }
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.InRegions(x, y, BarricadeManager.BARRICADE_REGIONS))
            {
                string val = comp.Translate(set.Language, null!);
                while (set.MoveNext())
                    Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Connection, val);
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static void SendSignUpdate(BarricadeDrop drop, UCPlayer player, bool updatePlainText = false)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                SendSignUpdate(drop, player, comp);
                return;
            }
            if (!updatePlainText ||
                Regions.tryGetCoordinate(drop.model.position, out byte x2, out byte y2) &&
                !Regions.checkArea(x2, y2, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                return;
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, player.Connection, sign.text);
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    private static void SendSignUpdate(BarricadeDrop drop, UCPlayer player, CustomSignComponent comp)
    {
        if (!comp.DropIsPlanted && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y) && !Regions.checkArea(x, y, player.Player.movement.region_x, player.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
            return;
        Data.SendChangeText.Invoke(((InteractableSign)drop.interactable).GetNetId(), ENetReliability.Unreliable, player.Connection, comp.Translate(player.Language, player));
    }
    public static void UpdateTraitSigns(UCPlayer? player, TraitData? data)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        string n = data?.TypeName!;
        bool a = n is null;
        if (player is null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                            BroadcastSignUpdate(drop, comp2);
                    }
                }
            }
        }
        else if (!player.IsOnline)
            return;
        else
        {
            int maxx = Math.Min(player.Player.movement.region_x + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            int maxy = Math.Min(player.Player.movement.region_y + BarricadeManager.BARRICADE_REGIONS, Regions.WORLD_SIZE);
            for (int x = Math.Max(0, player.Player.movement.region_x - BarricadeManager.BARRICADE_REGIONS); x <= maxx; ++x)
            {
                for (int y = Math.Max(0, player.Player.movement.region_y - BarricadeManager.BARRICADE_REGIONS); y <= maxy; ++y)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        BarricadeDrop drop = region.drops[i];
                        if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                            SendSignUpdate(drop, player, comp2);
                    }
                }
            }
        }

        bool b = player is null;
        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
            for (int i = 0; i < region.drops.Count; ++i)
            {
                BarricadeDrop drop = region.drops[i];
                if (drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL && ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) && comp is TraitSignComponent comp2 && (a || n!.Equals(comp2.TraitName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (b)
                        BroadcastSignUpdate(drop, comp2);
                    else
                        SendSignUpdate(drop, player!, comp2);
                }
            }
        }
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static string GetClientText(BarricadeDrop drop, UCPlayer player, out bool isLong)
    {
        if (drop.interactable is InteractableSign sign)
        {
            if (ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp))
            {
                isLong = comp is TranlationSignComponent t && t.IsLong;
                return comp.Translate(player.Language, player);
            }

            isLong = false;
            return sign.text;
        }

        isLong = false;
        return string.Empty;
    }
    /// <summary>Can lock <see cref="KitManager"/> write semaphore.</summary>
    public static string GetClientText(BarricadeDrop drop, UCPlayer player)
    {
        if (drop.interactable is InteractableSign sign)
        {
            return ActiveSigns.TryGetValue(drop.instanceID, out CustomSignComponent comp) ? comp.Translate(player.Language, player) : sign.text;
        }

        return string.Empty;
    }
    public static string QuickFormat(string input, string? val)
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
    public static void SetSignTextServerOnly(InteractableSign sign, string text)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeDrop barricadeByRootFast = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
        byte[] state = barricadeByRootFast.GetServersideData().barricade.state;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        if (bytes.Length > byte.MaxValue - 18)
        {
            L.LogWarning(text + " is too long to go on a sign! (SetSignTextServerOnly)");
            return;
        }
        byte[] newState = new byte[17 + bytes.Length];
        Buffer.BlockCopy(state, 0, newState, 0, 16);
        newState[16] = (byte)bytes.Length;
        if (bytes.Length != 0)
            Buffer.BlockCopy(bytes, 0, newState, 17, bytes.Length);
        BarricadeManager.updateState(barricadeByRootFast.model, newState, newState.Length);
        sign.updateState(barricadeByRootFast.asset, newState);
    }
    private abstract class CustomSignComponent : MonoBehaviour
    {
        protected InteractableSign Sign;
        public bool DropIsPlanted { get; private set; }
        public abstract bool PerPlayer { get; }
        public void Init(BarricadeDrop drop)
        {
            if (BarricadeManager.tryGetRegion(drop.model, out _, out _, out ushort plant, out _) && plant != ushort.MaxValue)
                DropIsPlanted = true;
            Sign = (InteractableSign)drop.interactable;
            Init();
        }
        protected abstract void Init();
        public abstract string Translate(string language, UCPlayer player);
    }
    private sealed class TranlationSignComponent : CustomSignComponent
    {
        private Translation? _translation;
        private string _signId;
        private string? _defCache;
        private bool _warn;
        public override bool PerPlayer => false;
        public bool CanCache { get; set; } = true;
        public bool IsLong { get; set; }
        public string SignId
        {
            get => _signId;
            set
            {
                _signId = value;
                _defCache = null;
                Init();
            }
        }
        [UsedImplicitly]
        void OnDisable()
        {
            Translation.OnReload -= OnReload;
        }
        protected override void Init()
        {
            if (Sign.text.Length > Prefix.Length + TraitPrefix.Length)
            {
                _signId = Sign.text.Substring(Prefix.Length);
            }
            else
            {
                _signId = Sign.text;
                if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid trait id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
            Translation.OnReload += OnReload;
            if (!string.IsNullOrEmpty(SignId))
                _translation = Translation.FromSignId(SignId);
        }
        private void OnReload() => _defCache = null;
        public override string Translate(string language, UCPlayer player)
        {
            if (CanCache && language.Equals(L.Default, StringComparison.Ordinal))
            {
                return _defCache ??= _translation?.Translate(L.Default) ?? SignId ?? Sign.text.Substring(Prefix.Length);
            }
            return _translation?.Translate(language) ?? SignId ?? Sign.text.Substring(Prefix.Length);
        }
    }
    private sealed class VehicleBaySignComponent : CustomSignComponent
    {
        private VehicleSpawn? _spawn;
        private string? _defCache;
        public override bool PerPlayer => true;
        public VehicleSpawn? Spawn
        {
            get
            {
                if (_spawn != null || !VehicleSpawnerOld.Loaded)
                    return _spawn;
                VehicleSpawnerOld.TryGetSpawnFromSign(Sign, out _spawn);
                return _spawn;
            }
        }
        [UsedImplicitly]
        void OnDisable()
        {
            Translation.OnReload -= OnReload;
        }
        private void OnReload() => _defCache = null;
        protected override void Init() { }
        public override string Translate(string language, UCPlayer player)
        {
            VehicleSpawn? spawn = Spawn;
            VehicleData? data = spawn?.Data?.Item;
            if (spawn != null && data != null)
            {
                if (language.Equals(L.Default, StringComparison.Ordinal))
                {
                    _defCache ??= Localization.TranslateVBS(spawn, data, language, data.Team);
                    return QuickFormat(_defCache, data.GetCostLine(player));
                }

                return QuickFormat(Localization.TranslateVBS(spawn, data, language, data.Team), data.GetCostLine(player));
            }
            return Sign.text;
        }
    }
    private sealed class TraitSignComponent : CustomSignComponent
    {
        private bool _warn;
        private TraitData? _trait;
        private string _traitName;
        private string? _defCache;
        private bool cacheFmt;
        public override bool PerPlayer => true;
        public string TraitName
        {
            get => _traitName;
            set
            {
                _traitName = value;
                _trait = null;
            }
        }
        public TraitData? Trait
        {
            get
            {
                if (_trait != null || !TraitManager.Loaded)
                    return _trait;
                if (TraitName is null)
                    return null;
                return _trait = TraitManager.FindTrait(TraitName);
            }
        }
        [UsedImplicitly]
        void OnDisable()
        {
            Translation.OnReload -= OnReload;
        }
        private void OnReload() => _defCache = null;
        protected override void Init()
        {
            Translation.OnReload += OnReload;
            if (Sign.text.Length > Prefix.Length + TraitPrefix.Length)
            {
                TraitName = Sign.text.Substring(Prefix.Length + TraitPrefix.Length);
                _ = Trait;
            }
            else
            {
                TraitName = null!;
                if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid trait id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
        }
        public override string Translate(string language, UCPlayer player)
        {
            TraitData? trait = Trait;
            if (trait != null)
            {
                ulong team = trait.Team is 1 or 2 ? trait.Team : player.GetTeam();
                if (language.Equals(L.Default, StringComparison.Ordinal))
                {
                    _defCache ??= TraitSigns.TranslateTraitSign(trait, language, team, out cacheFmt);
                    return cacheFmt ? TraitSigns.FormatTraitSign(trait, _defCache, player, team) : _defCache;
                }

                string txt = TraitSigns.TranslateTraitSign(trait, language, team, out bool fmt);
                return fmt ? TraitSigns.FormatTraitSign(trait, txt, player, team) : txt;
            }
            return Sign.text;
        }
    }
    private sealed class KitSignComponent : CustomSignComponent
    {
        private bool _warn;
        private string _kitName;
        public int LoadoutIndex { get; set; } = -1;
        public bool IsLoadout { get; set; }
        public override bool PerPlayer => true;
        public string KitName
        {
            get => _kitName;
            set
            {
                _kitName = value;
                if (_kit is not null && (value is null || !_kitName.Equals(_kit?.Item?.Id)))
                    _kit = null;
            }
        }

        private SqlItem<Kit>? _kit;
        public SqlItem<Kit>? Kit
        {
            get
            {
                if (KitName is null)
                    return null;
                if (_kit?.Item != null && _kit.Manager is KitManager m && m.IsLoaded)
                    return _kit;
                m = KitManager.GetSingletonQuick()!;
                if (m == null)
                    return _kit = null;
                return _kit = m.FindKitNoLock(KitName, true);
            }
        }
        protected override void Init()
        {
            if (IsLoadout)
            {
                if (Sign.text.Length > Prefix.Length + LoadoutPrefix.Length &&
                    byte.TryParse(Sign.text.Substring(Prefix.Length + LoadoutPrefix.Length), NumberStyles.Number, Data.AdminLocale, out byte loadout))
                {
                    LoadoutIndex = loadout;
                }
                else if (!_warn)
                {
                    L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid loadout id: \"" + Sign.text + "\".");
                    _warn = true;
                }
            }
            else
            {
                if (Sign.text.Length > Prefix.Length + KitPrefix.Length)
                {
                    KitName = Sign.text.Substring(Prefix.Length + KitPrefix.Length);
                    _ = Kit;
                }
                else
                {
                    KitName = null!;
                    if (!_warn)
                    {
                        L.LogWarning("Sign at " + gameObject.transform.position + " has an invalid kit id: \"" + Sign.text + "\".");
                        _warn = true;
                    }
                }
            }
        }
        public override string Translate(string language, UCPlayer player)
        {
            if (IsLoadout)
            {
                return LoadoutIndex > -1 ? Localization.TranslateLoadoutSign((byte)LoadoutIndex, language, player!) : Sign.text;
            }
            Kit? kit = Kit?.Item;
            if (kit != null)
            {
                return Localization.TranslateKitSign(language, kit, player);
            }
            return Sign.text;
        }
    }
}
