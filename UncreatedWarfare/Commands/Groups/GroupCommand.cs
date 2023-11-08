using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using System;
using OpenMod.Unturned.Users;

namespace Uncreated.Warfare.Commands.Groups;

[Command("group")]
[CommandDescription("Change or view your group info.")]
[CommandActor(typeof(UnturnedUser))]
public class GroupCommand : UnturnedCommand
{
    public GroupCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }
    protected override UniTask OnExecuteAsync()
    {
        throw new NotImplementedException();
    }
}
