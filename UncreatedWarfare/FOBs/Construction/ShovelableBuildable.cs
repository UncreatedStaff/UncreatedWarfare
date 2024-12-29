using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Fobs.Shovelables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.Construction;

public class ShovelableBuildable : IBuildableFobEntity
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EffectAsset? _shovelEffect;
    private readonly Guid _sessionId;
    public ShovelableInfo Info { get; }
    public IBuildable Buildable { get; }
    public int HitsRemaining { get; private set; }
    public bool IsCompleted => HitsRemaining <= 0;
    public bool IsEmplacement => Info.Emplacement != null;
    public PlayerContributionTracker Builders { get; }
    public Action<IBuildable?>? OnComplete { get; set; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public ShovelableBuildable(ShovelableInfo info, IBuildable foundation, IServiceProvider serviceProvider, IAssetLink<EffectAsset>? shovelEffect = null)
    {
        Info = info;
        Buildable = foundation;
        _shovelEffect = shovelEffect?.GetAssetOrFail();
        HitsRemaining = info.SupplyCost;
        Builders = new PlayerContributionTracker();
        _serviceProvider = serviceProvider;
        _sessionId = Guid.NewGuid();

        IdentifyingAsset = Info.Foundation;
    }

    public virtual void Complete(WarfarePlayer shoveler)
    {
        IBuildable? completedBuildable = null;
        if (Info.CompletedStructure.TryGetAsset(out ItemPlaceableAsset? completedAsset))
        {
            if (completedAsset is not ItemBarricadeAsset barricadeAsset)
                throw new NotSupportedException("Shoveable structures are not yet supported.");

            // drop the barricade
            Transform transform = BarricadeManager.dropNonPlantedBarricade(
                new Barricade(barricadeAsset),
                Buildable.Position,
                Buildable.Rotation,
                Buildable.Owner.m_SteamID,
                Buildable.Group.m_SteamID
            );
            completedBuildable = new BuildableBarricade(BarricadeManager.FindBarricadeByRootTransform(transform));
        }

        if (Info.Emplacement != null)
            DropEmplacement(Info.Emplacement, completedBuildable);

        if (Info.CompletedEffect != null)
        {
            EffectManager.triggerEffect(new TriggerEffectParameters(Info.CompletedEffect.GetAssetOrFail())
            {
                position = Buildable.Position,
                relevantDistance = 70,
                reliable = true
            });
        }

        OnComplete?.Invoke(completedBuildable);

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new ShovelableBuilt { Shovelable = this });

        Buildable.Destroy(); // make sure to only destroy the foundation events are invoked
    }

    private async void DropEmplacement(EmplacementInfo emplacementInfo, IBuildable? auxilliaryStructure = null)
    {
        emplacementInfo.Vehicle.AssertValid();

        WarfareVehicle vehicle = await _serviceProvider.GetRequiredService<VehicleService>().SpawnVehicleAsync(
            emplacementInfo.Vehicle,
            new Vector3(Buildable.Position.x, Buildable.Position.y + 2, Buildable.Position.z),
            // rotate x + 90 degrees because nelson sucks
            Quaternion.Euler(Buildable.Rotation.eulerAngles.x + 90, Buildable.Rotation.eulerAngles.y, Buildable.Rotation.eulerAngles.z),
            Buildable.Owner,
            Buildable.Group);
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
        EffectManager.triggerEffect(new TriggerEffectParameters(_shovelEffect)
        {
            position = point,
            relevantDistance = 70,
            reliable = true
        });

        SendProgressToast(shoveler);

        return true;
    }
    private void SendProgressToast(WarfarePlayer shoveler)
    {
        if (!shoveler.TryGetFromContainer(out ToastManager? toastManager))
            return;

        float progressPercent = 1 - (float)(HitsRemaining) / Info.SupplyCost;
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

    public void Dispose()
    {
        // don't need to dispose anything
    }
}
