using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Kits.Whitelists;
public class WhitelistPlacementBuffer : IPlayerComponent
{
    public WarfarePlayer Player { get; private set; }

    public void Init(IServiceProvider serviceProvider)
    {

    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
