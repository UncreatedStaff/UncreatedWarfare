using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class AttachCommand : Command
{
    public AttachCommand() : base("attach", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertPermissions(EAdminType.ADMIN_ON_DUTY);

        if (ctx.TryGet(out ))
    }
}