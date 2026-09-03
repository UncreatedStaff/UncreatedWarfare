using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Commands;

[Command("map"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSwapMapCommand : IExecutableCommand
{
    private readonly MapSwitchService _mapSwitchService;
    private readonly WarfareModule _module;
    private readonly IGameDataDbContext _dbContext;

    public required CommandContext Context { get; init; }

    public DebugSwapMapCommand(MapSwitchService mapSwitchService, WarfareModule module, IGameDataDbContext dbContext)
    {
        _mapSwitchService = mapSwitchService;
        _module = module;
        _dbContext = dbContext;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.HasArgs(1))
            throw Context.SendCorrectUsage("/wdev map <name... >");

        string? levelName = Context.GetRange(0);
        MapData? mapData = await _dbContext.Maps
            .AsNoTracking()
            .Include(x => x.Dependencies)
            .FirstOrDefaultAsync(x => x.DisplayName == levelName, token);

        await UniTask.SwitchToMainThread(token);

        if (mapData == null)
        {
            throw Context.ReplyString($"Can't find map {levelName}.");
        }

        Context.ReplyString($"Switching to {mapData.DisplayName}...");
        // the token will be cancelled when the player disconnects during the switching process, so don't use it
        await _mapSwitchService.SwitchMapAsync(mapData, _module.UnloadToken);
    }
}