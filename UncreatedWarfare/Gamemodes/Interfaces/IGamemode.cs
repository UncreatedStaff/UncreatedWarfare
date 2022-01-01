namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IGamemode
    {
        string DisplayName { get; }
        long GameID { get; }
        string Name { get; }
        EState State { get; }
    }
}
