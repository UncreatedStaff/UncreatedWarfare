using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs.Shovelables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.Construction;

public class ShovelableBuildable : BuildableFobEntity<ShovelableInfo>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EffectAsset? _shovelEffect;

    public int HitsRemaining
    {
        get;
        set
        {
            if (value < 0)
                field = 0;
            else
                field = value;
        }
    }

    public bool IsCompleted => HitsRemaining <= 0;
    public bool IsEmplacement => Info.Emplacement != null;
    public PlayerContributionTracker Builders { get; }
    public new ShovelableInfo Info => base.Info!;

    public event Action<IBuildable?>? OnComplete;

    public ShovelableBuildable(Team team, ShovelableInfo info, IBuildable foundation, IServiceProvider serviceProvider, IAssetLink<EffectAsset>? shovelEffect = null)
        : base(info ?? throw new ArgumentNullException(nameof(info)), foundation, team, serviceProvider, "Effects:Fobs:Shovel")
    {
        _shovelEffect = shovelEffect?.GetAssetOrFail();
        HitsRemaining = info.RequiredShovelHits;
        Builders = new PlayerContributionTracker();
        _serviceProvider = serviceProvider;
    }

    public virtual void Complete(WarfarePlayer shoveler)
    {
        IBuildable? completedBuildable = null;
        if (Info.CompletedStructure.TryGetAsset(out ItemPlaceableAsset? completedAsset))
        {
            // drop the barricade
            completedBuildable = Buildable.ReplaceBuildable(completedAsset, destroyOld: false);
        }

        if (Info.Emplacement != null)
            DropEmplacement(Info.Emplacement);

        if (Info.CompletedEffect != null)
        {
            EffectUtility.TriggerEffect(Info.CompletedEffect.GetAssetOrFail(), 70, Buildable.Position, reliable: true);
        }

        try
        {
            OnComplete?.Invoke(completedBuildable);
        }
        catch (Exception ex)
        {
            _serviceProvider.GetRequiredService<ILogger<ShovelableBuildable>>().LogError(ex, "Error invoking OnComplete.");
        }

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new ShovelableBuilt { Shovelable = this });

        DestroyIcon();

        Buildable.Destroy(); // make sure to only destroy the foundation events are invoked
    }

    private void DropEmplacement(EmplacementInfo emplacementInfo)
    {
        emplacementInfo.Vehicle.AssertValid();

        // async void eats exceptions
        UniTask.Create(async () =>
        {
            try
            {
                Vector3 position = Buildable.Position + Vector3.up * FobManager.EmplacementSpawnOffset;
                Quaternion rotation = Buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

                WarfareVehicle warfareVehicle = await _serviceProvider.GetRequiredService<VehicleService>().SpawnVehicleAsync(
                    emplacementInfo.Vehicle,
                    position,
                    rotation,
                    Buildable.Owner,
                    Buildable.Group
                );

                foreach (var itemAsset in warfareVehicle.Info.StartingItems)
                {
                    ItemManager.dropItem(new Item(itemAsset.GetAssetOrFail(), EItemOrigin.ADMIN), position, false, true, true);
                }
            }
            catch (Exception ex)
            {
                _serviceProvider.GetRequiredService<ILogger<ShovelableBuildable>>().LogError(ex, "Error spawning vehicle.");
            }
        });
    }

    public bool Shovel(WarfarePlayer shoveler, Vector3 point)
    {
        if (IsCompleted)
            return false;

        if (shoveler.Component<KitPlayerComponent>().ActiveClass == Class.CombatEngineer)
            HitsRemaining -= 2;
        else
            HitsRemaining--;
        
        if (shoveler.CurrentSession != null)
            Builders.RecordWork(shoveler.Steam64, 1);

        if (IsCompleted)
        {
            Complete(shoveler);
        }

        if (_shovelEffect != null)
        {
            EffectUtility.TriggerEffect(_shovelEffect, 70, point, reliable: true);
        }

        SendProgressToast(shoveler);

        return true;
    }

    private void SendProgressToast(WarfarePlayer shoveler)
    {
        if (!shoveler.TryGetFromContainer(out ToastManager? toastManager))
            return;

        float progressPercent = Mathf.Clamp(1 - (float)HitsRemaining / Info.RequiredShovelHits, 0, 1);
        int barCharactersToWrite = Mathf.RoundToInt(progressPercent * 29); // the toast UI has 29 characters
        toastManager.Queue(new ToastMessage(ToastMessageStyle.ProgressBar, new string('█', barCharactersToWrite)));
    }

    public override bool Equals(object? obj)
    {
        return obj is ShovelableBuildable shoveableBuildable && Buildable.Equals(shoveableBuildable.Buildable);
    }

    public override int GetHashCode()
    {
        return Buildable.GetHashCode();
    }
}