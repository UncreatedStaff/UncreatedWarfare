namespace Uncreated.Warfare.Gamemodes.Interfaces;

public interface IGamemode
{
    string DisplayName { get; }
    ulong GameId { get; }
    string Name { get; }
    State State { get; }
}
