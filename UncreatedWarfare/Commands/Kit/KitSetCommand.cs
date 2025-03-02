using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("set", "s"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitSetCommand : IExecutableCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    private readonly IPlayerService _playerService;
    private readonly EventDispatcher _eventDispatcher;
    public required CommandContext Context { get; init; }

    public KitSetCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _eventDispatcher = serviceProvider.GetRequiredService<EventDispatcher>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? property) || !Context.TryGet(1, out string? kitId) || !Context.TryGetRange(2, out string? newValue))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitDataStore.QueryKitAsync(kitId, KitInclude.Translations, token);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        kitId = kit.Id;

        SetPropertyResult result = SetPropertyResult.ObjectNotFound;
        Type? propertyType = null;
        bool significantChange = false;
        kit = await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Translations | KitInclude.UnlockRequirements, kit =>
        {
            KitType oldType = kit.Type;
            Class oldClass = kit.Class;
            Branch oldbranch = kit.Branch;
            float oldReqCooldown = kit.RequestCooldown;
            result = SettableUtil<KitModel>.SetProperty(kit, property, newValue, Context.ParseFormat, _serviceProvider, out property, out propertyType);

            if (result != SetPropertyResult.Success)
                return;

            significantChange = kit.Class != oldClass || kit.Branch != oldbranch || kit.Type != oldType;

            if (kit.Class == oldClass)
                return;

            // check for default values from the old class if the class changed and update those

            if (KitDefaults.GetDefaultBranch(oldClass) == oldbranch)
                kit.Branch = KitDefaults.GetDefaultBranch(kit.Class);

            if (Mathf.Abs(KitDefaults.GetDefaultRequestCooldown(oldClass) - oldReqCooldown) < 0.25f)
                kit.RequestCooldown = KitDefaults.GetDefaultRequestCooldown(kit.Class);


        }, Context.CallerId, token).ConfigureAwait(false);

        switch (result)
        {
            default: // ParseFailure
                Context.Reply(_translations.KitInvalidPropertyValue, newValue, propertyType!, property);
                return;

            case SetPropertyResult.PropertyProtected:
                Context.Reply(_translations.KitPropertyProtected, property);
                return;

            case SetPropertyResult.PropertyNotFound:
            case SetPropertyResult.TypeNotSettable:
                Context.Reply(_translations.KitPropertyNotFound, property);
                return;

            case SetPropertyResult.ObjectNotFound:
                Context.Reply(_translations.KitNotFound, kitId);
                return;

            case SetPropertyResult.Success:

                await UniTask.SwitchToMainThread(token);

                if (significantChange)
                {
                    await UpdateKit(kit!, token);
                }

                Context.Reply(_translations.KitPropertySet, property, kit!, newValue);
                Context.LogAction(ActionLogType.SetKitProperty, kitId + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                break;
        }
    }

    private async UniTask UpdateKit(Kit kit, CancellationToken token)
    {
        Kit? kitWithItems = await _kitDataStore.QueryKitAsync(kit.Key, KitInclude.Giveable, token).ConfigureAwait(false);

        if (kitWithItems == null)
            return;
        
        List<UniTask> eventTasks = new List<UniTask>();

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            KitPlayerComponent component = player.Component<KitPlayerComponent>();
            uint? kitKey = component.ActiveKitKey;
            if (kitKey.HasValue && kitKey.Value == kitWithItems.Key)
            {
                component.UpdateKit(kitWithItems);

                PlayerKitChanged args = new PlayerKitChanged
                {
                    Player = player,
                    KitId = kitWithItems.Key,
                    Class = kitWithItems.Class,
                    Kit = kit,
                    KitName = kitWithItems.Id,
                    WasRequested = false
                };

                eventTasks.Add(_eventDispatcher.DispatchEventAsync(args, CancellationToken.None));
            }
        }

        await UniTask.WhenAll(eventTasks);
    }
}