using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("map"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSwapMapCommand : IExecutableCommand
{
    private readonly MapSwitchService _mapSwitchService;
    private readonly MapScheduler _mapScheduler;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly IGameDataDbContext _dbContext;
    private readonly LayoutFactory _layoutFactory;

    public required CommandContext Context { get; init; }

    public DebugSwapMapCommand(
        MapSwitchService mapSwitchService,
        MapScheduler mapScheduler,
        CommandDispatcher commandDispatcher,
        IGameDataDbContext dbContext,
        LayoutFactory layoutFactory)
    {
        _mapSwitchService = mapSwitchService;
        _mapScheduler = mapScheduler;
        _commandDispatcher = commandDispatcher;
        _dbContext = dbContext;
        _layoutFactory = layoutFactory;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.HasArgs(1))
            throw Context.SendCorrectUsage("/wdev map <name... (case sensitive)>");

        string? levelName = Context.GetRange(0);
        MapData? mapData = await _dbContext.Maps
            .AsNoTracking()
            .Include(x => x.Dependencies)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.DisplayName == levelName, token);

        await UniTask.SwitchToMainThread(token);

        if (mapData == null)
        {
            throw Context.ReplyString($"Can't find map {levelName}.");
        }

        // reset next layout in case its on the wrong map
        _layoutFactory.NextLayout = null;

        if (!Context.MatchFlag('i', "instant"))
        {
            _mapScheduler.ScheduleMap(mapData);

            // broadcast will get sent no need to reply
            Context.Defer();

            return;
        }

        if (Provider.clients.Count > (Context.Player != null ? 1 : 0))
        {
            Context.ReplyString("Are you sure you want to change maps? There are other players on. Run /c to confirm in the next 10 seconds.");
            CommandWaitResult result = await _commandDispatcher.WaitForCommand(
                typeof(ConfirmCommand),
                Context.Caller,
                TimeSpan.FromSeconds(10),
                CommandWaitOptions.AbortOnOtherCommandExecuted | CommandWaitOptions.BlockOriginalExecution
            );

            if (!result.IsSuccessfullyExecuted)
            {
                Context.ReplyString("Map change aborted.");
                return;
            }
        }

        Context.ReplyString($"Switching to {mapData.DisplayName}...");

        // initialize the cached logger before the service scope is disposed
        _ = Context.Logger;

        _ = UniTask.Create(mapData, async mapData =>
        {
            try
            {
                // the token will be cancelled when the player disconnects during the switching process, so don't use it
                await _mapSwitchService.SwitchMapAsync(mapData, MapSwitchParameters.Default, token);

                Context.Logger.LogInformation($"Finished switching maps to {mapData.DisplayName}.");
            }
            catch (Exception ex)
            {
                Context.Logger.LogError(ex, "Error switching maps.");
            }
        });
    }
}