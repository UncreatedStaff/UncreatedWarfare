using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Traits;
public class TraitManager : ListSingleton<TraitData>, IPlayerPreInitListener, IGameStartListener, ILevelStartListener, IPlayerPostInitListener, IUIListener, ITimeSyncListener
{
    public List<Trait> ActiveTraits;
    public static TraitManager Singleton;
    public static readonly BuffUI BuffUI = new BuffUI();
    private static readonly TraitData[] DefaultTraits =
    {
        Motivated.DefaultData,
        RapidDeployment.DefaultData,
        Intimidation.DefaultData,
        BadOmen.DefaultData,
        AceArmor.DefaultData,
        Ghost.DefaultData,
        GuidedByGod.DefaultData,
        SelfRevive.DefaultData,
        StrengthInNumbers.DefaultData,
        Superheated.DefaultData
    };
    public static bool Loaded => Singleton.IsLoaded<TraitManager, TraitData>();
    public TraitManager() : base("traits", Data.Paths.TraitDataStorage) { }
    protected override string LoadDefaults() => JsonSerializer.Serialize(DefaultTraits, ConfigurationSettings.JsonSerializerSettings);
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

        KitManager.OnKitChanged += OnKitChanged;
        EventDispatcher.GroupChanged += OnGroupChanged;

        Localization.ClearSection(TranslationSection.Traits);

    }
    public override void Unload()
    {
        EventDispatcher.GroupChanged -= OnGroupChanged;
        KitManager.OnKitChanged -= OnKitChanged;
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
            pl.UpdateShovelSpeedMultipliers();
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
                    if (reg.drops[i].interactable is InteractableSign sign && sign.text.StartsWith(Signs.Prefix + Signs.TraitPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        TraitData? d = GetData(sign.text.Substring(Signs.Prefix.Length + Signs.TraitPrefix.Length));
                        if (d != null)
                            TraitSigns.InitTraitSign(d, reg.drops[i]);
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
        Signs.UpdateTraitSigns(e.Player, null);
        if (e.NewGroup is 1 or 2)
            BuffUI.SendBuffs(e.Player);
    }
    private static void OnKitChanged(UCPlayer player, Kit? kit, Kit? oldKit)
    {
        Signs.UpdateTraitSigns(player, null);
        for (int i = 0; i < player.ActiveTraits.Count; ++i)
        {
            if (player.ActiveTraits[i] is Buff buff)
            {
                if (buff.IsActivated)
                {
                    if (!buff.Data.CanClassUse(kit == null ? Class.None : kit.Class))
                    {
                        buff.IsActivated = false;
                        player.SendChat(T.TraitDisabledKitNotSupported, buff);
                        // unsure if this should stay in, but could possibly be nice if a player switches kit then has to watch their timer run out while waiting on a cooldown.
                        CooldownManager.RemoveCooldown(player, CooldownType.RequestKit);
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
    public void OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        BuffUI.SendBuffs(player);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        Signs.UpdateTraitSigns(null, null);
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
        Signs.UpdateTraitSigns(player, null);
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
        Signs.UpdateTraitSigns(player, null);
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
        Signs.UpdateTraitSigns(player, null);
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
            Signs.UpdateTraitSigns(member, null);
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
    /// <exception cref="CommandContext"/>
    public static void RequestTrait(TraitData trait, CommandContext ctx)
    {
        Singleton.AssertLoaded<TraitManager, TraitData>();

        ulong team = ctx.Player.GetTeam();
        if (trait.Team is 1 or 2 && trait.Team != team)
            throw ctx.Reply(T.RequestTraitWrongTeam, trait, TeamManager.GetFactionSafe(trait.Team)!);

        if (!trait.CanGamemodeUse())
            throw ctx.Reply(T.RequestTraitGamemodeLocked, trait, Data.Gamemode);

        if (trait.Delays != null && trait.Delays.Length > 0 && Delay.IsDelayed(trait.Delays, out Delay delay, team))
        {
            Localization.SendDelayRequestText(in delay, ctx.Player, team, Localization.DelayTarget.Trait);
            throw ctx.Defer();
        }

        for (int i = 0; i < ctx.Player.ActiveTraits.Count; ++i)
        {
            if (ctx.Player.ActiveTraits[i].Data.Type == trait.Type)
                throw ctx.Reply(T.TraitAlreadyActive, trait);
        }

        if (!ctx.Player.OnDuty())
        {
            if (CooldownManager.HasCooldownNoStateCheck(ctx.Player, CooldownType.GlobalRequestTrait, out Cooldown cooldown))
                throw ctx.Reply(T.RequestTraitGlobalCooldown, cooldown);

            if (CooldownManager.HasCooldown(ctx.Player, CooldownType.IndividualRequestTrait, out cooldown, trait.TypeName))
                throw ctx.Reply(T.RequestTraitSingleCooldown, trait, cooldown);
        }

        bool isBuff = typeof(Buff).IsAssignableFrom(trait.Type);
        if (isBuff && !BuffUI.HasBuffRoom(ctx.Player, false))
            throw ctx.Reply(T.RequestTraitTooManyBuffs);

        for (int i = 0; i < trait.UnlockRequirements.Length; i++)
        {
            UnlockRequirement req = trait.UnlockRequirements[i];
            if (req.CanAccess(ctx.Player))
                continue;
            throw req.RequestTraitFailureToMeet(ctx, trait);
        }

        if (!ctx.Player.HasKit || ctx.Player.KitClass <= Class.Unarmed)
            throw ctx.Reply(T.RequestTraitNoKit);

        if (!trait.CanClassUse(ctx.Player.KitClass))
            throw ctx.Reply(T.RequestTraitClassLocked, trait, ctx.Player.KitClass);

        if (ctx.Player.Squad is null)
        {
            if (trait.RequireSquadLeader)
                throw ctx.Reply(T.RequestTraitNotSquadLeader, trait);
            else if (trait.RequireSquad)
                throw ctx.Reply(T.RequestTraitNoSquad, trait);
        }
        else if (trait.RequireSquadLeader && ctx.Player.Squad.Leader.Steam64 != ctx.CallerId.m_SteamID)
            throw ctx.Reply(T.RequestTraitNotSquadLeader, trait);

        ctx.Defer();

        UCWarfare.RunTask(async ctx =>
        {
            if (trait.CreditCost != 0)
            {
                await ctx.Player.PurchaseSync.WaitAsync();
                try
                {
                    await Points.UpdatePointsAsync(ctx.Player, false);
                    if (ctx.Player.CachedCredits < trait.CreditCost)
                    {
                        await UniTask.SwitchToMainThread();
                        ctx.Reply(T.RequestKitCantAfford, ctx.Player.CachedCredits, trait.CreditCost);
                        return;
                    }
                    await Points.AwardCreditsAsync(ctx.Player, -trait.CreditCost, isPurchase: true, @lock: false);
                }
                finally
                {
                    ctx.Player.PurchaseSync.Release();
                }
            }

            await UniTask.SwitchToMainThread();
            if (ctx.Player.Squad is null)
            {
                if (trait.RequireSquadLeader)
                {
                    ctx.Player.SendChat(T.RequestTraitNotSquadLeader, trait);
                    return;
                }
                else if (trait.RequireSquad)
                {
                    ctx.Player.SendChat(T.RequestTraitNoSquad, trait);
                    return;
                }
            }
            else if (trait.RequireSquad && ctx.Player.Squad.Leader.Steam64 != ctx.CallerId.m_SteamID)
            {
                ctx.Player.SendChat(T.RequestTraitNotSquadLeader, trait);
                return;
            }

            if (isBuff && GetBuffCount(ctx.Player) > BuffUI.MaxBuffs)
            {
                ctx.Player.SendChat(T.RequestTraitTooManyBuffs);
                return;
            }
            for (int i = 0; i < ctx.Player.ActiveTraits.Count; ++i)
            {
                if (ctx.Player.ActiveTraits[i].Data.Type == trait.Type)
                {
                    ctx.Player.SendChat(T.TraitAlreadyActive, trait);
                    return;
                }
            }

            ctx.LogAction(ActionLogType.RequestTrait, $"Trait {trait.TypeName}, Team {team}, Cost: {trait.CreditCost}");
            if (CooldownManager.Config.GlobalTraitCooldown != null && CooldownManager.Config.GlobalTraitCooldown.HasValue && CooldownManager.Config.GlobalTraitCooldown.Value >= 0)
                CooldownManager.StartCooldown(ctx.Player, CooldownType.GlobalRequestTrait, CooldownManager.Config.GlobalTraitCooldown.Value);
            if (trait.Cooldown is not null && trait.Cooldown.HasValue && trait.Cooldown.Value >= 0f)
                CooldownManager.StartCooldown(ctx.Player, CooldownType.IndividualRequestTrait, trait.Cooldown.Value, trait.TypeName);
            Signs.UpdateTraitSigns(ctx.Player, null);
            GiveTrait(ctx.Player, trait);
        }, ctx, ctx: $"Request trait {trait.TypeName} for {ctx.CallerId.m_SteamID}.");
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

        if (Data.Gamemode.State == State.Staging)
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

    void IPlayerPostInitListener.OnPostPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        Signs.UpdateTraitSigns(player, null);
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
            string? name = Singleton[i].NameTranslations.Translate(Localization.GetDefaultLanguage());
            if (name != null && name.IndexOf(trait, StringComparison.OrdinalIgnoreCase) != -1)
                return Singleton[i];
        }

        return null;
    }

    public void UpdateUI(UCPlayer player)
    {
        if (player.GetTeam() is 1 or 2 && !player.HasUIHidden && !Data.Gamemode.LeaderboardUp())
            BuffUI.SendBuffs(player);
    }
    public void ShowUI(UCPlayer player)
    {
        if (player.GetTeam() is 1 or 2 && !player.HasUIHidden && !Data.Gamemode.LeaderboardUp())
            BuffUI.SendBuffs(player);
    }
    public void HideUI(UCPlayer player)
    {
        BuffUI.ClearFromPlayer(player.Connection);
    }

    void ITimeSyncListener.TimeSync(float time)
    {
        TraitSigns.TimeSync();
    }
    public static void WriteTraitLocalization(LanguageInfo language, string path, bool writeMising)
    {
        if (Singleton == null)
            return;

        using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using StreamWriter writer = new StreamWriter(str, System.Text.Encoding.UTF8);
        writer.WriteLine("# Trait Translations");
        writer.WriteLine("#  <br> = new line on signs");
        writer.WriteLine();
        for (int i = 0; i < Singleton.Count; i++)
        {
            if (WriteTraitIntl(Singleton[i], language, writer, writeMising) && i != Singleton.Count - 1)
                writer.WriteLine();
        }
    }
    private static bool WriteTraitIntl(TraitData trait, LanguageInfo language, StreamWriter writer, bool writeMising)
    {
        TraitData? defaultFaction = Array.Find(DefaultTraits, x => x.Type == trait.Type);

        GetValue(trait.NameTranslations, defaultFaction?.NameTranslations, out string? nameValue, out bool isNameValueDefault);
        GetValue(trait.DescriptionTranslations, defaultFaction?.DescriptionTranslations, out string? descValue, out bool isDescValueDefault);

        if (!writeMising && isNameValueDefault && isDescValueDefault)
            return false;

        writer.WriteLine("# " + trait.NameTranslations.Translate(Localization.GetDefaultLanguage(), trait.TypeName).Replace("\r", string.Empty).Replace('\n', ' ') + " (ID: " + trait.TypeName + ")");

        if (writeMising || !isNameValueDefault)
        {
            if (!isNameValueDefault)
                writer.WriteLine("# Default: " + trait.NameTranslations.Translate(Localization.GetDefaultLanguage()));
            writer.WriteLine("Name: " + (nameValue?.Replace("\r", string.Empty).Replace("\n", "<br>") ?? trait.TypeName));
        }
        if (writeMising || !isDescValueDefault)
        {
            if (!isNameValueDefault)
                writer.WriteLine("# Default: " + trait.DescriptionTranslations.Translate(Localization.GetDefaultLanguage()));
            writer.WriteLine("Description: " + (descValue?.Replace("\r", string.Empty).Replace("\n", "<br>") ?? string.Empty));
        }

        return true;
        void GetValue(TranslationList? loaded, TranslationList? @default, out string? value, out bool isDefaultValue)
        {
            value = null;
            if (loaded != null)
            {
                if (loaded.TryGetValue(language.Code, out value))
                    isDefaultValue = language.IsDefault;
                else if (!language.IsDefault && loaded.TryGetValue(L.Default, out value))
                    isDefaultValue = true;
                else if (@default != null && @default.TryGetValue(language.Code, out value))
                    isDefaultValue = language.IsDefault;
                else if (@default != null && !language.IsDefault && @default.TryGetValue(L.Default, out value))
                    isDefaultValue = true;
                else
                {
                    value = trait.TypeName;
                    isDefaultValue = true;
                }
            }
            else if (@default != null && @default.TryGetValue(language.Code, out value))
                isDefaultValue = language.IsDefault;
            else if (@default != null && !language.IsDefault && @default.TryGetValue(L.Default, out value))
                isDefaultValue = true;
            else
                isDefaultValue = true;
        }
    }
}