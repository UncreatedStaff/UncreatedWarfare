using System;

namespace Uncreated.Warfare.Players.Cooldowns;

public class CooldownTypeConfiguration
{
    public string Type { get; }

    public TimeSpan Duration { get; set; }

    public bool ResetOnGameEnd { get; set; } = true;

    public CooldownTypeConfiguration(string type)
    {
        Type = type;
    }
}