﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events.Players;
public class PlayerDied : PlayerEvent
{
    public EDeathCause Cause { get; internal set; }
    public ELimb Limb { get; internal set; }
    public UCPlayer? Killer { get; internal set; }
    public CSteamID Instigator { get; internal set; }
    public bool WasTeamkill { get; internal set; }
    public ulong DeadTeam { get; internal set; }
    public ulong KillerTeam { get; internal set; }

    public PlayerDied(UCPlayer player) : base(player)
    {
    }
}
