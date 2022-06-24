using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Framework;

namespace Uncreated.Warfare.Commands.CommandSystem;
public sealed class VanillaCommand : IExecutableCommand, IComparable<VanillaCommand>, IComparable<SDG.Unturned.Command>
{
    private readonly SDG.Unturned.Command _cmd;
    private readonly EAdminType allowedUsers;
    string IExecutableCommand.CommandName => _cmd.command;
    EAdminType IExecutableCommand.AllowedPermissions => allowedUsers;
    int IExecutableCommand.Priority => 0;
    IReadOnlyList<string>? IExecutableCommand.Aliases => null;
    public VanillaCommand(SDG.Unturned.Command cmd)
    {
        _cmd = cmd;
        allowedUsers = CommandHandler.GetVanillaPermissions(cmd);
    }

    bool IExecutableCommand.CheckPermission(CommandInteraction ctx)
    {
        if (ctx.IsConsole || ctx.Caller!.Player.channel.owner.isAdmin) return true;

        return ctx.Caller.PermissionCheck(allowedUsers, PermissionComparison.AtLeast);
    }
    void IExecutableCommand.Execute(CommandInteraction interaction)
    {
        _cmd.check(interaction.CallerCSteamID, _cmd.command, string.Join("/", interaction.Parameters));
    }
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
