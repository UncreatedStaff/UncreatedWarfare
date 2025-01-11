using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Moderation;

[PlayerComponent]
public class PlayerModerationCacheComponent : IPlayerComponent
{
    private DatabaseInterface _moderationSql;
    public WarfarePlayer Player { get; private set; }

    public string? TextMuteReason { get; private set; }
    public MuteType TextMuteType { get; private set; }
    public DateTime TextMuteExpiryTime { get; private set; }
    
    public string? VoiceMuteReason { get; private set; }
    public MuteType VoiceMuteType { get; private set; }
    public DateTime VoiceMuteExpiryTime { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _moderationSql = serviceProvider.GetRequiredService<DatabaseInterface>();
        Task.Run(async () =>
        {
            try
            {
                await RefreshActiveMute();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                serviceProvider.GetRequiredService<ILogger<PlayerModerationCacheComponent>>().LogError(ex, "Error fetching mute info.");
            }
        });
    }

    public async Task RefreshActiveMute()
    {
        if (Player.IsDisconnected)
            throw new OperationCanceledException();

        CancellationToken token = Player.DisconnectToken;
        Mute[] mutes = await _moderationSql.GetActiveEntries<Mute>(Player.Steam64,
            await _moderationSql.GetIPAddresses(Player.Steam64, true, token),
            await _moderationSql.GetHWIDs(Player.Steam64, token), detail: false, token: token,
            condition: $"`{DatabaseInterface.ColumnEntriesResolvedTimestamp}` IS NOT NULL");


        if (mutes.Length == 0)
        {
            TextMuteReason = null;
            TextMuteType = MuteType.None;
            TextMuteExpiryTime = default;
            VoiceMuteReason = null;
            VoiceMuteType = MuteType.None;
            VoiceMuteExpiryTime = default;
            return;
        }

        MuteType handled = MuteType.None;

        foreach (Mute mute in mutes.OrderByDescending(x => x.GetExpiryTimestamp(false)))
        {
            switch (mute.Type)
            {
                case MuteType.Voice:
                    if (handled is MuteType.Voice or MuteType.Both)
                        continue;
                    handled |= MuteType.Voice;
                    break;

                case MuteType.Text:
                    if (handled is MuteType.Text or MuteType.Both)
                        continue;
                    handled |= MuteType.Text;
                    break;

                case MuteType.Both:
                    if (handled == MuteType.Both)
                        continue;
                    handled = MuteType.Both;
                    break;
            }

            if ((mute.Type & MuteType.Voice) != 0)
            {
                VoiceMuteExpiryTime = mute.IsPermanent ? DateTime.MaxValue : mute.GetExpiryTimestamp(false).UtcDateTime;
                VoiceMuteReason = mute.Message;
                VoiceMuteType = mute.Type;
            }

            if ((mute.Type & MuteType.Text) != 0)
            {
                TextMuteExpiryTime = mute.IsPermanent ? DateTime.MaxValue : mute.GetExpiryTimestamp(false).UtcDateTime;
                TextMuteReason = mute.Message;
                TextMuteType = mute.Type;
            }

            if (handled == MuteType.Both)
                break;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
