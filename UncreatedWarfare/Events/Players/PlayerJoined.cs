namespace Uncreated.Warfare.Events.Players;
public class PlayerJoined : PlayerEvent
{
    private readonly PlayerSave? _save;
    public bool IsNewPlayer => SaveData is null;
    public PlayerSave? SaveData => _save;
    public PlayerJoined(UCPlayer player, PlayerSave? saveData) : base(player)
    {
        _save = saveData;
    }
}
