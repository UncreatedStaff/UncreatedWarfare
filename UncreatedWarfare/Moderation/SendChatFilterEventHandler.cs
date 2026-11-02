using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Handles stopping chat messages that violate the chat filter.
/// </summary>
internal class SendChatFilterEventHandler : IEventListener<PlayerChatRequested>
{
    private readonly ChatService _chatService;
    private readonly ModerationTranslations _translations;

    public SendChatFilterEventHandler(ChatService chatService, TranslationInjection<ModerationTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;
    }

    void IEventListener<PlayerChatRequested>.HandleEvent(PlayerChatRequested e, IServiceProvider serviceProvider)
    {
        if (e.HasAdminChatPermissions)
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        string? match = ChatFilterHelper.GetChatFilterViolation(e.Text);
        if (match == null)
            return;

        _chatService.Send(e.Player, _translations.ChatFilterFeedback, match);
        // todo: ActionLog.Add(ActionLogType.ChatFilterViolation, e.ChatMode switch { EChatMode.LOCAL => "AREA/SQUAD: ", EChatMode.GLOBAL => "GLOBAL: ", _ => "TEAM: " } + e.Text, e.Steam64);
        e.Cancel();
    }
}