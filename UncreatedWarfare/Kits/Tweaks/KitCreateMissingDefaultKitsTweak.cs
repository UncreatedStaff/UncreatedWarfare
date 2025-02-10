using DanielWillett.ReflectionTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Tweaks;

/// <summary>
/// Creates the 'default' kit and all unarmed kits at the start of a game.
/// </summary>
[Priority(10)]
internal sealed class KitCreateMissingDefaultKitsTweak : ILayoutHostedService
{
    private const string DefaultKitId = "default";

    private readonly ITeamManager<Team> _teamManager;
    private readonly IFactionDbContext _factionDbContext;
    private readonly IKitDataStore _kitSql;
    private readonly ILogger<KitCreateMissingDefaultKitsTweak> _logger;
    private readonly DefaultLoadoutItemsConfiguration _defaultItemsConfig;

    public KitCreateMissingDefaultKitsTweak(IServiceProvider serviceProvider, ILogger<KitCreateMissingDefaultKitsTweak> logger)
    {
        _logger = logger;

        _defaultItemsConfig = serviceProvider.GetRequiredService<DefaultLoadoutItemsConfiguration>();
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _kitSql = serviceProvider.GetRequiredService<IKitDataStore>();
        _factionDbContext = serviceProvider.GetRequiredService<IFactionDbContext>();
        _factionDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <inheritdoc />
    public async UniTask StartAsync(CancellationToken token)
    {
        foreach (Team team in _teamManager.AllTeams)
        {
            if (team.Faction.IsDefaultFaction)
                continue;

            uint? unarmedKit = team.Faction.UnarmedKit;
            Faction? factionModel;
            string kitName = team.Faction.KitPrefix + "unarmed";

            if (unarmedKit.HasValue)
            {
                uint pk = unarmedKit.Value;
                KitModel? kit = await _kitSql.QueryFirstAsync(kits => kits.Where(k => k.PrimaryKey == pk), KitInclude.FactionFilter, token).ConfigureAwait(false);

                if (kit != null && !NeedsUpdate(kit, team.Faction))
                    continue;

                factionModel = await _factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == team.Faction.PrimaryKey, token).ConfigureAwait(false);

                // kit is saved but doesn't have the right ID
                if (kit == null || !string.Equals(kit.Id, kitName, StringComparison.Ordinal))
                {
                    KitModel? otherKit = await _kitSql.QueryFirstAsync(kits => kits.Where(k => k.Id == kitName), KitInclude.FactionFilter, token).ConfigureAwait(false);
                    if (otherKit != null)
                        kit = otherKit;
                }

                if (kit != null)
                {
                    if (factionModel != null)
                    {
                        factionModel.UnarmedKitId = kit.PrimaryKey;
                        _factionDbContext.Update(factionModel);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to update unarmed kit for faction {team.Faction.Name}. (0x1)");
                    }

                    team.Faction.UnarmedKit = kit.PrimaryKey;
                    if (NeedsUpdate(kit, team.Faction))
                    {
                        await UpdateKit(kit, team.Faction).ConfigureAwait(false);
                    }
                    _logger.LogWarning($"Team {team.Faction.Name}'s unarmed kit wasn't configured or needed an update but a possible match was found with ID: {kit.Id}, using that one instead.");
                    continue;
                }

                _logger.LogWarning($"Team {team.Faction.Name}'s unarmed kit \"{unarmedKit.Value}\" was not found, an attempt will be made to auto-generate one.");
            }
            else
            {
                factionModel = await _factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == team.Faction.PrimaryKey, token).ConfigureAwait(false);
                KitModel? existing = await _kitSql.QueryFirstAsync(kits => kits.Where(k => k.Id == kitName), KitInclude.FactionFilter, token).ConfigureAwait(false);
                if (existing != null)
                {
                    if (factionModel != null)
                    {
                        factionModel.UnarmedKitId = existing.PrimaryKey;
                        _factionDbContext.Update(factionModel);
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to update unarmed kit for faction {team.Faction.Name}. (0x2)");
                    }

                    team.Faction.UnarmedKit = existing.PrimaryKey;
                    if (NeedsUpdate(existing, team.Faction))
                    {
                        await UpdateKit(existing, team.Faction).ConfigureAwait(false);
                    }
                    _logger.LogWarning($"Team {team.Faction.Name}'s unarmed kit wasn't configured but a possible match was found with ID: {existing.Id}, using that one instead.");
                    continue;
                }

                _logger.LogWarning($"Team {team.Faction.Name}'s unarmed kit hasn't been configured, an attempt will be made to auto-generate one.");
            }

            Kit newKit = await CreateDefaultKit(team.Faction, kitName, token).ConfigureAwait(false);
            team.Faction.UnarmedKit = newKit.Key;
            if (factionModel != null)
            {
                factionModel.UnarmedKitId = newKit.Key;
                _factionDbContext.Update(factionModel);
            }
            else
            {
                _logger.LogWarning($"Unable to update unarmed kit for faction {team.Faction.Name}. (0x3)");
            }
        }

        try
        {
            await _factionDbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating faction table with new unarmed kit(s).");
        }

        Kit? defaultKit = await _kitSql.QueryKitAsync(DefaultKitId, KitInclude.Base, token).ConfigureAwait(false);
        if (defaultKit != null)
            return;
        
        _logger.LogWarning($"The overall default kit \"{DefaultKitId}\" was not found, an attempt will be made to auto-generate one.");
        defaultKit = await CreateDefaultKit(null, DefaultKitId, token);
        _logger.LogInformation($"Created default kit: \"{defaultKit.Id}\" ({defaultKit.Key}).");
    }

    private Task UpdateKit(KitModel kit, FactionInfo? faction)
    {
        return _kitSql.UpdateKitAsync(kit.PrimaryKey, KitInclude.Items | KitInclude.FactionFilter, model =>
        {
            model.Type = KitType.Special;
            
            UpdateFactionFilter(model, faction);
            UpdateItems(model, faction);
        });
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;

    private bool NeedsUpdate(KitModel kit, FactionInfo? faction)
    {
        if (kit.Season < WarfareModule.Season)
            return true;

        if (faction == null && (kit.FactionFilter.Count > 0 || kit.FactionFilterIsWhitelist))
            return true;
        
        if (faction != null && (kit.FactionFilter.Count != 1 || kit.FactionFilter[0].FactionId != faction.PrimaryKey || !kit.FactionFilterIsWhitelist))
            return true;

        return false;
    }

    private Task<Kit> CreateDefaultKit(FactionInfo? faction, string id, CancellationToken token)
    {
        return _kitSql.AddKitAsync(id, Class.Unarmed,
            faction != null ? faction.NameTranslations.Translate(null) + " Default" : "Default", CSteamID.Nil,
            model =>
            {
                model.Type = KitType.Special;

                UpdateItems(model, faction);
                UpdateFactionFilter(model, faction);

            }, token);
    }

    private void UpdateItems(KitModel model, FactionInfo? faction)
    {
        IReadOnlyList<IItem> items = _defaultItemsConfig.GetDefaultsForClass(Class.Unarmed);

        foreach (IItem item in items)
        {
            if (item is IRedirectedItem && faction == null)
                continue;

            if (item is IClothingItem clothing && model.Items.Exists(x => x.ClothingSlot.HasValue && x.ClothingSlot.Value == clothing.ClothingType))
                continue;

            KitItemModel itemModel = new KitItemModel();
            KitItemUtility.CreateKitItemModel(item, itemModel);
            model.Items.Add(itemModel);
        }
    }

    private void UpdateFactionFilter(KitModel model, FactionInfo? faction)
    {
        model.FactionFilter.Clear();
        if (faction != null)
        {
            model.FactionFilter.Add(new KitFilteredFaction
            {
                FactionId = faction.PrimaryKey
            });
        }
        else
        {
            model.FactionFilterIsWhitelist = false;
        }

        model.FactionFilterIsWhitelist = true;
    }
}