using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;

namespace Uncreated.Warfare.FreeTeamDeathmatch.Tweaks;

internal class FtdmNoInjuresTweak : IEventListener<InjurePlayerRequested>
{
    public void HandleEvent(InjurePlayerRequested e, IServiceProvider serviceProvider)
    {
        e.Cancel();
    }
}
