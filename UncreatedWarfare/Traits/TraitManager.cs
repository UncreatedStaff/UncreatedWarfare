using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Traits.Buffs;

namespace Uncreated.Warfare.Traits;
public class TraitManager : ListSingleton<TraitData>, IPlayerInitListener
{
    public List<Trait> ActiveTraits;
    public static TraitManager Singleton;
    public static readonly BuffUI BuffUI = new BuffUI();
    private static readonly TraitData[] DEFAULT_TRAITS = new TraitData[]
    {
        Motivated.DEFAULT_DATA,
        RapidDeployment.DEFAULT_DATA
    };
    public static bool Loaded => Singleton.IsLoaded<TraitManager, TraitData>();
    public TraitManager() : base("traits", Data.Paths.TraitDataStorage) { }
    protected override void OnRead()
    {
        if (Loaded && BarricadeManager.regions != null)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    BarricadeRegion reg = BarricadeManager.regions[x, y];
                    for (int i = 0; i < reg.drops.Count; ++i)
                    {
                        if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(TraitSigns.TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                            Signs.BroadcastSign(sign.text, sign, x, y);
                    }
                }
            }
        }
    }
    protected override string LoadDefaults() => JsonSerializer.Serialize(DEFAULT_TRAITS, JsonEx.serializerSettings);
    public override void Load()
    {
        Singleton = this;
        if (ActiveTraits is null)
            ActiveTraits = new List<Trait>(128);
        else if (ActiveTraits.Count > 0)
            ActiveTraits.Clear();

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.GetTeam() is 1 or 2)
                BuffUI.SendBuffs(player);
        }
    }
    public override void Unload()
    {
        if (ActiveTraits != null)
        {
            for (int j = ActiveTraits.Count - 1; j >= 0; --j)
            {
                if (ActiveTraits[j].isActiveAndEnabled)
                    UnityEngine.Object.Destroy(ActiveTraits[j]);
            }

            ActiveTraits.Clear();
        }
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].ShovelSpeedMultipliers.Clear();
        BuffUI.ClearFromAllPlayers();
        Singleton = null!;
    }
    public void OnPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        BuffUI.SendBuffs(player);
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
        L.LogDebug("Activated trait: " + trait.Data.Type.Name);
    }
    internal static TraitData? GetData(Type type)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].Type == type)
                return Singleton[i];
        }
        return null;
    }
    internal static TraitData? GetData(string typeName)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                return Singleton[i];
        }
        return null;
    }
    /// <remarks>Run before old leader is changed.</remarks>
    internal static void OnPlayerPromotedSquadleader(UCPlayer player, Squad squad)
    {
        if (squad.Leader != null)
        {
            // change boosts from old leader
            for (int i = 0; i < squad.Leader.ActiveTraits.Count; ++i)
            {
                if (squad.Leader.ActiveTraits[i] is Buff buff && buff.Data.EffectDistributedToSquad)
                {
                    buff.SquadLeaderDemoted();
                }
            }
        }

        // change boosts for new leader
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff && buff.Data.EffectDistributedToSquad)
            {
                buff.SquadLeaderPromoted();
            }
        }
    }
    internal static void OnPlayerLeftSquad(UCPlayer player, Squad left)
    {
        // remove leaving player's buffs from other squadmates
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff)
            {
                if (!buff.IsActivated)
                    continue;
                if (buff.Data.EffectDistributedToSquad)
                {
                    for (int j = 0; j < left.Members.Count; ++j)
                    {
                        buff.RemovePlayer(left.Members[j]);
                    }
                }
                if (buff.Data.RequireSquad || buff.Data.RequireSquadLeader)
                {
                    buff.IsActivated = false;
                    player.SendChat(buff.Data.RequireSquadLeader ? T.TraitDisabledSquadLeaderDemoted : T.TraitDisabledSquadLeft, buff);
                }
            }
            
        }

        // remove other squadmates' buffs from leaving player
        for (int k = 0; k < left.Members.Count; ++k)
        {
            UCPlayer member = left.Members[k];
            if (member.Steam64 != player.Steam64)
                for (int i = 0; i < member.ActiveTraits.Count; ++i)
                {
                    if (member.ActiveTraits[i] is Buff buff && buff.Data.EffectDistributedToSquad)
                    {
                        buff.RemovePlayer(player);
                    }
                }
        }
    }
    internal static void OnPlayerJoinSquad(UCPlayer player, Squad joined)
    {
        // give other squadmates the new player's buffs
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff)
            {
                if (!buff.IsActivated)
                {
                    if ((buff.Data.RequireSquad || buff.Data.RequireSquadLeader) && player.Squad is not null)
                    {
                        if (!buff.Data.RequireSquadLeader || joined.Leader.Steam64 == player.Steam64)
                        {
                            buff.IsActivated = true;
                            player.SendChat(T.TraitReactivated, buff);
                        }
                    }
                }
                if (buff.IsActivated && buff.Data.EffectDistributedToSquad)
                {
                    for (int j = 0; j < joined.Members.Count; ++j)
                    {
                        UCPlayer m = joined.Members[j];
                        if (m.Steam64 != player.Steam64)
                            buff.AddPlayer(m);
                    }
                }
            }
        }

        // give new player other squadmates' buffs
        for (int k = 0; k < joined.Members.Count; ++k)
        {
            UCPlayer member = joined.Members[k];
            if (member.Steam64 != player.Steam64)
                for (int i = 0; i < member.ActiveTraits.Count; ++i)
                {
                    if (member.ActiveTraits[i] is Buff buff && buff.Data.EffectDistributedToSquad)
                    {
                        buff.AddPlayer(player);
                    }
                }
        }
    }
    /// <remarks>Run before <see cref="Squad.Members"/> is cleared.</remarks>
    internal static void OnSquadDisbanded(Squad squad)
    {
        for (int k = 0; k < squad.Members.Count; ++k)
        {
            UCPlayer member = squad.Members[k];
            for (int i = 0; i < member.ActiveTraits.Count; ++i)
            {
                if (member.ActiveTraits[i] is Buff buff)
                {
                    if (!buff.IsActivated)
                        continue;
                    if (buff.Data.EffectDistributedToSquad)
                    {
                        for (int j = 0; j < squad.Members.Count; ++j)
                        {
                            UCPlayer m2 = squad.Members[j];
                            if (m2.Steam64 != member.Steam64)
                                buff.RemovePlayer(m2);
                        }
                    }
                    if (buff.Data.RequireSquad || buff.Data.RequireSquadLeader)
                    {
                        buff.IsActivated = false;
                        buff.TargetPlayer.SendChat(T.TraitDisabledSquadLeft, buff);
                    }
                }
            }
        }
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
                    onlySl = Singleton[i].RequireSquadLeader;
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