using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Traits;
#if false
public abstract class NonTraitBuff : IBuff
{
    private readonly string _icon;
    private readonly WarfarePlayer _player;
    public float StartTime;
    public string Icon => _icon;
    public WarfarePlayer Player => _player;
    public bool IsBlinking { get; set; }
    bool IBuff.Reserved => false;
    public NonTraitBuff(string icon, WarfarePlayer player)
    {
        _icon = icon;
        StartTime = Time.realtimeSinceStartup;
        _player = player;
    }
}
#endif