using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("create", "c", "override"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitCreateCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    private readonly IFactionDataStore _factionStorage;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly KitWeaponTextService _kitWeaponTextService;

    public required CommandContext Context { get; init; }

    public KitCreateCommand(IServiceProvider serviceProvider)
    {
        _kitWeaponTextService = serviceProvider.GetRequiredService<KitWeaponTextService>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _factionStorage = serviceProvider.GetRequiredService<IFactionDataStore>();
        _commandDispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
        _assetRedirectService = serviceProvider.GetRequiredService<AssetRedirectService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out string? kitId))
        {
            throw Context.SendHelp();
        }

        Kit? existingKit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Base, token);
        if (existingKit != null)
        {
            // overwrite kit
            Context.Reply(_translations.KitConfirmOverride, existingKit, existingKit);

            // wait for /confirm
            CommandWaitResult confirmResult = await _commandDispatcher.WaitForCommand(typeof(ConfirmCommand), Context.Caller, token: token);
            if (!confirmResult.IsSuccessfullyExecuted)
                throw Context.Reply(_translations.KitCancelOverride);
            
            existingKit = await _kitDataStore.UpdateKitAsync(existingKit.Key, KitInclude.Items, async kit =>
            {
                await UniTask.SwitchToMainThread(token);

                List<IItem> items = ItemUtility.ItemsFromInventory(Context.Player, refillItems: true, assetRedirectService: _assetRedirectService);

                kit.Items.Clear();
                foreach (IItem item in items)
                {
                    KitItemModel model = new KitItemModel { KitId = kit.PrimaryKey };
                    KitItemUtility.CreateKitItemModel(item, model);
                    kit.Items.Add(model);
                }

                kit.Weapons = _kitWeaponTextService.GetWeaponText(items);

            }, Context.CallerId, token).ConfigureAwait(false);

            if (existingKit == null)
                throw Context.SendUnknownError();

            // todo: Context.LogAction(ActionLogType.EditKit, "OVERRIDE ITEMS " + existingKit.Id + ".");
            Context.Reply(_translations.KitOverwrote, existingKit);
            return;
        }

        if (!Context.TryGet(1, out Class @class))
        {
            if (Context.HasArgs(2))
                throw Context.Reply(_translations.ClassNotFound, Context.Get(1)!);

            throw Context.SendHelp();
        }

        KitType type = KitType.Public;
        if (Context.HasArgs(3) && !Context.MatchParameter(2, "default", "def", "-") && !Context.TryGet(2, out type))
        {
            throw Context.Reply(_translations.TypeNotFound, Context.Get(2)!);
        }

        string? factionString = null;
        if (Context.HasArgs(4) && !Context.MatchParameter(3, "default", "def", "-") && !Context.TryGet(3, out factionString))
        {
            throw Context.Reply(_translations.TypeNotFound, Context.Get(2)!);
        }

        FactionInfo? faction = null;
        if (!string.IsNullOrWhiteSpace(factionString))
        {
            faction = _factionStorage.FindFaction(factionString);

            if (faction == null)
            {
                throw Context.Reply(_translations.FactionNotFound, factionString);
            }
        }

        if (@class == Class.None)
        {
            @class = Class.Unarmed;
        }

        Kit kit = await _kitDataStore.AddKitAsync(kitId, @class, null, Context.CallerId, async kit =>
        {
            await UniTask.SwitchToMainThread(token);

            List<IItem> items = ItemUtility.ItemsFromInventory(Context.Player, refillItems: true, assetRedirectService: _assetRedirectService);

            kit.Items.Clear();
            foreach (IItem item in items)
            {
                KitItemModel model = new KitItemModel { KitId = kit.PrimaryKey };
                KitItemUtility.CreateKitItemModel(item, model);
                kit.Items.Add(model);
            }

            kit.Weapons = _kitWeaponTextService.GetWeaponText(items);

            kit.FactionId = faction?.PrimaryKey;
            kit.Type = type;

        }, token).ConfigureAwait(false);

        // todo: Context.LogAction(ActionLogType.CreateKit, kitId);
        Context.Reply(_translations.KitCreated, kit);
    }
}
