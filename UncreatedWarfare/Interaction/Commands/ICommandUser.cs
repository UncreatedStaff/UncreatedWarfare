using System;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;

[CannotApplyEqualityOperator]
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
    /// If this user is disconnected from the server.
    /// </summary>
    /// <remarks>Return <see langword="false"/> if not supported.</remarks>
    bool IsDisconnected { get; }

    /// <summary>
    /// Send a raw string as feedback to this actor.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    void SendMessage(string message);

    /// <summary>
    /// Create an <see cref="IModerationActor"/> for this command actor.
    /// </summary>
    /// <exception cref="NotSupportedException"/>
    IModerationActor GetModerationActor();
}