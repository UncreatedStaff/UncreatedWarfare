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
        _icon = icon;
        StartTime = Time.realtimeSinceStartup;
        _player = player;
    }
}
