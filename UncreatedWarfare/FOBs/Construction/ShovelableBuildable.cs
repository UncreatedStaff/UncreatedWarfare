using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs.Shovelables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.FOBs.Construction;

public class ShovelableBuildable : IBuildableFobEntity
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EffectAsset? _shovelEffect;
    public ShovelableInfo Info { get; }
    public IBuildable Buildable { get; }
    public int HitsRemaining { get; private set; }
    public bool IsCompleted => HitsRemaining <= 0;
    public bool IsEmplacement => Info.Emplacement != null;
    public PlayerContributionTracker Builders { get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;
    public bool PreventItemDrops => false;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public event Action<IBuildable?>? OnComplete;

    public ShovelableBuildable(ShovelableInfo info, IBuildable foundation, IServiceProvider serviceProvider, IAssetLink<EffectAsset>? shovelEffect = null)
    {
        Info = info;
        Buildable = foundation;
        _shovelEffect = shovelEffect?.GetAssetOrFail();
        HitsRemaining = info.SupplyCost;
        Builders = new PlayerContributionTracker();
        _serviceProvider = serviceProvider;

        IdentifyingAsset = Info.Foundation;
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

                await _serviceProvider.GetRequiredService<VehicleService>().SpawnVehicleAsync(
                    emplacementInfo.Vehicle,
                    position,
                    rotation,
                    Buildable.Owner,
                    Buildable.Group
                );
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

        float progressPercent = 1 - (float)HitsRemaining / Info.SupplyCost;
        int barCharactersToWrite = Mathf.RoundToInt(progressPercent * 25); // the toast UI has 25 characters
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