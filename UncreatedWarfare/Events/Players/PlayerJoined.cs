namespace Uncreated.Warfare.Events.Players;
public class PlayerJoined : PlayerEvent
{
    public bool IsNewPlayer { get; }
    public PlayerSave SaveData { get; }
    public PlayerJoined(UCPlayer player, bool newPlayer) : base(player)
    {
        IsNewPlayer = newPlayer;
        SaveData = player.Save;
    }
}
