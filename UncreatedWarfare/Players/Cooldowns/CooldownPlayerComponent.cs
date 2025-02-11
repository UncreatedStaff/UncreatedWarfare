using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Players.Cooldowns;

public class PlayerCooldownList
{
    private readonly List<Cooldown> _cooldowns;

    public CSteamID Player { get; }

    public int Count
    {
        get
        {
            lock (_cooldowns)
            {
                return _cooldowns.Count;
            }
        }
    }

    public PlayerCooldownList(CSteamID player, CooldownManager cooldownManager)
    {
        _cooldowns = new List<Cooldown>(cooldownManager.Cooldowns.Count);
        Player = player;
    }

    public Cooldown GetCooldown(string type, object? data)
    {
        lock (_cooldowns)
        {
            for (int i = 0; i < _cooldowns.Count; i++)
            {
                Cooldown c = _cooldowns[i];
                if (c.TypeEquals(type, data))
                {
                    return c;
                }
            }

            return default;
        }
    }

    public void AddCooldown(Cooldown cooldown)
    {
        lock (_cooldowns)
        {
            for (int i = 0; i < _cooldowns.Count; i++)
            {
                Cooldown c = _cooldowns[i];
                if (c.TypeEquals(cooldown))
                {
                    _cooldowns[i] = cooldown;
                    break;
                }
            }

            _cooldowns.Add(cooldown);
        }
    }

    public void ClearCooldowns()
    {
        lock (_cooldowns)
        {
            _cooldowns.Clear();
        }
    }

    public void RemoveCooldown(string type, object? data)
    {
        lock (_cooldowns)
        {
            for (int i = _cooldowns.Count - 1; i >= 0; i--)
            {
                Cooldown c = _cooldowns[i];
                if (!c.TypeEquals(type, data))
                    continue;

                _cooldowns.RemoveAt(i);
            }
        }
    }

    public void RemoveCooldown(string type)
    {
        lock (_cooldowns)
        {
            for (int i = _cooldowns.Count - 1; i >= 0; i--)
            {
                Cooldown c = _cooldowns[i];
                if (!c.Config.Type.Equals(type, StringComparison.Ordinal))
                    continue;

                _cooldowns.RemoveAt(i);
            }
        }
    }

    public void ClearExpiredCooldowns()
    {
        lock (_cooldowns)
        {
            for (int i = _cooldowns.Count - 1; i >= 0; i--)
            {
                Cooldown c = _cooldowns[i];
                if (c.IsActive())
                    continue;

                _cooldowns.RemoveAt(i);
            }
        }
    }

    public void ClearResettableCooldowns()
    {
        lock (_cooldowns)
        {
            for (int i = _cooldowns.Count - 1; i >= 0; i--)
            {
                Cooldown c = _cooldowns[i];
                if (!c.Config.ResetOnGameEnd)
                    continue;

                _cooldowns.RemoveAt(i);
            }
        }
    }
}