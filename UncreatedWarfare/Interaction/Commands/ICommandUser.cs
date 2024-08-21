namespace Uncreated.Warfare.Interaction.Commands;
public interface ICommandUser
{
    /// <summary>
    /// If permissions shouldn't even be considered for this actor (the console).
    /// </summary>
    bool IsSuperUser { get; }

    /// <summary>
    /// If <see cref="SendMessage"/> outputs to a terminal.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// If <see cref="SendMessage"/> should use IMGUI (Unity rich text) for coloring.
    /// </summary>
    bool IMGUI { get; }

    /// <summary>
    /// Steam ID of this actor where applicable, or zero.
    /// </summary>
    CSteamID Steam64 { get; }

    /// <summary>
    /// Send a raw string as feedback to this actor.
    /// </summary>
    void SendMessage(string message);
}
