using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("set", "s"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitSetCommand : IExecutableCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    public required CommandContext Context { get; init; }

    public KitSetCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
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
        kit = await _kitDataStore.UpdateKitAsync(kit.Key, KitInclude.Translations | KitInclude.UnlockRequirements, kit =>
        {
            Class oldClass = kit.Class;
            Branch oldbranch = kit.Branch;
            float oldReqCooldown = kit.RequestCooldown;
            result = SettableUtil<KitModel>.SetProperty(kit, property, newValue, Context.ParseFormat, _serviceProvider, out property, out propertyType);

            if (result != SetPropertyResult.Success || kit.Class == oldClass)
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

                Context.Reply(_translations.KitPropertySet, property, kit!, newValue);
                // todo: Context.LogAction(ActionLogType.SetKitProperty, kitId + ": " + property.ToUpper() + " >> " + newValue.ToUpper());
                break;
        }
    }
}