using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Vehicle;

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
    public TickResponsibilityCollection Builders { get; }
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
        Builders = new TickResponsibilityCollection();
        _serviceProvider = serviceProvider;
        _sessionId = Guid.NewGuid();
        
        if (Info.Emplacement?.Vehicle != null)
            IdentifyingAsset = Info.Emplacement.Vehicle;
        else if (Info.CompletedStructure != null)
            IdentifyingAsset = Info.CompletedStructure;
        else
            throw new AssetNotFoundException("ShoveableInfo has neither a CompletedStructure nor an Emplacement.Vehicle asset.");
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

        _serviceProvider.GetService<FobManager>()?.RegisterFobEntity(new EmplacementEntity(vehicle, auxilliaryStructure));
    }

    public bool Shovel(WarfarePlayer shoveler, Vector3 point)
    {
        if (IsCompleted)
            return false;

        HitsRemaining--;
        if (shoveler.CurrentSession != null)
            Builders.AddItem(new TickResponsibility(shoveler.Steam64.m_SteamID, shoveler.CurrentSession.SessionId, 1));

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
        return true;
    }
}
