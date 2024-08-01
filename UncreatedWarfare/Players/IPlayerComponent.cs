using System;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Component auto-added to players on join and destroyed on disconnect.
/// </summary>
public interface IPlayerComponent
{
    WarfarePlayer Player { get; set; }
    void Init(IServiceProvider serviceProvider);
}