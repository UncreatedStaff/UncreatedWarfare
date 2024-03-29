﻿using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DanielWillett.ReflectionTools;
using Uncreated.Framework;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits;
public abstract class Trait : MonoBehaviour, ITranslationArgument
{
    protected static readonly char[] DataSplitChars = { ',' };
    private TraitData _data;
    private UCPlayer _targetPlayer;
    private bool _inited;
    protected Coroutine? Coroutine;
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
    public bool HasStarted = false;
    public virtual void Init(TraitData data, UCPlayer target)
    {
        _data = data;
        _targetPlayer = target;
        _inited = true;
    }

    [UsedImplicitly]
    [SuppressMessage(Warfare.Data.SuppressCategory, Warfare.Data.SuppressID)]
    private void Start()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + GetType().Name + " was not initialized. You must run Trait.Init(...) within the same frame of creating it.");
        TraitManager.ActivateTrait(this);
        if (Warfare.Data.Gamemode.State == Gamemodes.State.Staging)
        {
            Warfare.Data.Gamemode.StagingPhaseOver += InternalStart;
            IsAwaitingStagingPhase = true;
            TargetPlayer.SendChat(T.TraitAwaitingStagingPhase, Data);
            StartTime = Time.realtimeSinceStartup + Warfare.Data.Gamemode.StagingSeconds;
            Signs.UpdateTraitSigns(TargetPlayer, Data);
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
            EventDispatcher.PlayerDied += OnPlayerDied;
            if (Data.TickSpeed > 0f)
                Coroutine = StartCoroutine(EffectCoroutine());
        }
        else if (Data.EffectDuration > 0f)
            Coroutine = StartCoroutine(EffectCoroutine());
        else
            Destroy(this);
    }

    private void OnPlayerDied(PlayerDied e)
    {
        if (e.Player.Steam64 != TargetPlayer.Steam64)
            return;

        EventDispatcher.PlayerDied -= OnPlayerDied;
        ActiveTime = Time.realtimeSinceStartup - StartTime;
        if (Coroutine != null)
        {
            StopCoroutine(Coroutine);
            Coroutine = null;
        }
        TargetPlayer.SendChat(T.TraitExpiredDeath, Data);
        Destroy(this);
    }

    [UsedImplicitly]
    [SuppressMessage(Warfare.Data.SuppressCategory, Warfare.Data.SuppressID)]
    private void OnDestroy()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + GetType().Name + " was not initialized.");

        TraitManager.DeactivateTrait(this);
        OnDeactivate();
        Signs.UpdateTraitSigns(TargetPlayer, Data);
    }
    protected virtual void OnActivate()
    {
        GiveItems();
        Signs.UpdateTraitSigns(TargetPlayer, Data);
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
        Coroutine = null;
        Destroy(this);
    }
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => Data is null
        ? Translation.Null(flags)
        : (Data as ITranslationArgument).Translate(language, format, target, culture, ref flags);
}

public class TraitData : ITranslationArgument
{
    private string _typeName;

    [JsonIgnore]
    public Type Type { get; private set; }

    [JsonPropertyName("type")]
    public string TypeName
    {
        get => _typeName;
        set
        {
            if (value is null)
                throw new JsonException("Type name must not be null.");
            Type = Accessor.GetTypesSafe(true)
                       .Where(x => !x.IsAbstract && x.IsPublic && !x.IsGenericType && !x.IsNested && typeof(Trait).IsAssignableFrom(x))
                       .FirstOrDefault(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                   ?? throw new JsonException("Type name could not be identified: \"" + value + "\".");
            _typeName = Type.Name;
        }
    }

    [CommandSettable]
    [JsonPropertyName("credit_cost")]
    public int CreditCost { get; set; }

    [JsonPropertyName("squad_distribute_effect")]
    public bool EffectDistributedToSquad { get; set; }

    [JsonPropertyName("squad_distribute_items")]
    public bool ItemsDistributedToSquad { get; set; }

    [JsonPropertyName("squad_distributed_multiplier")]
    public float SquadDistributedMultiplier { get; set; } = 1f;

    [JsonPropertyName("squad_distributed_multiplier_leader")]
    public float SquadLeaderDistributedMultiplier { get; set; } = 1f;

    [JsonPropertyName("require_squad")]
    public bool RequireSquad { get; set; }

    [JsonPropertyName("require_squad_leader")]
    public bool RequireSquadLeader { get; set; }

    [JsonPropertyName("gamemode_list_is_blacklist")]
    public bool GamemodeListIsBlacklist { get; set; }

    [JsonPropertyName("gamemode_list")]
    public string[] GamemodeList { get; set; }

    [JsonPropertyName("class_list_is_blacklist")]
    public bool ClassListIsBlacklist { get; set; }

    [JsonPropertyName("class_list")]
    [JsonConverter(typeof(ClassArrayConverter))]
    public Class[] ClassList { get; set; }

    [JsonPropertyName("lasts_until_death")]
    public bool LastsUntilDeath { get; set; }

    [JsonPropertyName("team")]
    public ulong Team { get; set; }

    [JsonPropertyName("unlock_requirements")]
    public UnlockRequirement[] UnlockRequirements { get; set; }

    [JsonPropertyName("name")]
    public TranslationList NameTranslations { get; set; }

    [JsonPropertyName("description")]
    public TranslationList DescriptionTranslations { get; set; }

    [JsonPropertyName("items_given")]
    public RotatableConfig<JsonAssetReference<ItemAsset>>[] ItemsGiven { get; set; }

    [JsonPropertyName("icon")]
    public RotatableConfig<string> Icon { get; set; }

    [JsonPropertyName("request_cooldown")]
    public RotatableConfig<float> Cooldown { get; set; }

    [JsonPropertyName("delays")]
    public Delay[] Delays { get; set; }

    [JsonPropertyName("effect_duration")]
    public float EffectDuration { get; set; }

    [JsonPropertyName("tick_speed")]
    public float TickSpeed { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    public bool CanClassUse(Class @class)
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
    public const string FormatName = "n";
    [FormatDisplay(typeof(Trait), "Type Name")]
    [FormatDisplay("Type Name")]
    public const string FormatTypeName = "t";
    [FormatDisplay(typeof(Trait), "Colored Type Name")]
    [FormatDisplay("Colored Type Name")]
    public const string FormatColorTypeName = "ct";
    [FormatDisplay(typeof(Trait), "Description")]
    [FormatDisplay("Description")]
    public const string FormatDescription = "d";
    [FormatDisplay(typeof(Trait), "Colored Name")]
    [FormatDisplay("Colored Name")]
    public const string FormatColorName = "cn";
    [FormatDisplay(typeof(Trait), "Colored Description")]
    [FormatDisplay("Colored Description")]
    public const string FormatColorDescription = "cd";
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null && !format.Equals(FormatName, StringComparison.Ordinal))
        {
            if (format.Equals(FormatTypeName, StringComparison.Ordinal))
                return TypeName;
            if (format.Equals(FormatDescription, StringComparison.Ordinal))
                return DescriptionTranslations != null
                    ? DescriptionTranslations.Translate(language, string.Empty).Replace('\n', ' ')
                    : Translation.Null(flags & TranslationFlags.NoRichText);
            if (format.Equals(FormatColorTypeName, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), TypeName, flags);
            if (format.Equals(FormatColorName, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), NameTranslations != null
                    ? NameTranslations.Translate(language, TypeName).Replace('\n', ' ')
                    : TypeName, flags);
            if (format.Equals(FormatColorDescription, StringComparison.Ordinal))
                return Localization.Colorize(TeamManager.GetTeamHexColor(Team), DescriptionTranslations != null
                    ? DescriptionTranslations.Translate(language, string.Empty).Replace('\n', ' ')
                    : Translation.Null(flags & TranslationFlags.NoRichText), flags);
        }
        return NameTranslations != null
            ? NameTranslations.Translate(language, TypeName).Replace('\n', ' ')
            : TypeName;
    }
}