using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("upgrade", "update", "upg"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitUpgradeCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    private readonly LoadoutService _loadoutService;
    private readonly KitRequestService _kitRequestService;

    public required CommandContext Context { get; init; }

    public KitUpgradeCommand(IServiceProvider serviceProvider)
    {
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
        _kitRequestService = serviceProvider.GetRequiredService<KitRequestService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? kitName))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitName, KitInclude.Translations, token).ConfigureAwait(false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        if (kit.Season >= WarfareModule.Season)
        {
            throw Context.Reply(_translations.DoesNotNeedUpgrade, kit);
        }

        if (!Context.TryGet(1, out Class @class))
        {
            throw Context.SendHelp();
        }

        if (LoadoutIdHelper.Parse(kit.Id, out CSteamID playerId) < 1)
            throw Context.Reply(_translations.KitLoadoutIdBadFormat);

        kit = await _loadoutService.UpgradeLoadoutAsync(playerId, Context.CallerId, @class, kit.Key, token: token).ConfigureAwait(false);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitName);
        }

        Context.Reply(_translations.LoadoutUpgraded, kit, @class);
        await _kitRequestService.GiveKitAsync(Context.Player, new KitBestowData(kit), token).ConfigureAwait(false);
    }
}