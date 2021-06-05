using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Squads
{
    public class RallyManager
    {

    }

    public class RallyPoint
    {
        BarricadeData structure; // physical barricade structure of the rallypoint
        List<UnturnedPlayer> AwaitingPlayers; // list of players currently waiting to teleport to the rally

        public RallyPoint(BarricadeData structure)
        {
            this.structure = structure;
            AwaitingPlayers = new List<UnturnedPlayer>();
        }
    }
}
