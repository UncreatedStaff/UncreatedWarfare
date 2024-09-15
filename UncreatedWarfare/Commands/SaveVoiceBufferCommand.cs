using System;
using System.IO;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("savevoice")]
[MetadataFile(nameof(GetHelpMetadata))]
public class SaveVoiceBufferCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Save a player's voice history.",
            Parameters =
            [
                new CommandParameter("player", typeof(IPlayer))
            ]
        };
    }
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out _, out UCPlayer? onlinePlayer, true) || onlinePlayer == null)
            throw Context.SendPlayerNotFound();

        AudioRecordPlayerComponent? playerComp = AudioRecordPlayerComponent.Get(onlinePlayer);
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