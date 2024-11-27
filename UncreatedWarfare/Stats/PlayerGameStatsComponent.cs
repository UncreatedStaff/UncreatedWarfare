using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Stats;

[PlayerComponent]
public class PlayerGameStatsComponent : IPlayerComponent
{
    private LeaderboardPhase? _phase;

    public double[] Stats { get; private set; } = Array.Empty<double>();
    public WarfarePlayer Player { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        Layout layout = serviceProvider.GetRequiredService<Layout>();

        _phase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault();
        Stats = _phase == null ? Array.Empty<double>() : new double[_phase.PlayerStats.Length];
    }

    public void AddToStat(string statName, int value)
    {
        AddToStat(statName, (double)value);
    }

    public void AddToStat(string statName, double value)
    {
        if (_phase == null)
            return;

        int index = _phase.GetStatIndex(statName);
        if (index < 0 || index >= Stats.Length)
            return;

        Stats[index] += value;
    }
    
    public void TryAddToStat(int index, int value)
    {
        TryAddToStat(index, (double)value);
    }

    public void TryAddToStat(int index, double value)
    {
        if (index >= Stats.Length)
            return;

        Stats[index] += value;
    }
    
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
