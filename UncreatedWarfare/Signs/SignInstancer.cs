using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Signs;

/// <summary>
/// Handles sending specific sign data to specific players.
/// </summary>
public class SignInstancer : ILayoutHostedService,
    IEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<SignTextChanged>,
    IEventListener<PlayerLocaleUpdated>
{
    /// <summary>
    /// Signature for <see cref="UpdateSigns{TProvider,TState}(ref TState,WhereStateRef{TProvider,TState})"/> and <see cref="UpdateSigns{TProvider,TState}(WarfarePlayer,ref TState,WhereStateRef{TProvider,TState})"/>.
    /// </summary>
    public delegate bool WhereStateRef<in TProvider, TState>(InteractableSign sign, TProvider provider, ref TState state)
        where TProvider : BaseSignInstanceProvider;

    /// <summary>
    /// Signature for <see cref="UpdateSigns{TProvider,TState}(TState,WhereState{TProvider,TState})"/> and <see cref="UpdateSigns{TProvider,TState}(WarfarePlayer,TState,WhereState{TProvider,TState})"/>.
    /// </summary>
    public delegate bool WhereState<in TProvider, in TState>(InteractableSign sign, TProvider provider, TState state)
        where TProvider : BaseSignInstanceProvider;

    private static readonly ClientInstanceMethod<string> SendChangeText = ReflectionUtility.FindRequiredRpc<InteractableSign, ClientInstanceMethod<string>>("SendChangeText");

    private const int MaxSignUpdatesPerFrame = 3;

    private readonly WarfareModule _warfare;
    private IServiceProvider? _serviceProvider;
    private readonly SignInstanceData[] _types;
    private readonly Dictionary<uint, BaseSignInstanceProvider> _signProviders = new Dictionary<uint, BaseSignInstanceProvider>(256);
    // private readonly Dictionary<uint, int> _signProviderTypeIndexes = new Dictionary<uint, int>(256);
    private readonly ILogger<SignInstancer> _logger;
    private readonly ITranslationService _translationService;
    private readonly IPlayerService _playerService;

    private readonly List<HashSet<ulong>> _playerListPool = new List<HashSet<ulong>>(64);
    private readonly Queue<BaseSignInstanceProvider> _updateQueue = new Queue<BaseSignInstanceProvider>();

    public SignInstancer(WarfareModule module, ILogger<SignInstancer> logger, ITranslationService translationService, IPlayerService playerService)
    {
        _warfare = module;
        _logger = logger;
        _translationService = translationService;
        _playerService = playerService;

        // find all BaseSignInstanceProvider's
        List<SignInstanceData> instances = new List<SignInstanceData>(8);
        foreach (Type signProviderType in Accessor.GetTypesSafe().Where(typeof(BaseSignInstanceProvider).IsAssignableFrom))
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

        TimeUtility.updated += OnUpdate;
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        ResetAllSigns(changeText: false);
        _serviceProvider = null;
        TimeUtility.updated -= OnUpdate;
        return UniTask.CompletedTask;
    }

    private HashSet<ulong> EnqueueUpdate(BaseSignInstanceProvider provider)
    {
        if (provider.IsQueued)
            return provider.Players!;

        if (provider.Players == null)
        {
            if (_playerListPool.Count > 0)
            {
                provider.Players = _playerListPool[^1];
                _playerListPool.RemoveAt(_playerListPool.Count - 1);
            }
            else
            {
                provider.Players = new HashSet<ulong>(32);
            }
        }

        provider.IsQueued = true;
        _updateQueue.Enqueue(provider);
        return provider.Players;
    }

    private void ApplyDequeue(BaseSignInstanceProvider provider)
    {
        provider.IsQueued = false;
        provider.IsQueuedAllPlayers = false;
        HashSet<ulong>? oldPlayers = provider.Players;
        provider.Players = null;
        if (oldPlayers != null)
        {
            oldPlayers.Clear();
            _playerListPool.Add(oldPlayers);
        }
    }

    private void MarkAllPlayers(BaseSignInstanceProvider provider)
    {
        provider.IsQueuedAllPlayers = true;
        provider.Players?.Clear();
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
    public BaseSignInstanceProvider? GetSignProvider(BarricadeDrop drop)
    {
        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        _signProviders.TryGetValue(drop.instanceID, out BaseSignInstanceProvider? provider);
        return provider;
    }

    /// <summary>
    /// Get the text that would show on a sign for any player with the given <paramref name="language"/> and <paramref name="culture"/>.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public string GetSignText(BarricadeDrop drop, LanguageInfo language, CultureInfo culture)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        uint instanceId = drop.instanceID;

        if (!_signProviders.TryGetValue(instanceId, out BaseSignInstanceProvider? provider))
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
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        if (drop.interactable is not InteractableSign sign)
            throw new ArgumentException("Barricade must be sign.", nameof(drop));

        uint instanceId = drop.instanceID;

        if (!_signProviders.TryGetValue(instanceId, out BaseSignInstanceProvider provider))
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
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            EnqueueUpdate(p.Value);
            MarkAllPlayers(p.Value);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>() where TProvider : BaseSignInstanceProvider
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            EnqueueUpdate(provider);
            MarkAllPlayers(provider);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(Func<InteractableSign, TProvider, bool> selector) where TProvider : BaseSignInstanceProvider
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider))
                continue;

            EnqueueUpdate(provider);
            MarkAllPlayers(provider);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>(TState state, WhereState<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, state))
                continue;

            EnqueueUpdate(provider);
            MarkAllPlayers(provider);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>(ref TState state, WhereStateRef<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, ref state))
                continue;

            EnqueueUpdate(provider);
            MarkAllPlayers(provider);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns(WarfarePlayer player)
    {
        if (!player.IsOnline)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            HashSet<ulong> set = EnqueueUpdate(p.Value);
            set.Add(player.Steam64.m_SteamID);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(WarfarePlayer player) where TProvider : BaseSignInstanceProvider
    {
        if (!player.IsOnline)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            set.Add(player.Steam64.m_SteamID);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>(WarfarePlayer player, Func<InteractableSign, TProvider, bool> selector) where TProvider : BaseSignInstanceProvider
    {
        if (!player.IsOnline)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            set.Add(player.Steam64.m_SteamID);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>(WarfarePlayer player, TState state, WhereState<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        if (!player.IsOnline)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, state))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            set.Add(player.Steam64.m_SteamID);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>(WarfarePlayer player, ref TState state, WhereStateRef<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        if (!player.IsOnline)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, ref state))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            set.Add(player.Steam64.m_SteamID);
            ++ct;
        }

        return ct;
    }

    /// <summary>
    /// Update all signs for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns([HandlesResourceDisposal] LanguageSetEnumerator players)
    {
        if (players.IsEmpty)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            HashSet<ulong> set = EnqueueUpdate(p.Value);
            AddLangSetToHashSet(set, ref players);
            ++ct;
        }

        players.Dispose();
        return ct;
    }

    /// <summary>
    /// Update all signs with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>([HandlesResourceDisposal] LanguageSetEnumerator players) where TProvider : BaseSignInstanceProvider
    {
        if (players.IsEmpty)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            AddLangSetToHashSet(set, ref players);
            ++ct;
        }
        
        players.Dispose();
        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider>([HandlesResourceDisposal] LanguageSetEnumerator players, Func<InteractableSign, TProvider, bool> selector) where TProvider : BaseSignInstanceProvider
    {
        if (players.IsEmpty)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            AddLangSetToHashSet(set, ref players);
            ++ct;
        }

        players.Dispose();
        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>([HandlesResourceDisposal] LanguageSetEnumerator players, TState state, WhereState<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        if (players.IsEmpty)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, state))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            AddLangSetToHashSet(set, ref players);
            ++ct;
        }

        players.Dispose();
        return ct;
    }

    /// <summary>
    /// Update all signs that match a predicate with a certain provider type for a player.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public int UpdateSigns<TProvider, TState>([HandlesResourceDisposal] LanguageSetEnumerator players, ref TState state, WhereStateRef<TProvider, TState> selector) where TProvider : BaseSignInstanceProvider
    {
        if (players.IsEmpty)
            return 0;

        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        foreach (KeyValuePair<uint, BaseSignInstanceProvider> p in _signProviders)
        {
            if (p.Value is not TProvider provider)
                continue;

            InteractableSign sign = (InteractableSign)provider.Barricade.interactable;
            if (!selector(sign, provider, ref state))
                continue;

            HashSet<ulong> set = EnqueueUpdate(provider);
            AddLangSetToHashSet(set, ref players);
            ++ct;
        }

        players.Dispose();
        return ct;
    }

    private void OnUpdate()
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        for (int i = 0; i < MaxSignUpdatesPerFrame && _updateQueue.TryDequeue(out BaseSignInstanceProvider next); ++i)
        {
            BarricadeDrop barricade = next.Barricade;

            if (next.IsQueuedAllPlayers)
            {
                UpdateSignImmediate(barricade, next);
            }
            else if (next.Players is { Count: > 0 } players)
            {
                UpdateSignImmediateForPlayers(barricade, next, players);
            }

            ApplyDequeue(next);
        }
    }

    private void UpdateSignImmediateForPlayers(BarricadeDrop barricade, BaseSignInstanceProvider provider, HashSet<ulong> players)
    {
        if (!BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out _))
            return;

        NetId netId = barricade.interactable.GetNetId();

        TranslationOptions groupOptions = provider.GroupOptions;
        if ((groupOptions & TranslationOptions.PerPlayerTranslation) == 0)
        {
            foreach (LanguageSet set in _translationService.SetOf.PlayersIn(players, groupOptions))
            {
                string text = _serviceProvider == null
                    ? provider.FallbackText
                    : provider.Translate(_translationService.ValueFormatter, _serviceProvider, set.Language, set.Culture, null);

                SendChangeText.Invoke(
                    netId,
                    ENetReliability.Unreliable,
                    plant != ushort.MaxValue
                        ? set.GatherTransportConnections()
                        : set.GatherTransportConnections(x, y, BarricadeManager.BARRICADE_REGIONS),
                    text
                );
            }
        }
        else
        {
            foreach (ulong pl in players)
            {
                WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(pl);
                if (player == null)
                    continue;

                if (plant != ushort.MaxValue
                    && !Regions.checkArea(player.UnturnedPlayer.movement.region_x, player.UnturnedPlayer.movement.region_y, x, y, BarricadeManager.BARRICADE_REGIONS))
                {
                    continue;
                }

                string text = _serviceProvider == null
                    ? provider.FallbackText
                    : provider.Translate(_translationService.ValueFormatter, _serviceProvider, player.Locale.LanguageInfo, player.Locale.CultureInfo, player);

                SendChangeText.Invoke(netId, ENetReliability.Unreliable, player.Connection, text);
            }
        }
    }

    /// <summary>
    /// Update a specific sign instantly.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public void UpdateSignImmediate(BarricadeDrop barricade)
    {
        BaseSignInstanceProvider? provider = GetSignProvider(barricade);
        if (provider == null)
        {
            CheckBarricadeForInit(barricade);
            provider = GetSignProvider(barricade);

            if (provider == null)
                return;
        }

        UpdateSignImmediate(barricade, provider);
    }

    private void UpdateSignImmediate(BarricadeDrop barricade, BaseSignInstanceProvider provider)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        if (barricade.interactable is not InteractableSign sign || !BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out _))
            return;

        NetId netId = sign.GetNetId();
        TranslationOptions groupOptions = provider.GroupOptions;
        if ((groupOptions & TranslationOptions.PerPlayerTranslation) == 0)
        {
            LanguageSetEnumerator enumerator = plant == ushort.MaxValue
                ? _translationService.SetOf.PlayersInArea(x, y, BarricadeManager.BARRICADE_REGIONS, groupOptions)
                : _translationService.SetOf.AllPlayers(groupOptions);

            foreach (LanguageSet set in enumerator)
            {
                string text = _serviceProvider == null
                    ? provider.FallbackText
                    : provider.Translate(_translationService.ValueFormatter, _serviceProvider, set.Language, set.Culture, null);

                SendChangeText.Invoke(
                    netId,
                    ENetReliability.Unreliable,
                    plant != ushort.MaxValue
                        ? set.GatherTransportConnections()
                        : set.GatherTransportConnections(x, y, BarricadeManager.BARRICADE_REGIONS),
                    text
                );
            }
        }
        else
        {
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                if (plant != ushort.MaxValue)
                {
                    PlayerMovement move = player.UnturnedPlayer.movement;
                    if (!Regions.checkArea(move.region_x, move.region_y, x, y, BarricadeManager.BARRICADE_REGIONS))
                        continue;
                }

                WarfarePlayerLocale locale = player.Locale;
                string text = _serviceProvider == null
                    ? provider.FallbackText
                    : provider.Translate(_translationService.ValueFormatter, _serviceProvider, locale.LanguageInfo, locale.CultureInfo, player);

                SendChangeText.Invoke(netId, ENetReliability.Unreliable, player.Connection, text);
            }
        }
    }

    private void ResetAllSigns(bool changeText = true)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

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
        // _signProviderTypeIndexes.Clear();
    }

    private bool TryGetProviderType(string text, out int index)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

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
    void IEventListener<PlayerLocaleUpdated>.HandleEvent(PlayerLocaleUpdated e, IServiceProvider serviceProvider)
    {
        UpdateSigns(e.Player);
    }
    private void RemoveBarricade(BarricadeDrop barricade)
    {
        // _signProviderTypeIndexes.Remove(barricade.instanceID);
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

        using IDisposable? profiler = ProfilerUtil.Profile();

        ref SignInstanceData data = ref _types[dataIndex];

        if (remove)
        {
            RemoveBarricade(barricade);
        }
        else if (_signProviders.ContainsKey(barricade.instanceID))
        {
            return;
        }

        BaseSignInstanceProvider provider = (BaseSignInstanceProvider)ReflectionUtility.CreateInstanceFixed(_serviceProvider, data.Type, [ barricade ]);
        _signProviders.Add(barricade.instanceID, provider);
        // _signProviderTypeIndexes.Add(barricade.instanceID, dataIndex);

        string extraInfo = text.Length <= data.Prefix.Length + 1 || text[data.Prefix.Length] != '_' ? text.Substring(data.Prefix.Length) : text.Substring(data.Prefix.Length + 1);
        try
        {
            provider.Initialize(barricade, extraInfo, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sign provider {0} for barricade {1} #{2}.", data.Type, barricade.asset.itemName, barricade.instanceID);
        }

        UpdateSignImmediate(barricade);
    }

    private struct SignInstanceData
    {
        public readonly Type Type;
        public readonly string Prefix;
        public SignInstanceData(Type type, string prefix)
        {
            Type = type;
            Prefix = prefix;
        }
    }

    private static void AddLangSetToHashSet(HashSet<ulong> set, ref LanguageSetEnumerator players)
    {
        while (players.MoveNext())
        {
            while (players.Set.MoveNext())
            {
                set.Add(players.Set.Next.Steam64.m_SteamID);
            }
        }

        players.Reset();
    }
}