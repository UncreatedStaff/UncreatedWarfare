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

        string path = Path.Combine(_warfare.HomeDirectory, "Voice",
            onlinePlayer.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + "_" +
            DateTime.UtcNow.ToString("s").Replace(':', '_') + ".wav");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        FileStream fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write, FileShare.Read
        );

        AudioConvertResult status = await playerComp.TryConvert(fs, leaveOpen: false, token);
        Context.ReplyString(status == AudioConvertResult.Success
            ? $"Converted audio for {onlinePlayer}."
            : $"Failed to convert audio for {onlinePlayer}. Reason: {status}.");

        playerComp.Reset();
    }
}