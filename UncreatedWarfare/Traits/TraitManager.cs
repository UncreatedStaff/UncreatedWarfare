using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Traits;
public class TraitManager : ListSingleton<TraitData>
{
    public List<Trait> ActiveTraits;
    public static TraitManager Singleton;
    public static readonly TraitUI TraitUI;
    private static readonly TraitData[] DEFAULT_TRAITS = new TraitData[]
    {
        Motivated.DEFAULT_DATA
    };
    public static bool Loaded => Singleton.IsLoaded<TraitManager, TraitData>();
    public TraitManager() : base("traits", Data.Paths.TraitDataStorage) { }
    protected override string LoadDefaults() => JsonSerializer.Serialize(DEFAULT_TRAITS, JsonEx.serializerSettings);
    public override void Load()
    {
        Singleton = this;
        if (ActiveTraits is null)
            ActiveTraits = new List<Trait>(128);
        else if (ActiveTraits.Count > 0)
            ActiveTraits.Clear();

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            TraitUI.SendTraits(PlayerManager.OnlinePlayers[i], true);
    }
    public override void Unload()
    {
        for (int i = 0; i < ActiveTraits.Count; ++i)
        {
            Trait t = ActiveTraits[i];
            if (t.isActiveAndEnabled)
                UnityEngine.Object.Destroy(t);
        }

        TraitUI.ClearFromAllPlayers();
        ActiveTraits.Clear();
        Singleton = null!;
    }
    protected override void OnRead()
    {
        if (ActiveTraits is null) return;
        for (int i = 0; i < ActiveTraits.Count; ++i)
        {
            Trait t = ActiveTraits[i];
            Type type = t.Data.Type;
            for (int j = 0; j < Count; ++j)
            {
                TraitData d = this[j];
                if (d.Type == type)
                {
                    t.Data = d;
                    break;
                }
            }
        }
    }
    public static bool TryCreate<T>(UCPlayer player, out Trait trait) where T : Trait
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        Type type = typeof(T);
        if (type.IsAbstract || !type.IsPublic || type.IsNested || type.IsGenericType)
            throw new ArgumentException(nameof(T), "Invalid type argument: " + type.Name);
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].Type == type)
            {
                trait = player.Player.gameObject.AddComponent<T>();
                trait.Init(Singleton[i], player);
                return true;
            }
        }
        trait = null!;
        return false;
    }
    internal static void ActivateTrait(Trait trait)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        Singleton.ActiveTraits.Add(trait);
        trait.TargetPlayer.ActiveTraits.Add(trait);
        if (trait.Data.DistributedToSquad && trait.TargetPlayer.Squad is not null)
        {
            for (int i = 0; i < trait.TargetPlayer.Squad.Members.Count; ++i)
            {
                TraitUI.SendTraits(trait.TargetPlayer.Squad.Members[i], false);
            }
        }
        else
            TraitUI.SendTraits(trait.TargetPlayer, false);
        L.LogDebug("Activated trait: " + trait.Data.Type.Name);
    }
    internal static void DeactivateTrait(Trait trait)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        Singleton.ActiveTraits.Remove(trait);
        trait.TargetPlayer.ActiveTraits.Remove(trait);
        L.LogDebug("Deactivated trait: " + trait.Data.Type.Name);
    }
    public static bool IsAffectedOwner<T>(UCPlayer player) where T : Buff => IsAffectedOwner(typeof(T), player);
    public static bool IsAffectedSquad<T>(UCPlayer player) where T : Buff => IsAffectedSquad(typeof(T), player);
    public static bool IsAffectedOwner(Type type, UCPlayer player)
    {
        if (player.ActiveTraits is null) return false;
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            Trait t = player.ActiveTraits[i];
            if (t.Data.Type == type && t.isActiveAndEnabled && t.Inited)
                return true;
        }
        return false;
    }
    public static bool IsAffectedSquad(Type type, UCPlayer player)
    {
        if (IsAffectedOwner(type, player)) return true;
        if (player.Squad is null) return false;
        bool onlySl = false;
        if (Loaded)
        {
            for (int i = 0; i < Singleton.Count; ++i)
            {
                if (Singleton[i].Type == type)
                {
                    onlySl = Singleton[i].SquadLeaderRequired;
                    break;
                }
            }
        }
        if (onlySl)
            return player.Squad.Leader.Steam64 != player.Steam64 && IsAffectedOwner(type, player.Squad.Leader);

        for (int i = 0; i < player.Squad.Members.Count; ++i)
        {
            UCPlayer pl = player.Squad.Members[i];
            if (pl.Steam64 == player.Steam64) continue;
            if (IsAffectedOwner(type, pl)) return true;
        }
        return false;
    }
}