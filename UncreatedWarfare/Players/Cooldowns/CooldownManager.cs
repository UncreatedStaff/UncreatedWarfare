using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Cooldowns;

public sealed class CooldownManager : BaseAlternateConfigurationFile, ILayoutHostedService
{
    private const string KnownTypes = "Uncreated.Warfare.Players.Cooldowns.KnownCooldowns";

    private CooldownTypeConfiguration[] _cooldowns;

    private int _fobPlayersMin;
    private int _fobPlayersMax;
    private float _fobCooldownMin;
    private float _fobCooldownMax;
    private float _fobCooldownAlpha;

    private readonly IPlayerService _playerService;
    private readonly PlayerDictionary<PlayerCooldownList> _activeCooldowns;

    /// <summary>
    /// All available cooldown types.
    /// </summary>
    public IReadOnlyList<CooldownTypeConfiguration> Cooldowns { get; private set; }

    public CooldownManager(IPlayerService playerService) : base(Path.Combine("Cooldowns", "Cooldowns.yml"))
    {
        _playerService = playerService;

        _activeCooldowns = new PlayerDictionary<PlayerCooldownList>();

        Cooldowns = null!;
        _cooldowns = null!;
        HandleChange();
    }

    protected override void HandleChange()
    {
        List<CooldownTypeConfiguration> configs = new List<CooldownTypeConfiguration>();
        foreach (IConfigurationSection section in UnderlyingConfiguration.GetChildren())
        {
            CooldownTypeConfiguration configuration = new CooldownTypeConfiguration(section.Key);
            section.Bind(configuration);
            configs.Add(configuration);

            if (!section.Key.Equals(KnownCooldowns.DeployFOB, StringComparison.OrdinalIgnoreCase))
                continue;

            _fobPlayersMin    = section.GetValue("MinimumPlayers", 24);
            _fobPlayersMax    = section.GetValue("MaximumPlayers", 60);
            _fobCooldownMin   = section.GetValue("MinimumCooldown", 60f);
            _fobCooldownMax   = section.GetValue("MaximumCooldown", 90f);
            _fobCooldownAlpha = section.GetValue("Alpha", 2f);
        }

        _cooldowns = configs.ToArray();
        Cooldowns = new ReadOnlyCollection<CooldownTypeConfiguration>(_cooldowns);
    }

    public CooldownTypeConfiguration? FindConfiguration(string type)
    {
        foreach (CooldownTypeConfiguration config in _cooldowns)
        {
            if (config.Type.Equals(type, StringComparison.Ordinal))
                return config;
        }

        return null;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        lock (_activeCooldowns)
        {
            List<CSteamID>? toRemove = null;
            foreach (PlayerCooldownList list in _activeCooldowns.Values.ToList())
            {
                list.ClearExpiredCooldowns();
                list.ClearResettableCooldowns();
                if (list.Count == 0 && !_playerService.IsPlayerOnlineThreadSafe(list.Player))
                {
                    (toRemove ??= new List<CSteamID>(4)).Add(list.Player);
                }
            }

            if (toRemove != null)
            {
                foreach (CSteamID id in toRemove)
                {
                    _activeCooldowns.Remove(id);
                }
            }
        }
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;

    /// <summary>
    /// Start a cooldown on a player with a configured duration.
    /// </summary>
    /// <exception cref="CooldownNotFoundException"/>
    public void StartCooldown(WarfarePlayer player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        StartCooldown(player.Steam64, type, data);
    }

    /// <summary>
    /// Start a cooldown on a player with a custom duration.
    /// </summary>
    public void StartCooldown(WarfarePlayer player, [ValueProvider(KnownTypes)] string type, TimeSpan duration, object? data = null)
    {
        StartCooldown(player.Steam64, type, duration, data);
    }

    /// <summary>
    /// Start a cooldown on a player with a configured duration.
    /// </summary>
    /// <exception cref="CooldownNotFoundException"/>
    public void StartCooldown(CSteamID player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        CooldownTypeConfiguration? config = FindConfiguration(type);
        if (config == null)
            throw new CooldownNotFoundException(type);

        StartCooldown(player, config, config.Duration, data);
    }

    /// <summary>
    /// Start a cooldown on a player with a custom duration.
    /// </summary>
    public void StartCooldown(CSteamID player, [ValueProvider(KnownTypes)] string type, TimeSpan duration, object? data = null)
    {
        CooldownTypeConfiguration config = FindConfiguration(type) ?? new CooldownTypeConfiguration(type) { Duration = duration };

        StartCooldown(player, config, duration, data);
    }

    /// <summary>
    /// Start a cooldown on a player with custom information.
    /// </summary>
    public void StartCooldown(WarfarePlayer player, Cooldown cooldown)
    {
        StartCooldown(player.Steam64, cooldown);
    }

    /// <summary>
    /// Start a cooldown on a player with custom information.
    /// </summary>
    public void StartCooldown(CSteamID player, Cooldown cooldown)
    {
        if (cooldown.Config == null)
            throw new ArgumentException("Argument missing cooldown config.", nameof(cooldown));

        if (cooldown.Duration.Ticks <= 0f || player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return;

        lock (_activeCooldowns)
        {
            if (_activeCooldowns.TryGetValue(player, out PlayerCooldownList? list))
            {
                list.ClearExpiredCooldowns();
                list.AddCooldown(cooldown);
            }
            else
            {
                list = new PlayerCooldownList(player, this);
                list.AddCooldown(cooldown);
                _activeCooldowns.Add(player, list);
            }
        }
    }

    private void StartCooldown(CSteamID player, CooldownTypeConfiguration config, TimeSpan duration, object? data)
    {
        if (duration.Ticks <= 0f || player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            return;

        lock (_activeCooldowns)
        {
            Cooldown cooldown = new Cooldown(DateTime.UtcNow, duration, config, data);
            if (_activeCooldowns.TryGetValue(player, out PlayerCooldownList? list))
            {
                list.ClearExpiredCooldowns();
                list.AddCooldown(cooldown);
            }
            else
            {
                list = new PlayerCooldownList(player, this);
                list.AddCooldown(cooldown);
                _activeCooldowns.Add(player, list);
            }
        }
    }

    /// <summary>
    /// Check if a player has an active cooldown.
    /// </summary>
    public bool HasCooldown(WarfarePlayer player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        return HasCooldown(player.Steam64, type, out _, data);
    }

    /// <summary>
    /// Check if a player has an active cooldown.
    /// </summary>
    public bool HasCooldown(WarfarePlayer player, [ValueProvider(KnownTypes)] string type, out Cooldown cooldown, object? data = null)
    {
        return HasCooldown(player.Steam64, type, out cooldown, data);
    }

    /// <summary>
    /// Check if a player has an active cooldown.
    /// </summary>
    public bool HasCooldown(CSteamID player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        return HasCooldown(player, type, out _, data);
    }

    /// <summary>
    /// Check if a player has an active cooldown.
    /// </summary>
    public bool HasCooldown(CSteamID player, [ValueProvider(KnownTypes)] string type, out Cooldown cooldown, object? data = null)
    {
        CooldownTypeConfiguration? config = FindConfiguration(type);
        if (config == null)
        {
            cooldown = default;
            return false;
        }

        lock (_activeCooldowns)
        {
            if (!_activeCooldowns.TryGetValue(player, out PlayerCooldownList? list))
            {
                cooldown = default;
                return false;
            }

            list.ClearExpiredCooldowns();
            cooldown = list.GetCooldown(type, data);
            if (cooldown.Config != null)
                return true;

            if (list.Count == 0 && !_playerService.IsPlayerOnlineThreadSafe(player))
                _activeCooldowns.Remove(player);

            return false;
        }
    }

    /// <summary>
    /// Remove all cooldowns of a type for the given player.
    /// </summary>
    public void RemoveCooldown(WarfarePlayer player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        RemoveCooldown(player.Steam64, type, data);
    }

    /// <summary>
    /// Remove all cooldowns of a type for the given player.
    /// </summary>
    public void RemoveCooldown(CSteamID player, [ValueProvider(KnownTypes)] string type, object? data = null)
    {
        CooldownTypeConfiguration? config = FindConfiguration(type);
        if (config == null)
            return;

        lock (_activeCooldowns)
        {
            if (!_activeCooldowns.TryGetValue(player, out PlayerCooldownList? list))
                return;

            list.ClearExpiredCooldowns();
            list.RemoveCooldown(type, data);
            if (list.Count == 0 && !_playerService.IsPlayerOnlineThreadSafe(player))
                _activeCooldowns.Remove(player);
        }
    }

    /// <summary>
    /// Remove all cooldowns for the given player.
    /// </summary>
    public void RemoveCooldown(WarfarePlayer player)
    {
        RemoveCooldown(player.Steam64);
    }

    /// <summary>
    /// Remove all cooldowns for the given player.
    /// </summary>
    public void RemoveCooldown(CSteamID player)
    {
        lock (_activeCooldowns)
        {
            _activeCooldowns.Remove(player);
        }
    }

    /// <summary>
    /// Remove all cooldowns of the given type.
    /// </summary>
    public void RemoveCooldown([ValueProvider(KnownTypes)] string type, object? data = null)
    {
        CooldownTypeConfiguration? config = FindConfiguration(type);
        if (config == null)
            return;

        lock (_activeCooldowns)
        {
            List<CSteamID>? toRemove = null;
            foreach (PlayerCooldownList list in _activeCooldowns.Values)
            {
                list.ClearExpiredCooldowns();
                list.RemoveCooldown(type, data);
                if (list.Count == 0 && !_playerService.IsPlayerOnlineThreadSafe(list.Player))
                    (toRemove ??= new List<CSteamID>(4)).Add(list.Player);
            }

            if (toRemove == null)
                return;

            foreach (CSteamID id in toRemove)
            {
                _activeCooldowns.Remove(id);
            }
        }
    }

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public float GetFOBDeployCooldown() => GetFOBDeployCooldown(_playerService.OnlinePlayers.Count(x => x.Team.IsValid));

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public float GetFOBDeployCooldown(int players)
    {
        players = Mathf.Clamp(players, _fobPlayersMin, _fobPlayersMax);

        float a = _fobCooldownAlpha;
        if (a == 0f)
            a = 2f;

        // (LaTeX)
        // base function: f\left(x\right)=\left(1-\left(1-t\right)^{a}\right)
        // scaled: \left(C_{2}-C_{1}\right)f\left(\left(x-P_{1}\right)\frac{1}{\left(P_{2}-P_{1}\right)}\right)+C_{1}

        return _fobCooldownMin +
               (_fobCooldownMax - _fobCooldownMin) *
               (1f - Mathf.Pow(1 -
                   /* t = */ (players - _fobPlayersMin) * (1f / (_fobPlayersMax - _fobPlayersMin))
                   , a)
               );
    }
}