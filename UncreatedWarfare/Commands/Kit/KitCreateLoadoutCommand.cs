using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("createloadout", "cloadout", "cl"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitCreateLoadoutCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly LoadoutService _loadoutService;
    private readonly KitWeaponTextService _kitWeaponTextService;
    private readonly IPlayerService _playerService;
    private readonly IUserDataService _userDataService;
    private readonly AssetRedirectService _assetRedirectService;
    public required CommandContext Context { get; init; }

    public KitCreateLoadoutCommand(IServiceProvider serviceProvider)
    {
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
        _kitWeaponTextService = serviceProvider.GetRequiredService<KitWeaponTextService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _assetRedirectService = serviceProvider.GetRequiredService<AssetRedirectService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out CSteamID steam64, out _) || !Context.TryGet(1, out Class @class))
        {
            throw Context.SendHelp();
        }

        if (!Context.TryGetRange(2, out string? signText) || string.IsNullOrWhiteSpace(signText))
        {
            signText = null;
        }

        Kit loadout = await _loadoutService.CreateLoadoutAsync(steam64, Context.CallerId, @class, signText, async kit =>
        {
            await UniTask.SwitchToMainThread(token);

            List<IItem> items = ItemUtility.ItemsFromInventory(Context.Player, assetRedirectService: _assetRedirectService);

            kit.Items ??= new List<KitItemModel>(items.Count);
            foreach (IItem item in items)
            {
                KitItemModel model = new KitItemModel { KitId = kit.PrimaryKey };
                KitItemUtility.CreateKitItemModel(item, model);
                kit.Items.Add(model);
            }

            kit.Weapons = _kitWeaponTextService.GetWeaponText(items);

            // items have already been added so might as well unlock it
            kit.Disabled = false;

        }, token).ConfigureAwait(false);
        
        IPlayer player = await _playerService.GetOfflinePlayer(steam64, _userDataService, CancellationToken.None).ConfigureAwait(false);

        Context.Reply(_translations.LoadoutCreated, @class, player, player, loadout);
    }
}