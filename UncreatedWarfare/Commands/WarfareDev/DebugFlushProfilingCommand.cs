using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("profile"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugFlushProfilingCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
#if !PROFILING
        throw Context.SendNotImplemented();
#else
        Context.AssertRanByTerminal();

        if (Context.MatchParameter(0, "enable", "start"))
        {
            if (ProfilerUtil.Enabled)
                throw Context.ReplyString("Already enabled.");

            ProfilerUtil.Enabled = true;
            throw Context.ReplyString("Enabled profiling.");
        }

        if (Context.MatchParameter(0, "disable", "stop"))
        {
            if (!ProfilerUtil.Enabled)
                throw Context.ReplyString("Already disabled.");

            ProfilerUtil.Enabled = false;
            throw Context.ReplyString("Disabled profiling.");
        }

        if (Context.MatchParameter(0, "flush"))
        {
            ProfilerUtil.Flush();
            throw Context.ReplyString("Flushed profiling data.");
        }

        throw Context.SendCorrectUsage("/wdev profile <enable|disable|flush>");
#endif
    }
}