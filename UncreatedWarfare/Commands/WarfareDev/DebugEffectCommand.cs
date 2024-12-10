using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("effect"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugEffectCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        const string usage = "/test effect [clear] <name/id/guid> (for UI only - [key : int16] [arg0 : str] [arg1 : str] [arg2 : str] [arg3 : str] )";

        Context.AssertArgs(0, usage);
        EffectAsset? asset;
        if (Context.MatchParameter(0, "clear", "remove", "delete"))
        {
            Context.AssertArgs(1, usage);
            if (Context.MatchParameter(1, "all", "*", "any"))
                asset = null;
            else if (!Context.TryGet(1, out asset, out _, allowMultipleResults: true))
                throw Context.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{Context.Get(1)}</color>.");
            if (asset == null)
            {
                // todo: implement
                throw Context.ReplyString("<#9fa1a6>Cleared all effects.");
            }

            EffectManager.ClearEffectByGuid(asset.GUID, Context.Player.Connection);
            throw Context.ReplyString($"<#9fa1a6>Cleared all {asset.name} effects.");
        }

        if (!Context.TryGet(0, out asset, out _, allowMultipleResults: true))
            throw Context.ReplyString($"<#ff8c69>Can't find an effect with the term: <#ddd>{Context.Get(0)}</color>.");

        short key = Context.MatchParameter(1, "-", "_") || !Context.TryGet(1, out short s) ? (short)-1 : s;
        if (asset?.effect == null)
        {
            throw Context.ReplyString($"<#ff8c69>{asset?.name}'s effect property hasn't been set. Possibly the effect was set up incorrectly.");
        }

        if (asset.effect.GetComponentInChildren<Canvas>() != null)
        {
            ArraySegment<string> args = Context.Parameters[2..];
            switch (args.Count)
            {
                case <= 0:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true);
                    Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with no arguments and key {key}.");
                    break;

                case 1:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, args[0]);
                    Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{args[0]}\" and key {key}.");
                    break;

                case 2:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, args[0], args[1]);
                    Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{args[0]}\", {{1}} = \"{args[1]}\" and key {key}.");
                    break;

                case 3:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, args[0], args[1], args[2]);
                    Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{args[0]}\", {{1}} = \"{args[1]}\", {{2}} = \"{args[2]}\" and key {key}.");
                    break;

                default:
                    EffectManager.sendUIEffect(asset.id, key, Context.Player.Connection, true, args[0], args[1], args[2], args[3]);
                    Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you with {{0}} = \"{args[0]}\", {{1}} = \"{args[1]}\", {{2}} = \"{args[2]}\", {{3}} = \"{args[3]}\" and key {key}.");
                    break;
            }

            return UniTask.CompletedTask;
        }

        EffectUtility.TriggerEffect(asset, Context.Player.Connection, Context.Player.Position, true);
        Context.ReplyString($"<#9fa1a6>Sent {asset.name} to you at {Context.Player.Position.ToString("0.##", Context.Culture)}.");
        return UniTask.CompletedTask;
    }
}