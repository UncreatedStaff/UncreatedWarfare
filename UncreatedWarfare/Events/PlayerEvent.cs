﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events;
public class PlayerEvent : EventState
{
    private readonly UCPlayer _player;
    public UCPlayer Player => _player;
    public ulong Steam64 => _player.Steam64;
    public PlayerEvent(UCPlayer player)
    {
        _player = player;
    }
}