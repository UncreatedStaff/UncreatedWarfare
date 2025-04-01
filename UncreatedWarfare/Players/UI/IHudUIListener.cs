namespace Uncreated.Warfare.Players.UI;

public interface IHudUIListener
{
    void Hide(WarfarePlayer? player);

    void Restore(WarfarePlayer? player);
}