using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Vehicles.Spawners.Delays;

namespace Uncreated.Warfare.Commands;

[Command("copyfrom", "copy", "cf"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitCopyFromCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly IKitDataStore _kitDataStore;
    private readonly LoadoutService _loadoutService;
    private readonly LanguageService _languageService;
    private readonly ICachableLanguageDataStore _languageSql;
    private readonly KitRequestService _kitRequestService;

    public required CommandContext Context { get; init; }

    public KitCopyFromCommand(IServiceProvider serviceProvider)
    {
        _languageService = serviceProvider.GetRequiredService<LanguageService>();
        _kitRequestService = serviceProvider.GetRequiredService<KitRequestService>();
        _languageSql = serviceProvider.GetRequiredService<ICachableLanguageDataStore>();
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<KitCommandTranslations>>().Value;
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out string? existingKitId) || !Context.TryGet(1, out string? newKitId))
        {
            throw Context.SendHelp();
        }
        
        Kit? sourceKit = await _kitDataStore.QueryKitAsync(existingKitId, KitInclude.All, token);
        if (sourceKit == null)
        {
            throw Context.Reply(_translations.KitNotFound, existingKitId);
        }

        
        Kit? existingKit = await _kitDataStore.QueryKitAsync(newKitId, KitInclude.Translations, token);
        if (existingKit != null)
        {
            throw Context.Reply(_translations.KitNameTaken, newKitId);
        }

        Kit kit;

        if (LoadoutIdHelper.Parse(newKitId, out CSteamID player) > 0)
        {
            if (sourceKit.Type != KitType.Loadout)
            {
                throw Context.Reply(_translations.KitCopyNonLoadoutToLoadout, sourceKit, newKitId);
            }

            kit = await _loadoutService.CreateLoadoutAsync(player, Context.CallerId, sourceKit.Class, null, kit =>
            {
                CopyKit(kit, sourceKit);
                return Task.CompletedTask;
            }, token).ConfigureAwait(false);

            newKitId = kit.Id;
        }
        else
        {
            kit = await _kitDataStore.AddKitAsync(newKitId, sourceKit.Class, null, Context.CallerId, kit =>
            {
                CopyKit(kit, sourceKit);
            }, token);
        }

        Context.LogAction(ActionLogType.CreateKit, newKitId + " COPIED FROM " + sourceKit.Id);
        Context.Reply(_translations.KitCopied, sourceKit, kit);
    }

    private void CopyKit(KitModel kit, Kit source)
    {
        kit.Class = source.Class;
        kit.Branch = source.Branch;
        kit.Type = source.Type;
        kit.MinRequiredSquadMembers = source.MinRequiredSquadMembers;
        kit.RequiresSquad = source.RequiresSquad;
        kit.CreditCost = source.CreditCost;
        kit.PremiumCost = source.PremiumCost;
        kit.SquadLevel = source.SquadLevel;
        kit.FactionId = source.Faction.PrimaryKey;
        kit.Disabled = source.IsLocked;
        kit.FactionFilterIsWhitelist = source.FactionFilterIsWhitelist;
        kit.MapFilterIsWhitelist = source.MapFilterIsWhitelist;
        kit.RequestCooldown = (float)source.RequestCooldown.TotalSeconds;
        kit.RequiresNitro = source.RequiresServerBoost;
        kit.LastEditedAt = source.LastEditedTimestamp;
        kit.LastEditor = source.LastEditingPlayer.m_SteamID;
        kit.Weapons = source.WeaponText;

        foreach (IKitItem item in source.Items)
        {
            KitItemModel model = new KitItemModel { KitId = kit.PrimaryKey };
            KitItemUtility.CreateKitItemModel(item, model);
            model.Id = 0;
            kit.Items.Add(model);
        }

        foreach (Skillset skillset in source.Skillsets)
        {
            KitSkillset model = new KitSkillset
            {
                KitId = kit.PrimaryKey,
                Skillset = skillset
            };
            kit.Skillsets.Add(model);
        }

        foreach (FactionInfo faction in source.FactionFilter)
        {
            KitFilteredFaction model = new KitFilteredFaction
            {
                KitId = kit.PrimaryKey,
                FactionId = faction.PrimaryKey
            };
            kit.FactionFilter.Add(model);
        }

        foreach (uint map in source.MapFilter)
        {
            KitFilteredMap model = new KitFilteredMap
            {
                KitId = kit.PrimaryKey,
                Map = map
            };
            kit.MapFilter.Add(model);
        }

        foreach (ILayoutDelay<LayoutDelayContext> delay in source.Delays)
        {
            KitDelay model = new KitDelay
            {
                KitId = kit.PrimaryKey,
                Type = delay.GetType().AssemblyQualifiedName!,
                Data = JsonSerializer.Serialize(delay, ConfigurationSettings.JsonCondensedSerializerSettings)
            };
            kit.Delays.Add(model);
        }

        foreach (UnlockRequirement delay in source.Delays)
        {
            KitUnlockRequirement model = new KitUnlockRequirement
            {
                KitId = kit.PrimaryKey,
                Type = delay.GetType().AssemblyQualifiedName!,
                Data = JsonSerializer.Serialize(delay, ConfigurationSettings.JsonCondensedSerializerSettings)
            };
            kit.UnlockRequirements.Add(model);
        }

        foreach (KeyValuePair<string, string> translation in source.Translations)
        {
            LanguageInfo? lang = string.IsNullOrEmpty(translation.Key)
                ? _languageService.GetDefaultLanguage()
                : _languageSql.GetInfoCached(translation.Key);

            if (lang == null)
                continue;

            KitTranslation model = new KitTranslation
            {
                KitId = kit.PrimaryKey,
                LanguageId = lang.Key,
                Value = translation.Value
            };

            kit.Translations.Add(model);
        }
    }
}