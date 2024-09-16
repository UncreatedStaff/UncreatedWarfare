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
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Signs;

/// <summary>
/// Handles sending specific sign data to specific players.
/// </summary>
public class SignInstancer : IEventListener<BarricadePlaced>, IEventListener<BarricadeDestroyed>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignInstanceData[] _types;
    private readonly string?[] _batchBuffer;
    private readonly Dictionary<uint, ISignInstanceProvider> _signProviders = new Dictionary<uint, ISignInstanceProvider>(256);
    private readonly Dictionary<uint, int> _signProviderTypeIndexes = new Dictionary<uint, int>(256);
    private readonly ILogger<SignInstancer> _logger;
    private readonly ITranslationService _translationService;

    public SignInstancer(IServiceProvider serviceProvider, ILogger<SignInstancer> logger, ITranslationService translationService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _translationService = translationService;

        // find all ISignInstanceProvider's
        List<SignInstanceData> instances = new List<SignInstanceData>(8);
        foreach (Type signProviderType in Accessor.GetTypesSafe().Where(typeof(ISignInstanceProvider).IsAssignableFrom))
        {
            if (signProviderType.IsAbstract || !signProviderType.IsClass)
                return;

            foreach (SignPrefixAttribute prefix in signProviderType.GetAttributesSafe<SignPrefixAttribute>())
            {
                if (string.IsNullOrEmpty(prefix.Prefix))
                    continue;

                SignInstanceData data = new SignInstanceData(signProviderType, prefix.Prefix);
                instances.Add(data);
            }
        }

        _types = instances.ToArray();
        _batchBuffer = new string[_types.Length];
    }

    public bool IsInstanced(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        return _signProviders.ContainsKey(drop.instanceID);
    }

    public ISignInstanceProvider? GetSignProvider(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        _signProviders.TryGetValue(drop.instanceID, out ISignInstanceProvider? provider);
        return provider;
    }

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

        return provider.Translate(language, culture, null);
    }

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

        return provider.Translate(player.Locale.LanguageInfo, player.Locale.CultureInfo, player);
    }

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

    public int UpdateSigns(WarfarePlayer player)
        => UpdateSigns(new LanguageSet(player));

    public int UpdateSigns<TProvider>(WarfarePlayer player) where TProvider : class, ISignInstanceProvider
        => UpdateSigns<TProvider>(new LanguageSet(player));

    public int UpdateSigns<TProvider>(WarfarePlayer player, Func<InteractableSign, TProvider, bool> selector) where TProvider : class, ISignInstanceProvider
        => UpdateSigns(new LanguageSet(player), selector);

    public int UpdateSigns(LanguageSet set)
    {
        GameThread.AssertCurrent();

        for (int i = 0; i < _batchBuffer.Length; ++i)
            _batchBuffer[i] = null;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider provider))
                continue;

            int dataIndex = _signProviderTypeIndexes[barricade.Drop.instanceID];
            ref SignInstanceData data = ref _types[dataIndex];

            BroadcastUpdate(set, provider, in barricade, sign, ref data.CanBatch, ref data.HasCanBatch, ref _batchBuffer[dataIndex]);
            ++ct;
        }

        return ct;
    }

    public int UpdateSigns<TProvider>(LanguageSet set) where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        string? batchTranslate = null;
        bool hasCanBatch = false, canBatch = false;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider provider) || provider is not TProvider)
                continue;

            BroadcastUpdate(set, provider, in barricade, sign, ref canBatch, ref hasCanBatch, ref batchTranslate);
            ++ct;
        }

        return ct;
    }

    public int UpdateSigns<TProvider>(LanguageSet set, Func<InteractableSign, TProvider, bool> selector) where TProvider : class, ISignInstanceProvider
    {
        GameThread.AssertCurrent();

        string? batchTranslate = null;
        bool hasCanBatch = false, canBatch = false;

        int ct = 0;
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is not InteractableSign sign)
                continue;

            if (!_signProviders.TryGetValue(barricade.Drop.instanceID, out ISignInstanceProvider p) || p is not TProvider provider || !selector(sign, provider))
                continue;

            BroadcastUpdate(set, p, in barricade, sign, ref canBatch, ref hasCanBatch, ref batchTranslate);
            ++ct;
        }

        return ct;
    }

    private static void BroadcastUpdate(LanguageSet set, ISignInstanceProvider provider, in BarricadeInfo barricade, InteractableSign sign, ref bool canBatch, ref bool hasCanBatch, ref string? batchTranslate)
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
            text = batchTranslate ??= provider.Translate(set.Language, set.Culture, null);
            Data.SendChangeText.Invoke(netId, ENetReliability.Unreliable, set.GatherTransportConnections(region.x, region.y, BarricadeManager.BARRICADE_REGIONS), text);
        }
        else while (set.MoveNext())
        {
            PlayerMovement movement = set.Next.UnturnedPlayer.movement;
            if (!Regions.checkArea(movement.region_x, movement.region_y, region.x, region.y, BarricadeManager.BARRICADE_REGIONS))
                continue;

            text = provider.Translate(set.Language, set.Culture, set.Next);
            Data.SendChangeText.Invoke(netId, ENetReliability.Unreliable, set.Next.Connection, text);
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
        _signProviderTypeIndexes.Remove(e.InstanceId);
        _signProviders.Remove(e.InstanceId);
    }

    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        if (e.Barricade.interactable is not InteractableSign sign)
            return;

        string text = sign.text;
        if (!TryGetProviderType(text, out int dataIndex))
        {
            return;
        }

        ref SignInstanceData data = ref _types[dataIndex];

        ISignInstanceProvider provider = (ISignInstanceProvider)ActivatorUtilities.CreateInstance(_serviceProvider, data.Type);
        _signProviders.Add(e.Barricade.instanceID, provider);
        _signProviderTypeIndexes.Add(e.Barricade.instanceID, dataIndex);

        string extraInfo = text.Length <= data.Prefix.Length + 1 || text[data.Prefix.Length] != '_' ? text.Substring(data.Prefix.Length) : text.Substring(data.Prefix.Length + 1);
        try
        {
            provider.Initialize(e.Barricade, extraInfo, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sign provider {0} for barricade {1} #{2}.", Accessor.Formatter.Format(data.Type), e.Barricade.asset.itemName, e.Barricade.instanceID);
        }
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