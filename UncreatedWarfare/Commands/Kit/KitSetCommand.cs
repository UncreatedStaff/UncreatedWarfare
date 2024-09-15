using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("set", "s"), SubCommandOf(typeof(KitCommand))]
internal class KitSetCommand : IExecutableCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IKitsDbContext _dbContext;
    public CommandContext Context { get; set; }

    public KitSetCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? property) || !Context.TryGet(1, out string? kitId) || !Context.TryGetRange(2, out string? newValue))
        {
            throw Context.SendHelp();
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, true, x => x.Kits
            .Include(y => y.UnlockRequirementsModels)
            .Include(y => y.Translations)
        );

        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound);
        }

        kitId = kit.InternalName;

        KitType prevType = kit.Type;
        Class oldClass = kit.Class;
        Branch oldbranch = kit.Branch;
        float oldReqCooldown = kit.RequestCooldown;
        float? oldTeamLimit = kit.TeamLimit;
        SetPropertyResult result = SettableUtil<Kit>.SetProperty(kit, property, newValue, Context.ParseFormat, _serviceProvider, out property, out Type propertyType);

        switch (result)
        {
            default: // ParseFailure
                Context.Reply(_translations.KitInvalidPropertyValue, newValue, propertyType, property);
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
                if (kit.Class != oldClass)
                {
                    // check for default values from the old class if the class changed and update those
                    if (oldTeamLimit.HasValue && Mathf.Abs(KitDefaults.GetDefaultTeamLimit(oldClass) - oldTeamLimit.Value) < 0.005f)
                        kit.TeamLimit = null;

                    if (KitDefaults.GetDefaultBranch(oldClass) == oldbranch)
                        kit.Branch = KitDefaults.GetDefaultBranch(kit.Class);

                    if (Mathf.Abs(KitDefaults.GetDefaultRequestCooldown(oldClass) - oldReqCooldown) < 0.25f)
                        kit.RequestCooldown = KitDefaults.GetDefaultRequestCooldown(kit.Class);
                }

                kit.UpdateLastEdited(Context.CallerId);
                _dbContext.Update(kit);
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                await UniTask.SwitchToMainThread(token);

                _kitManager.Signs.UpdateSigns(kit);
                Context.Reply(_translations.KitPropertySet, property, kit, newValue);
                Context.LogAction(ActionLogType.SetKitProperty, kitId + ": " + property.ToUpper() + " >> " + newValue.ToUpper());

                if (oldbranch == kit.Branch && oldClass == kit.Class && prevType == kit.Type)
                {
                    break;
                }

                // refresh kit
                kit = await _kitManager.GetKit(kit.PrimaryKey, token, x => KitManager.RequestableSet(x, false));
                if (kit == null)
                {
                    break;
                }

                await UniTask.SwitchToMainThread(token);
                _kitManager.InvokeAfterMajorKitUpdate(kit, true);
                break;
        }
    }
}