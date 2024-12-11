using System;
using System.IO;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("savevoice"), MetadataFile]
internal sealed class SaveVoiceBufferCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out _, out WarfarePlayer? onlinePlayer, true) || onlinePlayer == null)
            throw Context.SendPlayerNotFound();

        AudioRecordPlayerComponent? playerComp = onlinePlayer.Component<AudioRecordPlayerComponent>();
        if (playerComp == null)
            throw Context.SendUnknownError();

        FileStream fs = new FileStream(
            Path.Combine(Data.Paths.BaseDirectory, "Voice", onlinePlayer.Steam64 + "_" + DateTime.UtcNow.ToString("s").Replace(':', '_') + ".wav"),
            FileMode.Create,
            FileAccess.Write, FileShare.Read
        );

        AudioRecordManager.AudioConvertResult status = await playerComp.TryConvert(fs, false, token);
        Context.ReplyString($"Converted audio for {onlinePlayer}. Status: {status}.");
        playerComp.Reset();
    }
}