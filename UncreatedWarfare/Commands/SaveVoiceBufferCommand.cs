using System;
using System.Globalization;
using System.IO;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("savevoice"), MetadataFile]
internal sealed class SaveVoiceBufferCommand : IExecutableCommand
{
    private readonly WarfareModule _warfare;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public SaveVoiceBufferCommand(WarfareModule warfare)
    {
        _warfare = warfare;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        (_, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0, remainder: true).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        if (onlinePlayer == null)
            throw Context.SendPlayerNotFound();

        AudioRecordPlayerComponent? playerComp = onlinePlayer.Component<AudioRecordPlayerComponent>();
        if (playerComp == null)
            throw Context.SendUnknownError();

        FileStream fs = new FileStream(
            Path.Combine(_warfare.HomeDirectory, "Voice", onlinePlayer.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + "_" + DateTime.UtcNow.ToString("s").Replace(':', '_') + ".wav"),
            FileMode.Create,
            FileAccess.Write, FileShare.Read
        );

        AudioRecordManager.AudioConvertResult status = await playerComp.TryConvert(fs, leaveOpen: false, token);
        Context.ReplyString($"Converted audio for {onlinePlayer}. Status: {status}.");
        playerComp.Reset();
    }
}