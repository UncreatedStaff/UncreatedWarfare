using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Players.Components;

[PlayerComponent]
public class FirstLifeComponent : IPlayerComponent,
    IEventListener<PlayerDied>
{
    public WarfarePlayer Player { get; set; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        if (!isOnJoin)
        {
            // reset IsFirstLife if this method is running because a new layout has started
            Player.Save.IsFirstLife = true;
            return;
        }
        
        Layout layout = serviceProvider.GetRequiredService<Layout>();
        bool isJoiningNewGame = layout.LayoutId != Player.Save.LastGameId;
        
        // reset IsFirstLife because a player joined into a new game
        if (isJoiningNewGame)
            Player.Save.IsFirstLife = true;
    }

    public void HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        Player.Save.IsFirstLife = false;
    }
}