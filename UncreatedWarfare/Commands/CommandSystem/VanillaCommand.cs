using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public sealed class VanillaCommand : IExecutableCommand, IComparable<VanillaCommand>, IComparable<SDG.Unturned.Command>
{
    private readonly SDG.Unturned.Command _cmd;
    private readonly EAdminType _allowedUsers;
    SemaphoreSlim? IExecutableCommand.Semaphore { get; set; }
    string IExecutableCommand.CommandName => _cmd.command;
    EAdminType IExecutableCommand.AllowedPermissions => _allowedUsers;
    bool IExecutableCommand.ExecuteAsynchronously => false;
    int IExecutableCommand.Priority => 0;
    IReadOnlyList<string>? IExecutableCommand.Aliases => null;
    bool IExecutableCommand.Synchronize => false;
    public VanillaCommand(SDG.Unturned.Command cmd)
    {
        _cmd = cmd;
        _allowedUsers = CommandHandler.GetVanillaPermissions(cmd);
    }

    bool IExecutableCommand.CheckPermission(CommandInteraction ctx)
    {
        if (ctx.IsConsole || ctx.Caller!.Player.channel.owner.isAdmin) return true;

        return ctx.Caller.PermissionCheck(_allowedUsers, PermissionComparison.AtLeast);
    }

    void IExecutableCommand.Execute(CommandInteraction interaction)
    {
        _cmd.check(interaction.CallerCSteamID, _cmd.command, string.Join("/", interaction.Parameters));
    }
    Task IExecutableCommand.Execute(CommandInteraction interaction, CancellationToken token) => throw new NotImplementedException();
    CommandInteraction IExecutableCommand.SetupCommand(UCPlayer? caller, string[] args, string message, bool keepSlash)
    {
        if (!keepSlash && args.Length > 0)
        {
            ref string end = ref args[args.Length - 1];
            // removes the accidental ending backslash from the last argument
            if (end.Length > 1 && end.EndsWith("\\", StringComparison.Ordinal))
                end = end.Substring(0, end.Length - 1);
        }

        return new CommandInteraction(new CommandInteraction.ContextData(caller, args, message), this);
    }

    int IComparable<SDG.Unturned.Command>.CompareTo(SDG.Unturned.Command other) => _cmd.CompareTo(other);
    int IComparable<VanillaCommand>.CompareTo(VanillaCommand other) => _cmd.CompareTo(other._cmd);
}
