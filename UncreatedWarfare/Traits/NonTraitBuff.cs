using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Traits;
public abstract class NonTraitBuff : IBuff
{
    private readonly string _icon;
    private readonly UCPlayer _player;
    public float StartTime;
    public string Icon => _icon;
    public UCPlayer Player => _player;
    public bool IsBlinking { get; set; }
    bool IBuff.Reserved => false;
    public NonTraitBuff(string icon, UCPlayer player)
    {
        this._icon = icon;
        this.StartTime = Time.realtimeSinceStartup;
        this._player = player;
    }
}
