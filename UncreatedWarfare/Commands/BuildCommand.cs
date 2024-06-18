using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[HideFromHelp, Command("build")]
public class BuildCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.Reply(T.BuildLegacyExplanation);
    }
}
