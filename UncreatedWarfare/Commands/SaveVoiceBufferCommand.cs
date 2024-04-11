using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Commands;
public class SaveVoiceBufferCommand : AsyncCommand
{
    public SaveVoiceBufferCommand() : base("savevoice", EAdminType.ADMIN_ON_DUTY, sync: true)
    {
        Structure = new CommandStructure
        {
            Description = "Save a player's voice history.",
            Parameters =
            [
                new CommandParameter("player", typeof(IPlayer))
            ]
        };
    }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        if (!ctx.TryGet(0, out _, out UCPlayer? onlinePlayer, true) || onlinePlayer == null)
            throw ctx.SendPlayerNotFound();

        AudioRecordPlayerComponent? playerComp = AudioRecordPlayerComponent.Get(onlinePlayer);
        if (playerComp == null)
            throw ctx.SendUnknownError();

        FileStream fs = new FileStream(
                            Path.Combine(Data.Paths.BaseDirectory, "Voice", onlinePlayer.Steam64 + "_" + DateTime.UtcNow.ToString("s").Replace(':', '_') + ".wav"),
                            FileMode.Create,
                            FileAccess.Write, FileShare.Read
        );

        AudioRecordManager.AudioConvertResult status = await playerComp.TryConvert(fs, false, token);
        ctx.ReplyString($"Converted audio for {onlinePlayer}. Status: {status}.");

        playerComp.Reset();
    }
}