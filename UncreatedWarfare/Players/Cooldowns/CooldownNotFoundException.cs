using System;

namespace Uncreated.Warfare.Players.Cooldowns;

public class CooldownNotFoundException : Exception
{
    public CooldownNotFoundException(string type) : base($"Cooldown \"{type}\" not found in configuration.") { }
}