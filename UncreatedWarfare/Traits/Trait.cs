using SDG.Unturned;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Traits;
public abstract class Trait : MonoBehaviour
{
    private TraitData _data;
    private UCPlayer _targetPlayer;
    private bool _inited = false;
    protected Coroutine? _coroutine;
    public TraitData Data { get => _data; internal set => _data = value; }
    public UCPlayer TargetPlayer => _targetPlayer;
    public bool Inited => _inited;
    public virtual void Init(TraitData data, UCPlayer target)
    {
        _data = data;
        _targetPlayer = target;
        _inited = true;
    }

    private void Start()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + this.GetType().Name + " was not initialized. You must run Trait.Init(...) within the same frame of creating it.");

        TraitManager.ActivateTrait(this);
        OnActivate();
        if (Data.EffectDuration > 0f)
        {
            _coroutine = StartCoroutine(EffectCoroutine());
        }
        else
            Destroy(this);
    }
    private void OnDestroy()
    {
        if (!_inited)
            throw new InvalidOperationException("Trait " + this.GetType().Name + " was not initialized.");

        TraitManager.DeactivateTrait(this);
        OnDeactivate();
    }
    protected virtual void OnActivate()
    {
        GiveItems();
    }
    protected virtual void OnDeactivate() { }
    /// <summary>Only called if <see cref="TraitData.TickSpeed"/> is > 0.</summary>
    protected virtual void Tick(float activeTime) { }
    private void GiveItems()
    {
        if (Data.ItemsGiven is null || Data.ItemsGiven.Length > 0)
            return;

        if (!Data.DistributedToSquad || _targetPlayer.Squad is null || (Data.SquadLeaderRequired && _targetPlayer.Squad.Leader.Steam64 != _targetPlayer.Steam64))
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
    private IEnumerator EffectCoroutine()
    {
        if (Data.TickSpeed > 0f)
        {
            int ticks = Mathf.CeilToInt(Data.EffectDuration / Data.TickSpeed);
            float seconds = 0f;
            for (int i = 0; i < ticks; ++i)
            {
                yield return new WaitForSecondsRealtime(Data.TickSpeed);
                seconds += Data.TickSpeed;
                Tick(seconds);
            }
        }
        else
            yield return new WaitForSecondsRealtime(Data.EffectDuration);

        _coroutine = null;
        Destroy(this);
    }
}

public class TraitData
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

    [JsonPropertyName("credit_cost")]
    public int CreditCost { get; set; }

    [JsonPropertyName("squad_distribute")]
    public bool DistributedToSquad { get; set; }

    [JsonPropertyName("squad_leader_required")]
    public bool SquadLeaderRequired { get; set; }

    [JsonPropertyName("gamemode_list_is_blacklist")]
    public bool GamemodeListIsBlacklist { get; set; }

    [JsonPropertyName("gamemode_list")]
    public string[] GamemodeList { get; set; }

    [JsonPropertyName("class_list_is_blacklist")]
    public bool ClassListIsBlacklist { get; set; }

    [JsonPropertyName("class_list")]
    public EClass[] ClassList { get; set; }

    [JsonPropertyName("unlock_requirements")]
    public BaseUnlockRequirement[] UnlockRequirements { get; set; }

    [JsonPropertyName("name")]
    public TranslationList NameTranslations { get; set; }

    [JsonPropertyName("description")]
    public TranslationList DescriptionTranslations { get; set; }

    [JsonPropertyName("item_given")]
    public RotatableConfig<JsonAssetReference<ItemAsset>>[] ItemsGiven { get; set; }

    [JsonPropertyName("icon")]
    public RotatableConfig<string> Icon { get; set; }

    [JsonPropertyName("delays")]
    public Delay[] Delays { get; set; }

    [JsonPropertyName("effect_duration")]
    public float EffectDuration { get; set; }

    [JsonPropertyName("tick_speed")]
    public float TickSpeed { get; set; }

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
        string gm = Data.Gamemode is null ? throw new InvalidOperationException("There is not a loaded gamemode.") : Data.Gamemode.Name;
        if (GamemodeList is null) return true;
        for (int i = 0; i < GamemodeList.Length; ++i)
        {
            if (GamemodeList[i].Equals(gm, StringComparison.OrdinalIgnoreCase)) return !GamemodeListIsBlacklist;
        }
        return GamemodeListIsBlacklist;
    }
}