using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Signs;

/// <summary>
/// Handles sending specific sign data to specific players.
/// </summary>
public class SignInstancer : ILayoutHostedService, IEventListener<BarricadePlaced>, IEventListener<BarricadeDestroyed>, IEventListener<SignTextChanged>
{
    private static readonly ClientInstanceMethod<string> SendChangeText = ReflectionUtility.FindRequiredRpc<InteractableSign, ClientInstanceMethod<string>>("SendChangeText");

    private readonly WarfareModule _warfare;
    private IServiceProvider? _serviceProvider;
    private readonly SignInstanceData[] _types;
    private readonly Dictionary<uint, ISignInstanceProvider> _signProviders = new Dictionary<uint, ISignInstanceProvider>(256);
    private readonly Dictionary<uint, int> _signProviderTypeIndexes = new Dictionary<uint, int>(256);
    private readonly ILogger<SignInstancer> _logger;
    private readonly ITranslationService _translationService;

    public SignInstancer(WarfareModule module, ILogger<SignInstancer> logger, ITranslationService translationService)
    {
        _warfare = module;
        _logger = logger;
        _translationService = translationService;

        // find all ISignInstanceProvider's
        List<SignInstanceData> instances = new List<SignInstanceData>(8);
        foreach (Type signProviderType in Accessor.GetTypesSafe().Where(typeof(ISignInstanceProvider).IsAssignableFrom))
        {
            if (signProviderType.IsAbstract || !signProviderType.IsClass)
                continue;

            foreach (SignPrefixAttribute prefix in signProviderType.GetAttributesSafe<SignPrefixAttribute>())
            {
                if (string.IsNullOrEmpty(prefix.Prefix))
                    continue;

                SignInstanceData data = new SignInstanceData(signProviderType, prefix.Prefix);
                instances.Add(data);
            }
        }

        _types = instances.ToArray();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _serviceProvider = _warfare.ScopedProvider.Resolve<IServiceProvider>();
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            CheckBarricadeForInit(barricade.Drop);
        }

        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        ResetAllSigns(changeText: false);
        _serviceProvider = null;
        return UniTask.CompletedTask;
    }
    
    /// <summary>
    /// Check if a specific sign is being instanced.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsInstanced(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        return _signProviders.ContainsKey(drop.instanceID);
    }

    /// <summary>
    /// Get the sign provider for a specific sign.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public ISignInstanceProvider? GetSignProvider(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        _signProviders.TryGetValue(drop.instanceID, out ISignInstanceProvider? provider);
        return provider;
    }

    /// <summary>
    /// Get the text that would show on a sign for any player with the given <paramref name="language"/> and <paramref name="culture"/>.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public string GetSignText(BarricadeDrop drop, LanguageInfo language, CultureInfo culture)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        uint instanceId = drop.instanceID;

        if (!_signProviders.TryGetValue(instanceId, out ISignInstanceProvider? provider))
        {
            return sign.text;
        }

        return _serviceProvider == null
            ? provider.FallbackText
            : provider.Translate(_translationService.ValueFormatter, _serviceProvider, language, culture, null);
    }

    /// <summary>
    /// Get the text that would show on a sign for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public string GetSignText(BarricadeDrop drop, WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        uint instanceId = drop.instanceID;

        if (!_signProviders.TryGetValue(instanceId, out ISignInstanceProvider provider))
        {
            return sign.text;
        }

        return _serviceProvider == null
            ? provider.FallbackText
            : provider.Translate(_translationService.ValueFormatter, _serviceProvider, player.Locale.LanguageInfo, player.Locale.CultureInfo, player);
    }

    /// <summary>
    /// Update all signs.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns()
    {
        GameThread.AssertCurrent();

        int ct = 0;
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            ct += UpdateSigns(set);
        }

        return ct;
    }

    /// <summary>
    /// Update all signs with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>() where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        int ct = 0;
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            ct += UpdateSigns<TProvider>(set);
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(Func<InteractableSign, TProvider, bool> selector) where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        int ct = 0;
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            ct += UpdateSigns(set, selector);
        }

        return ct;
    }

    /// <summary>
    /// Update all signs for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns(WarfarePlayer player)
        => UpdateSigns(new LanguageSet(player));

    /// <summary>
    /// Update all signs with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(WarfarePlayer player) where TProvider : class, ISignInstanceProvider
        => UpdateSigns<TProvider>(new LanguageSet(player));

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(WarfarePlayer player, Func<InteractableSign, TProvider, bool> selector) where TProvider : class, ISignInstanceProvider
        => UpdateSigns(new LanguageSet(player), selector);

    /// <summary>
    /// Update all signs for a set of players.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns(LanguageSet set)
    {
        GameThread.AssertCurrent();

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider provider))
                continue;

            int dataIndex = _signProviderTypeIndexes[barricade.Drop.instanceID];
            ref SignInstanceData data = ref _types[dataIndex];

            string? batch = null;
            BroadcastUpdate(set, provider, in barricade, sign, ref data.CanBatch, ref data.HasCanBatch, ref batch);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs with a certain provider type for a set of players.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(LanguageSet set) where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        bool hasCanBatch = false, canBatch = false;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider provider) || provider is not TProvider)
                continue;

            string? batchTranslate = null;
            BroadcastUpdate(set, provider, in barricade, sign, ref canBatch, ref hasCanBatch, ref batchTranslate);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a set of players.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(LanguageSet set, Func<InteractableSign, TProvider, bool> selector) where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        bool hasCanBatch = false, canBatch = false;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider p) || p is not TProvider provider || !selector(sign, provider))
                continue;

            string? batchTranslate = null;
            BroadcastUpdate(set, p, in barricade, sign, ref canBatch, ref hasCanBatch, ref batchTranslate);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update a specific sign.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public void UpdateSign(BarricadeDrop barricade)
    {
        GameThread.AssertCurrent();

        if (barricade.interactable is not InteractableSign sign || !BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out _))
            return;

        // index isn't needed
        BarricadeInfo info = plant == ushort.MaxValue
            ? new BarricadeInfo(barricade, -1, new RegionCoord(x, y))
            : new BarricadeInfo(barricade, -1, plant);

        LanguageSetEnumerator enumerator = plant == ushort.MaxValue
            ? _translationService.SetOf.PlayersInArea(x, y, BarricadeManager.BARRICADE_REGIONS)
            : _translationService.SetOf.AllPlayers();

        ISignInstanceProvider? provider = GetSignProvider(barricade);
        if (provider == null)
        {
            CheckBarricadeForInit(barricade);
            provider = GetSignProvider(barricade);

            if (provider == null)
                return;
        }

        foreach (LanguageSet set in enumerator)
        {
            bool canBatch = true;
            bool hasCanBatch = false;
            string? batch = null;

            BroadcastUpdate(set, provider, in info, sign, ref canBatch, ref hasCanBatch, ref batch);
        }
    }

    private void ResetAllSigns(bool changeText = true)
    {
        GameThread.AssertCurrent();

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.ContainsKey(barricade.Drop.instanceID))
                continue;

            if (changeText)
                SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Unreliable, BarricadeManager.GatherClientConnections(barricade.Coord.x, barricade.Coord.y, barricade.Plant), sign.text);
        }

        _signProviders.Clear();
        _signProviderTypeIndexes.Clear();
    }

    private void BroadcastUpdate(LanguageSet set, ISignInstanceProvider provider, in BarricadeInfo barricade, InteractableSign sign, ref bool canBatch, ref bool hasCanBatch, ref string? batchTranslate)
    {
        NetId netId = sign.GetNetId();

        if (!hasCanBatch)
        {
            canBatch = provider.CanBatchTranslate;
            hasCanBatch = true;
        }

        RegionCoord region = barricade.Coord;
        string? text;
        if (canBatch)
        {
            text = batchTranslate ??= _serviceProvider == null
                ? provider.FallbackText
                : provider.Translate(_translationService.ValueFormatter, _serviceProvider, set.Language, set.Culture, null);

            SendChangeText.Invoke(
                netId,
                ENetReliability.Unreliable,
                barricade.Plant != ushort.MaxValue
                ? set.GatherTransportConnections()
                : set.GatherTransportConnections(region.x, region.y, BarricadeManager.BARRICADE_REGIONS),
                text
            );
        }
        else while (set.MoveNext())
        {
            PlayerMovement movement = set.Next.UnturnedPlayer.movement;
            if (barricade.Plant == ushort.MaxValue && !Regions.checkArea(movement.region_x, movement.region_y, region.x, region.y, BarricadeManager.BARRICADE_REGIONS))
                continue;

            text = _serviceProvider == null
                ? provider.FallbackText
                : provider.Translate(_translationService.ValueFormatter, _serviceProvider, set.Language, set.Culture, set.Next);

            SendChangeText.Invoke(netId, ENetReliability.Unreliable, set.Next.Connection, text);
        }
    }

    private bool TryGetProviderType(string text, out int index)
    {
        for (int i = 0; i < _types.Length; ++i)
        {
            ref SignInstanceData data = ref _types[i];
            if (!text.StartsWith(data.Prefix))
            {
                continue;
            }

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    void IEventListener<BarricadeDestroyed>.HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        RemoveBarricade(e.Barricade);
    }

    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        CheckBarricadeForInit(e.Barricade);
    }

    void IEventListener<SignTextChanged>.HandleEvent(SignTextChanged e, IServiceProvider serviceProvider)
    {
        CheckBarricadeForInit(e.Barricade, remove: true);
    }

    private void RemoveBarricade(BarricadeDrop barricade)
    {
        _signProviderTypeIndexes.Remove(barricade.instanceID);
        _signProviders.Remove(barricade.instanceID);
    }

    private void CheckBarricadeForInit(BarricadeDrop barricade, bool remove = false)
    {
        if (barricade.interactable is not InteractableSign sign || _serviceProvider == null)
            return;

        string? text = sign.text;
        if (string.IsNullOrEmpty(text) || !TryGetProviderType(text, out int dataIndex))
        {
            return;
        }

        ref SignInstanceData data = ref _types[dataIndex];

        if (remove)
        {
            RemoveBarricade(barricade);
        }
        else if (_signProviders.ContainsKey(barricade.instanceID))
        {
            return;
        }

        ISignInstanceProvider provider = (ISignInstanceProvider)ActivatorUtilities.CreateInstance(_serviceProvider, data.Type);
        _signProviders.Add(barricade.instanceID, provider);
        _signProviderTypeIndexes.Add(barricade.instanceID, dataIndex);

        string extraInfo = text.Length <= data.Prefix.Length + 1 || text[data.Prefix.Length] != '_' ? text.Substring(data.Prefix.Length) : text.Substring(data.Prefix.Length + 1);
        try
        {
            provider.Initialize(barricade, extraInfo, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sign provider {0} for barricade {1} #{2}.", data.Type, barricade.asset.itemName, barricade.instanceID);
        }

        UpdateSign(barricade);
    }

    private struct SignInstanceData
    {
        public readonly Type Type;
        public readonly string Prefix;
        public bool CanBatch;
        public bool HasCanBatch;
        public SignInstanceData(Type type, string prefix)
        {
            Type = type;
            Prefix = prefix;
        }
    }
}