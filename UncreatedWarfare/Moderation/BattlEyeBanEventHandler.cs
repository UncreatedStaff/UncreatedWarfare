using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Moderation;

internal sealed class BattlEyeBanEventHandler : IAsyncEventListener<BattlEyeKicked>
{
    private readonly DatabaseInterface _moderationSql;
    private readonly ChatService _chatService;
    private readonly ModerationTranslations _translations;

    public BattlEyeBanEventHandler(DatabaseInterface moderationSql, ChatService chatService, TranslationInjection<ModerationTranslations> translations)
    {
        _moderationSql = moderationSql;
        _chatService = chatService;
        _translations = translations.Value;
    }

    public async UniTask HandleEventAsync(BattlEyeKicked e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        _chatService.Broadcast<IPlayer>(_translations.BattlEyeKickBroadcast, e.Player);

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        await _moderationSql.AddOrUpdate(new BattlEyeKick
        {
            Player = e.Steam64.m_SteamID,
            Message = e.KickReason,
            StartedTimestamp = utcNow,
            ResolvedTimestamp = utcNow
        }, token);
    }
}