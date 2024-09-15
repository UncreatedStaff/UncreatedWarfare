using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("upgrade", "update", "upg"), SubCommandOf(typeof(KitCommand))]
internal class KitUpgradeCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    public CommandContext Context { get; set; }

    public KitUpgradeCommand(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? kitName))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitManager.FindKit(kitName, token, true);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        if (!kit.NeedsUpgrade)
        {
            if (kit.Season != UCWarfare.Season)
            {
                kit.Season = UCWarfare.Season;
                _dbContext.Update(kit);
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                throw Context.Reply(_translations.KitUpgraded, kit);
            }

            await UniTask.SwitchToMainThread(token);
            throw Context.Reply(_translations.DoesNotNeedUpgrade, kit);
        }

        if (!Context.TryGet(1, out Class @class))
        {
            throw Context.SendHelp();
        }

        if (LoadoutIdHelper.Parse(kit.InternalName, out CSteamID playerId) < 1)
            throw Context.Reply(_translations.KitLoadoutIdBadFormat);

        kit = await _kitManager.Loadouts.UpgradeLoadout(Context.CallerId, playerId, @class, kit.InternalName, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        Context.Reply(_translations.LoadoutUpgraded, kit, @class);
        await _kitManager.Requests.GiveKit(Context.Player, kit, true, false, token).ConfigureAwait(false);
    }
}