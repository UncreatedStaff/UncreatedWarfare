using SDG.Unturned;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits;
public abstract class Trait : MonoBehaviour, ITranslationArgument
{
    protected static readonly char[] dataSplitChars = new char[] { ',' };
    private TraitData _data;
    private UCPlayer _targetPlayer;
    private bool _inited = false;
    protected Coroutine? _coroutine;
    public float ActiveTime { get; private set; }
    public float StartTime { get; private set; }

    public TraitData Data
    {
        get => _data;
        internal set => _data = value;
    }
    public UCPlayer TargetPlayer => _targetPlayer;
    public bool Inited => _inited;
    public bool IsAwaitingStagingPhase { get; private set; }
    public bool _hasStarted = false;
    public virtual void Init(TraitData data, UCPlayer target)
    {
        _data = data;
        _targetPlayer = target;
        _inited = true;
    }

    [SuppressMessage(Warfare.Data.SUPPRESS_CATEGORY, Warfare.Data.SUPPRESS_ID)]
    private void Start()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + this.GetType().Name + " was not initialized. You must run Trait.Init(...) within the same frame of creating it.");
        TraitManager.ActivateTrait(this);
        if (Warfare.Data.Gamemode.State == Gamemodes.EState.STAGING)
        {
            Warfare.Data.Gamemode.StagingPhaseOver += InternalStart;
            IsAwaitingStagingPhase = true;
            TargetPlayer.SendChat(T.TraitAwaitingStagingPhase, Data);
            StartTime = Time.realtimeSinceStartup + Warfare.Data.Gamemode.StagingSeconds;
            TraitSigns.SendAllTraitSigns(TargetPlayer, Data);
        }
        else
        {
            InternalStart();
        }
    }

    private void InternalStart()
    {
        if (IsAwaitingStagingPhase)
        {
            Warfare.Data.Gamemode.StagingPhaseOver -= InternalStart;
            IsAwaitingStagingPhase = false;
        }
        if (this == null)
            return;
        if (Data.LastsUntilDeath)
            TargetPlayer.SendChat(T.RequestTraitGivenUntilDeath, Data);
        else if (Data.EffectDuration > 0)
            TargetPlayer.SendChat(T.RequestTraitGivenTimer, Data, Localization.GetTimeFromSeconds(Mathf.CeilToInt(Data.EffectDuration), TargetPlayer));
        else
            TargetPlayer.SendChat(T.RequestTraitGiven, Data);
        OnActivate();
        StartTime = Time.realtimeSinceStartup;
        if (Data.LastsUntilDeath)
        {
            EventDispatcher.OnPlayerDied += OnPlayerDied;
            if (Data.TickSpeed > 0f)
                _coroutine = StartCoroutine(EffectCoroutine());
        }
        else if (Data.EffectDuration > 0f)
            _coroutine = StartCoroutine(EffectCoroutine());
        else
            Destroy(this);
    }

    private void OnPlayerDied(PlayerDied e)
    {
        if (e.Player.Steam64 != TargetPlayer.Steam64)
            return;

        EventDispatcher.OnPlayerDied -= OnPlayerDied;
        ActiveTime = Time.realtimeSinceStartup - StartTime;
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
        TargetPlayer.SendChat(T.TraitExpiredDeath, Data);
        Destroy(this);
    }

    [SuppressMessage(Warfare.Data.SUPPRESS_CATEGORY, Warfare.Data.SUPPRESS_ID)]
    private void OnDestroy()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + this.GetType().Name + " was not initialized.");

        TraitManager.DeactivateTrait(this);
        OnDeactivate();
        TraitSigns.SendAllTraitSigns(TargetPlayer, Data);
    }
    protected virtual void OnActivate()
    {
        GiveItems();
        TraitSigns.SendAllTraitSigns(TargetPlayer, Data);
    }
    protected virtual void OnDeactivate() { }
    /// <summary>Only called if <see cref="TraitData.TickSpeed"/> is > 0.</summary>
    protected virtual void Tick() { }
    private void GiveItems()
    {
        if (Data.ItemsGiven is null || Data.ItemsGiven.Length > 0)
            return;

        if (!Data.ItemsDistributedToSquad || _targetPlayer.Squad is null || (Data.RequireSquadLeader && _targetPlayer.Squad.Leader.Steam64 != _targetPlayer.Steam64))
            GiveItems(_targetPlayer);
        else
        {
            for (int i = 0; i < _targetPlayer.Squad.Members.Count; ++i)
                GiveItems(_targetPlayer.Squad.Members[i]);
        }
    }
    private void GiveItems(UCPlayer player)
    {
        for (int i = 0; i < Data.ItemsGiven.Length; ++i)
            if (Data.ItemsGiven[i].ValidReference(out ItemAsset asset))
                player.Player.inventory.tryAddItem(new Item(asset.id, true), false, true);
    }
    private IEnumerator EffectCoroutine(float progressedTime = 0f)
    {
        float tl = Data.EffectDuration - progressedTime;
        ActiveTime = progressedTime;
        if (tl > 0 || Data.LastsUntilDeath)
        {
            if (Data.TickSpeed > 0f)
            {
                int ticks = Mathf.CeilToInt(tl / Data.TickSpeed);
                Buff? buff = this as Buff;
                bool hasHitBlinkMrkr = buff == null;
                for (int i = 0; i < ticks; ++i)
                {
                    yield return new WaitForSecondsRealtime(Data.TickSpeed);
                    ActiveTime += Data.TickSpeed;
                    if (!hasHitBlinkMrkr && Data.EffectDuration - ActiveTime <= Buff.BLINK_LEAD_TIME)
                    {
                        hasHitBlinkMrkr = true;
                        buff!._shouldBlink = true;
                        TraitManager.BuffUI.UpdateBuffTimeState(buff);
                    }
                    Tick();
                }
            }
            else if (this is not Buff buff)
            {
                yield return new WaitForSecondsRealtime(tl);
            }
            else if (tl < Buff.BLINK_LEAD_TIME)
            {
                if (!buff._shouldBlink)
                {
                    buff._shouldBlink = true;
                    buff.OnBlinkingUpdated();
                }
                yield return new WaitForSecondsRealtime(tl);
            }
            else
            {
                yield return new WaitForSecondsRealtime(tl - Buff.BLINK_LEAD_TIME);
                ActiveTime = Data.EffectDuration - Buff.BLINK_LEAD_TIME;
                buff._shouldBlink = true;
                buff.OnBlinkingUpdated();
                yield return new WaitForSecondsRealtime(Buff.BLINK_LEAD_TIME);
            }
        }
        ActiveTime = Data.EffectDuration;
        yield return null;
        if (!Data.LastsUntilDeath)
            TargetPlayer.SendChat(T.TraitExpiredTime, Data);
        _coroutine = null;
        Destroy(this);
    }
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags) => Data is null
        ? Translation.Null(flags)
        : (Data as ITranslationArgument).Translate(language, format, target, ref flags);
}

public class TraitData : ITranslationArgument
{
    [JsonIgnore] private string _typeName;
    [JsonIgnore] public Type Type { get; private set; }

    [JsonPropertyName("type")]
    public string TypeName
    {
        get => _typeName;
        set
        {
            if (value is null)
                throw new JsonException("Type name must not be null.");
            Type = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => !x.IsAbstract && x.IsPublic && !x.IsGenericType && !x.IsNested && typeof(Trait).IsAssignableFrom(x))
                .FirstOrDefault(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                   ?? throw new JsonException("Type name could not be identified: \"" + value + "\".");
            _typeName = value;
        }
    }

    [CommandSettable]
    [JsonPropertyName("credit_cost")]
    public int CreditCost;

    [JsonPropertyName("squad_distribute_effect")]
    public bool EffectDistributedToSquad;

    [JsonPropertyName("squad_distribute_items")]
    public bool ItemsDistributedToSquad;

    [JsonPropertyName("squad_distributed_multiplier")]
    public float SquadDistributedMultiplier = 1f;

    [JsonPropertyName("squad_distributed_multiplier_leader")]
    public float SquadLeaderDistributedMultiplier = 1f;

    [JsonPropertyName("require_squad")]
    public bool RequireSquad;

    [JsonPropertyName("require_squad_leader")]
    public bool RequireSquadLeader;

    [JsonPropertyName("gamemode_list_is_blacklist")]
    public bool GamemodeListIsBlacklist;

    [JsonPropertyName("gamemode_list")]
    public string[] GamemodeList;

    [JsonPropertyName("class_list_is_blacklist")]
    public bool ClassListIsBlacklist;

    [JsonPropertyName("class_list")]
    public EClass[] ClassList;

    [JsonPropertyName("lasts_until_death")]
    public bool LastsUntilDeath;

    [JsonPropertyName("team")]
    public ulong Team;

    [JsonPropertyName("unlock_requirements")]
    public BaseUnlockRequirement[] UnlockRequirements { get; set; }

    [JsonPropertyName("name")]
    public TranslationList NameTranslations;

    [JsonPropertyName("description")]
    public TranslationList DescriptionTranslations;

    [JsonPropertyName("items_given")]
    public RotatableConfig<JsonAssetReference<ItemAsset>>[] ItemsGiven { get; set; }

    [JsonPropertyName("icon")]
    public RotatableConfig<string> Icon;

    [JsonPropertyName("request_cooldown")]
    public RotatableConfig<float> Cooldown;

    [JsonPropertyName("delays")]
    public Delay[] Delays;

    [JsonPropertyName("effect_duration")]
    public float EffectDuration;

    [JsonPropertyName("tick_speed")]
    public float TickSpeed;

    [JsonPropertyName("data")]
    public string Data;

    [JsonConstructor]
    public TraitData()
    {

    }
    public bool CanClassUse(EClass @class)
    {
        if (ClassList is null) return true;
        for (int i = 0; i < ClassList.Length; ++i)
        {
            if (ClassList[i] == @class) return !ClassListIsBlacklist;
        }
        return ClassListIsBlacklist;
    }
    /// <exception cref="InvalidOperationException">No gamemode is loaded.</exception>
    public bool CanGamemodeUse()
    {
        string gm = Warfare.Data.Gamemode is null ? throw new InvalidOperationException("There is not a loaded gamemode.") : Warfare.Data.Gamemode.Name;
        if (GamemodeList is null) return true;
        for (int i = 0; i < GamemodeList.Length; ++i)
        {
            if (GamemodeList[i].Equals(gm, StringComparison.OrdinalIgnoreCase)) return !GamemodeListIsBlacklist;
        }
        return GamemodeListIsBlacklist;
    }

    [FormatDisplay(typeof(Trait), "Name")]
    [FormatDisplay("Name")]
    public const string NAME = "n";
    [FormatDisplay(typeof(Trait), "Type Name")]
    [FormatDisplay("Type Name")]
    public const string TYPE_NAME = "t";
    [FormatDisplay(typeof(Trait), "Colored Type Name")]
    [FormatDisplay("Colored Type Name")]
    public const string COLOR_TYPE_NAME = "ct";
    [FormatDisplay(typeof(Trait), "Description")]
    [FormatDisplay("Description")]
    public const string DESCRIPTION = "d";
    [FormatDisplay(typeof(Trait), "Colored Name")]
    [FormatDisplay("Colored Name")]
    public const string COLOR_NAME = "cn";
    [FormatDisplay(typeof(Trait), "Colored Description")]
    [FormatDisplay("Colored Description")]
    public const string COLOR_DESCRIPTION = "cd";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null && !format.Equals(NAME, StringComparison.Ordinal))
        {
            if (format.Equals(TYPE_NAME, StringComparison.Ordinal))
                return TypeName;
            if (format.Equals(DESCRIPTION, StringComparison.Ordinal))
                return DescriptionTranslations != null
                    ? DescriptionTranslations.Translate(language).Replace('\n', ' ')
                    : Translation.Null(flags & TranslationFlags.NoRichText);
            if (format.Equals(COLOR_TYPE_NAME, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), TypeName, flags);
            if (format.Equals(COLOR_NAME, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), NameTranslations != null
                    ? NameTranslations.Translate(language).Replace('\n', ' ')
                    : TypeName, flags);
            if (format.Equals(COLOR_DESCRIPTION, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), DescriptionTranslations != null
                    ? DescriptionTranslations.Translate(language).Replace('\n', ' ')
                    : Translation.Null(flags & TranslationFlags.NoRichText), flags);
        }
        return NameTranslations != null
            ? NameTranslations.Translate(language).Replace('\n', ' ')
            : TypeName;
    }
}