using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands;

[Command("migratekitaccess"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugMigrateKitAccessCommand : IExecutableCommand
{
    private readonly IKitAccessService _kitAccessService;
    public required CommandContext Context { get; init; }

    public DebugMigrateKitAccessCommand(IKitAccessService kitAccessService)
    {
        _kitAccessService = kitAccessService;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        if (_kitAccessService is not MySqlKitAccessService kitAccessService)
        {
            throw Context.SendNotImplemented();
        }

        try
        {
            await kitAccessService.MigrateSeason4KitAccessAsync(Context.MatchFlag('d', "delete"), Context.ServiceProvider, token);
            Context.ReplyString("Operation completed.");
        }
        catch (Exception ex)
        {
            Context.ReplyString("Failed to migrate kits with an exception. See console.");
            Context.Logger.LogError(ex, "Error migrating kit access.");
        }
    }
}