using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked after a player sends a chat message.
/// </summary>
public class PlayerChatSent : PlayerEvent
{
    /// <summary>
    /// The text that will be sent to everyone else.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The original text the player sent.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// The player's names to be formatted into the message for each player.
    /// </summary>
    public required PlayerNames PlayerName { get; init; }
    
    /// <summary>
    /// The text that will be sent in front of the message.
    /// </summary>
    public required string Prefix { get; init; }

    /// <summary>
    /// The color of the message.
    /// </summary>
    public required Color MessageColor { get; init; }

    /// <summary>
    /// If rich text should be allowed.
    /// </summary>
    public required bool AllowRichText { get; init; }

    /// <summary>
    /// If the chat message was sent out instead of just being ignored.
    /// </summary>
    /// <remarks>Note this is not the same as cancelling the event because <see cref="PlayerChatSent"/> is still dispatched.</remarks>
    public required bool WasReplicated { get; init; }

    /// <summary>
    /// Custom icon to use instead of the player's profile picture.
    /// </summary>
    /// <remarks><see langword="null"/> = the sender's profile picture.</remarks>
    public required string? IconUrlOverride { get; init; }

    /// <summary>
    /// The broadcast mode.
    /// </summary>
    public required EChatMode ChatMode { get; init; }

    /// <summary>
    /// Getter for a list of all players the message should be sent to.
    /// </summary>
    public required Func<PlayerChatRequested, IEnumerable<WarfarePlayer>> TargetPlayers { get; set; }

    /// <summary>
    /// The original chat request.
    /// </summary>
    public required PlayerChatRequested Request { get; set; }

    /// <summary>
    /// Formats the message for a player.
    /// </summary>
    public required Func<WarfarePlayer, bool, string?> FormatHandler { get; init; }
}