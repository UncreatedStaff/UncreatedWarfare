using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player tries to send a chat message.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer)]
public class PlayerChatRequested : CancellablePlayerEvent
{
    private EChatMode _chatMode;

    /// <summary>
    /// The text that will be sent to everyone else.
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// If the caller has permission to bypass checks for chat.
    /// </summary>
    public required bool HasAdminChatPermissions { get; init; }
    
    /// <summary>
    /// The original text the player sent.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// The player's names to be formatted into the message for each player.
    /// </summary>
    public required PlayerNames PlayerName { get; set; }
    
    /// <summary>
    /// The text that will be sent in front of the message.
    /// </summary>
    public required string Prefix { get; set; }

    /// <summary>
    /// The color of the message.
    /// </summary>
    public required Color MessageColor { get; set; }

    /// <summary>
    /// If the message came from an event hook.
    /// </summary>
    public required bool IsUnityMessage { get; init; }

    /// <summary>
    /// If rich text should be allowed.
    /// </summary>
    public required bool AllowRichText { get; set; }

    /// <summary>
    /// If the chat message should be sent out instead of just ignored.
    /// </summary>
    /// <remarks>Note this is not the same as cancelling the event because <see cref="PlayerChatSent"/> will still get dispatched.</remarks>
    public required bool ShouldReplicate { get; set; }

    /// <summary>
    /// Custom icon to use instead of the player's profile picture.
    /// </summary>
    /// <remarks><see langword="null"/> = the sender's profile picture.</remarks>
    public required string? IconUrlOverride { get; set; }

    /// <summary>
    /// The broadcast mode.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public required EChatMode ChatMode
    {
        get => _chatMode;
        set
        {
            if (value is not EChatMode.GLOBAL and not EChatMode.LOCAL and not EChatMode.GROUP)
                throw new ArgumentOutOfRangeException(nameof(value));

            _chatMode = value;
        }
    }

    /// <summary>
    /// Getter for a list of all players the message should be sent to.
    /// </summary>
    public required Func<PlayerChatRequested, IEnumerable<WarfarePlayer>> TargetPlayers { get; set; }
}