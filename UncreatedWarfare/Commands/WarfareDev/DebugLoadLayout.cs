using System;
using System.IO;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts;

namespace Uncreated.Warfare.Commands;

[Command("layout", "gamemode"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugLoadLayout : IExecutableCommand
{
    private readonly LayoutFactory _layoutFactory;

    public required CommandContext Context { get; init; }

    public DebugLoadLayout(LayoutFactory layoutFactory)
    {
        _layoutFactory = layoutFactory;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? layoutName))
        {
            throw Context.SendHelp();
        }

        bool instant = Context.MatchFlag('i', "instant");

        UniTask.Create(async () =>
        {
            await UniTask.NextFrame();

            try
            {
                LayoutInfo? layout = _layoutFactory.SelectLayoutByName(layoutName);
                if (layout == null)
                {
                    Context.ReplyString($"Layout not found or ambiguous match: <#ddd>{layoutName}</color>.");
                    return;
                }

                layout.Dispose();
                _layoutFactory.NextLayout = new FileInfo(layout.FilePath);

                if (instant)
                {
                    Context.ReplyString($"Starting layout: <#ddd>{layout.DisplayName}</color>.");
                    await _layoutFactory.StartNextLayout(CancellationToken.None);
                    Context.ReplyString("Done.");
                }
                else
                {
                    Context.ReplyString($"The next layout will be: <#ddd>{layout.DisplayName}</color>.");
                }
            }
            catch (Exception ex)
            {
                Context.Logger.LogError(ex, "Error getting or starting layout.");
            }
        });

        Context.Defer();

        return UniTask.CompletedTask;
    }
}