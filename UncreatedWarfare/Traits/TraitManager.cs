using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Traits;
public class TraitManager : ListSingleton<TraitData>, IPlayerInitListener, IGameStartListener, ILevelStartListener, IPlayerAsyncInitListener
{
    public List<Trait> ActiveTraits;
    public static TraitManager Singleton;
    public static readonly BuffUI BuffUI = new BuffUI();
    private static readonly TraitData[] DEFAULT_TRAITS = new TraitData[]
    {
        Motivated.DEFAULT_DATA,
        RapidDeployment.DEFAULT_DATA,
        Intimidation.DEFAULT_DATA,
        BadOmen.DEFAULT_DATA,
        AceArmor.DEFAULT_DATA,
        Ghost.DEFAULT_DATA,
        GuidedByGod.DEFAULT_DATA,
        SelfRevive.DEFAULT_DATA,
        StrengthInNumbers.DEFAULT_DATA,
        Superheated.DEFAULT_DATA
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
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.GetTeam() is 1 or 2)
                BuffUI.SendBuffs(player);
        }
        
        

        KitManager.OnKitChanged += OnKitChagned;
        EventDispatcher.OnGroupChanged += OnGroupChanged;
    }
    public override void Unload()
    {
        EventDispatcher.OnGroupChanged -= OnGroupChanged;
        KitManager.OnKitChanged -= OnKitChagned;
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
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            pl.ShovelSpeedMultipliers.Clear();
            for (int j = 0; j < pl.ActiveBuffs.Length; ++j)
                pl.ActiveBuffs[j] = null;
            pl.ActiveTraits.Clear();
        }
        BuffUI.ClearFromAllPlayers();
        Singleton = null!;
    }
    public void OnLevelReady()
    {
        if (BarricadeManager.regions == null)
            return;

        for (byte x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                BarricadeRegion reg = BarricadeManager.regions[x, y];
                for (int i = 0; i < reg.drops.Count; ++i)
                {
                    if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(TraitSigns.TRAIT_SIGN_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        TraitData? d = GetData(sign.text.Substring(TraitSigns.TRAIT_SIGN_PREFIX.Length));
                        if (d != null)
                            TraitSigns.InitTraitSign(d, reg.drops[i], sign);
                    }
                }
            }
        }
    }
    protected override void OnRead()
    {
        for (int i = 0; i < Count; ++i)
        {
            Type type = this[i].Type;
            if (type != null)
            {
                FieldInfo? info = type.GetField("DATA", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (info != null && info.FieldType.IsAssignableFrom(typeof(TraitData)))
                    info.SetValue(null, this[i]);
            }
        }
        base.OnRead();
    }
    private void OnGroupChanged(GroupChanged e)
    {
        TraitSigns.SendAllTraitSigns(e.Player);
        if (e.NewGroup is 1 or 2)
            BuffUI.SendBuffs(e.Player);
    }
    private void OnKitChagned(UCPlayer player, Kit kit, string oldKit)
    {
        TraitSigns.SendAllTraitSigns(player);
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff)
            {
                if (buff.IsActivated)
                {
                    if (!buff.Data.CanClassUse(kit.Class))
                    {
                        buff.IsActivated = false;
                        player.SendChat(T.TraitDisabledKitNotSupported, buff);
                        // unsure if this should stay in, but could possibly be nice if a player switches kit then has to watch their timer run out while waiting on a cooldown.
                        CooldownManager.RemoveCooldown(player, ECooldownType.REQUEST_KIT);
                    }
                }
                else if (CheckReactivateValid(buff))
                {
                    buff.IsActivated = true;
                    player.SendChat(T.TraitReactivated, buff);
                }
            }
        }
    }
    private static bool CheckReactivateValid(Trait trait)
    {
        if (!trait.Data.CanClassUse(trait.TargetPlayer.KitClass))
            return false;
        if (!trait.Data.CanGamemodeUse())
            return false;
        if (trait.Data.RequireSquad || trait.Data.RequireSquadLeader)
        {
            Squad? sq = trait.TargetPlayer.Squad;
            if (sq is null || (trait.Data.RequireSquadLeader && sq.Leader.Steam64 != trait.TargetPlayer.Steam64))
                return false;
        }

        return trait is not Buff b || b.CanEnable;
    }
    public void OnPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        BuffUI.SendBuffs(player);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        TraitSigns.BroadcastAllTraitSigns();
    }
    public static bool TryCreate<T>(UCPlayer player, out T trait) where T : Trait
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        Type type = typeof(T);
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].Type == type)
            {
                if (TryCreate(Singleton[i], player, out Trait trait2))
                {
                    trait = (trait2 as T)!;
                    return trait is not null;
                }
                trait = null!;
                return false;
            }
        }
        trait = null!;
        return false;
    }
    public static bool TryCreate(TraitData data, UCPlayer player, out Trait trait)
    {
        (trait = (player.Player.gameObject.AddComponent(data.Type) as Trait)!)?.Init(data, player);
        return trait is not null;
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
        TraitSigns.SendAllTraitSigns(player);
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
        TraitSigns.SendAllTraitSigns(player);
    }
    internal static void OnPlayerJoinSquad(UCPlayer player, Squad joined)
    {
        // give other squadmates the new player's buffs
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff)
            {
                if (!buff.IsActivated && CheckReactivateValid(buff))
                {
                    buff.IsActivated = true;
                    player.SendChat(T.TraitReactivated, buff);
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
        TraitSigns.SendAllTraitSigns(player);
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
            TraitSigns.SendAllTraitSigns(member);
        }
    }
    internal static void DeactivateTrait(Trait trait)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();
        Singleton.ActiveTraits.Remove(trait);
        trait.TargetPlayer.ActiveTraits.Remove(trait);
        if (trait is Buff buff)
        {
            BuffUI.RemoveBuff(buff.TargetPlayer, buff);
        }

        L.LogDebug("Deactivated trait: " + trait.Data.Type.Name);
    }
    public static bool IsAffectedOwner<T>(UCPlayer player) where T : Buff => IsAffectedOwner(typeof(T), player, out _);
    public static bool IsAffectedSquad<T>(UCPlayer player) where T : Buff => IsAffectedSquad(typeof(T), player, out _);
    public static bool IsAffectedOwner(Type type, UCPlayer player, out Trait trait)
    {
        if (player.ActiveTraits is null)
        {
            trait = null!;
            return false;
        }
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            Trait t = player.ActiveTraits[i];
            if (t.Data.Type == type && t.isActiveAndEnabled && t.Inited && (t is not Buff b || b.IsActivated))
            {
                trait = t;
                return true;
            }
        }

        trait = null!;
        return false;
    }
    public static bool IsAffectedSquad(Type type, UCPlayer player, out Trait trait)
    {
        if (IsAffectedOwner(type, player, out trait)) return true;
        if (player.Squad is null) return false;
        bool onlySl = false;
        if (Loaded)
        {
            for (int i = 0; i < Singleton.Count; ++i)
            {
                if (Singleton[i].Type == type)
                {
                    if (!Singleton[i].EffectDistributedToSquad)
                        return false;
                    onlySl = Singleton[i].RequireSquadLeader;
                    break;
                }
            }
        }
        if (onlySl)
            return player.Squad.Leader.Steam64 != player.Steam64 && IsAffectedOwner(type, player.Squad.Leader, out trait);

        for (int i = 0; i < player.Squad.Members.Count; ++i)
        {
            UCPlayer pl = player.Squad.Members[i];
            if (pl.Steam64 == player.Steam64) continue;
            if (IsAffectedOwner(type, pl, out trait)) return true;
        }
        trait = null!;
        return false;
    }
    public static bool IsAffected(TraitData data, UCPlayer player, out Trait trait)
    {
        if (IsAffectedOwner(data.Type, player, out trait)) return true;
        if (player.Squad is null || !data.EffectDistributedToSquad) return false;
        if (data.RequireSquadLeader)
            return player.Squad.Leader.Steam64 != player.Steam64 && IsAffectedOwner(data.Type, player.Squad.Leader, out trait);

        for (int i = 0; i < player.Squad.Members.Count; ++i)
        {
            UCPlayer pl = player.Squad.Members[i];
            if (pl.Steam64 == player.Steam64) continue;
            if (IsAffectedOwner(data.Type, pl, out trait)) return true;
        }

        trait = null!;
        return false;
    }
    /// <exception cref="CommandInteraction"/>
    public static void RequestTrait(TraitData trait, CommandInteraction ctx)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();

        ulong team = ctx.Caller.GetTeam();
        if (trait.Team is 1 or 2 && trait.Team != team)
            throw ctx.Reply(T.RequestTraitWrongTeam, trait, TeamManager.GetFactionSafe(trait.Team)!);

        if (!trait.CanGamemodeUse())
            throw ctx.Reply(T.RequestTraitGamemodeLocked, trait, Data.Gamemode);

        if (trait.Delays != null && trait.Delays.Length > 0 && Delay.IsDelayed(trait.Delays, out Delay delay, team))
        {
            Localization.SendDelayRequestText(in delay, ctx.Caller, team, Localization.EDelayMode.TRAITS);
            throw ctx.Defer();
        }

        for (int i = 0; i < ctx.Caller.ActiveTraits.Count; ++i)
        {
            if (ctx.Caller.ActiveTraits[i].Data.Type == trait.Type)
                throw ctx.Reply(T.TraitAlreadyActive, trait);
        }

        if (!ctx.Caller.OnDuty())
        {
            if (CooldownManager.HasCooldownNoStateCheck(ctx.Caller, ECooldownType.REQUEST_TRAIT_GLOBAL, out Cooldown cooldown))
                throw ctx.Reply(T.RequestTraitGlobalCooldown, cooldown);

            if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.REQUEST_TRAIT_SINGLE, out cooldown, trait.TypeName))
                throw ctx.Reply(T.RequestTraitSingleCooldown, trait, cooldown);
        }

        bool isBuff = typeof(Buff).IsAssignableFrom(trait.Type);
        if (isBuff && BuffUI.HasBuffRoom(ctx.Caller, false))
            throw ctx.Reply(T.RequestTraitTooManyBuffs);

        for (int i = 0; i < trait.UnlockRequirements.Length; i++)
        {
            BaseUnlockRequirement req = trait.UnlockRequirements[i];
            if (req.CanAccess(ctx.Caller))
                continue;
            if (req is LevelUnlockRequirement level)
            {
                RankData data = new RankData(Points.GetLevelXP(level.UnlockLevel));
                throw ctx.Reply(T.RequestTraitLowLevel, trait, data);
            }
            else if (req is RankUnlockRequirement rank)
            {
                ref Ranks.RankData data = ref Ranks.RankManager.GetRank(rank.UnlockRank, out bool success);
                if (!success)
                    L.LogWarning("Invalid rank order in trait requirement: " + trait.TypeName + " :: " + rank.UnlockRank + ".");
                throw ctx.Reply(T.RequestTraitLowRank, trait, data);
            }
            else if (req is QuestUnlockRequirement quest)
            {
                if (Assets.find(quest.QuestID) is QuestAsset asset)
                {
                    ctx.Caller.Player.quests.sendAddQuest(asset.id);
                    throw ctx.Reply(T.RequestTraitQuestIncomplete, trait, asset);
                }
                else
                {
                    throw ctx.Reply(T.RequestTraitQuestIncomplete, trait, null!);
                }
            }
            else
            {
                L.LogWarning("Unhandled trait requirement type: " + req.GetType().Name);
                throw ctx.SendUnknownError();
            }
        }

        if (ctx.Caller.Kit is null || ctx.Caller.KitClass <= EClass.UNARMED)
            throw ctx.Reply(T.RequestTraitNoKit);

        if (!trait.CanClassUse(ctx.Caller.KitClass))
            throw ctx.Reply(T.RequestTraitClassLocked, trait, ctx.Caller.KitClass);

        if (ctx.Caller.Squad is null)
        {
            if (trait.RequireSquadLeader)
                throw ctx.Reply(T.RequestTraitNotSquadLeader, trait);
            else if (trait.RequireSquad)
                throw ctx.Reply(T.RequestTraitNoSquad, trait);
        }
        else if (trait.RequireSquadLeader && ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
            throw ctx.Reply(T.RequestTraitNotSquadLeader, trait);



        Task.Run(async () =>
        {
            if (trait.CreditCost != 0)
            {
                await ctx.Caller.PurchaseSync.WaitAsync();
                try
                {
                    await Points.UpdatePointsAsync(ctx.Caller);
                    if (ctx.Caller.CachedCredits < trait.CreditCost)
                    {
                        await UCWarfare.ToUpdate();
                        ctx.Reply(T.RequestKitCantAfford, trait.CreditCost - ctx.Caller.CachedCredits, trait.CreditCost);
                        return;
                    }
                    await Points.AwardCreditsAsync(ctx.Caller, -trait.CreditCost, isPurchase: true);
                }
                finally
                {
                    ctx.Caller.PurchaseSync.Release();
                }
            }

            await UCWarfare.ToUpdate();
            if (ctx.Caller.Squad is null)
            {
                if (trait.RequireSquadLeader)
                {
                    ctx.Caller.SendChat(T.RequestTraitNotSquadLeader, trait);
                    return;
                }
                else if (trait.RequireSquad)
                {
                    ctx.Caller.SendChat(T.RequestTraitNoSquad, trait);
                    return;
                }
            }
            else if (trait.RequireSquad && ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
            {
                ctx.Caller.SendChat(T.RequestTraitNotSquadLeader, trait);
                return;
            }

            if (isBuff && GetBuffCount(ctx.Caller) > BuffUI.MAX_BUFFS)
            {
                ctx.Caller.SendChat(T.RequestTraitTooManyBuffs);
                return;
            }
            for (int i = 0; i < ctx.Caller.ActiveTraits.Count; ++i)
            {
                if (ctx.Caller.ActiveTraits[i].Data.Type == trait.Type)
                {
                    ctx.Caller.SendChat(T.TraitAlreadyActive, trait);
                    return;
                }
            }

            ctx.LogAction(EActionLogType.REQUEST_TRAIT, $"Trait {trait.TypeName}, Team {team}, Cost: {trait.CreditCost}");
            if (CooldownManager.Config.GlobalTraitCooldown != null && CooldownManager.Config.GlobalTraitCooldown.HasValue && CooldownManager.Config.GlobalTraitCooldown.Value >= 0)
                CooldownManager.StartCooldown(ctx.Caller, ECooldownType.REQUEST_TRAIT_GLOBAL, CooldownManager.Config.GlobalTraitCooldown.Value);
            if (trait.Cooldown is not null && trait.Cooldown.HasValue && trait.Cooldown.Value >= 0f)
                CooldownManager.StartCooldown(ctx.Caller, ECooldownType.REQUEST_TRAIT_SINGLE, trait.Cooldown.Value, trait.TypeName);
            TraitSigns.SendAllTraitSigns(ctx.Caller);
            GiveTrait(ctx.Caller, trait);
        });
        ctx.Defer();
    }
    private static int GetBuffCount(UCPlayer player)
    {
        int ct = 0;
        if (player.ActiveBuffs is not null)
        {
            for (int i = 0; i < player.ActiveBuffs.Length; ++i)
            {
                if (player.ActiveBuffs[i] != null)
                    ++ct;
            }
        }

        if (Data.Gamemode.State == EState.STAGING)
        {
            for (int i = 0; i < player.ActiveTraits.Count; ++i)
            {
                if (player.ActiveTraits[i] is Buff b && b.IsAwaitingStagingPhase)
                    ++ct;
            }
        }
        return ct;
    }
    public static void GiveTrait(UCPlayer player, TraitData data)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();

        if (!TryCreate(data, player, out _))
            player.SendChat(T.UnknownError);
    }

    public void OnAsyncInitComplete(UCPlayer player)
    {
        TraitSigns.SendAllTraitSigns(player);
    }

    internal static TraitData? FindTrait(string trait)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();

        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].TypeName.Equals(trait, StringComparison.OrdinalIgnoreCase))
                return Singleton[i];
        }
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].TypeName.IndexOf(trait, StringComparison.OrdinalIgnoreCase) != -1)
                return Singleton[i];
        }
        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].NameTranslations.Translate(L.DEFAULT).IndexOf(trait, StringComparison.OrdinalIgnoreCase) != -1)
                return Singleton[i];
        }

        return null;
    }
}