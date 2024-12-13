using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Moderation.Reports;

[PlayerComponent]
internal class PlayerReportComponent : IPlayerComponent
{
    public WarfarePlayer Player { get; private set; } = null!;

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {

    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
