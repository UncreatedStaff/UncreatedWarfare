using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using System;

namespace Uncreated.Warfare.Commands.Groups;

[Command("join")]
[CommandDescription("Join a team.")]
[CommandParent(typeof(GroupCommand))]
public class GroupJoinCommand : UnturnedCommand
{
    public GroupJoinCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }
    protected override UniTask OnExecuteAsync()
    {
        throw new NotImplementedException();
    }
}
