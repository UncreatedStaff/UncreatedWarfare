using SDG.Unturned;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Buildables;

namespace Uncreated.Warfare.Events.Barricades;
public sealed class SalvageBarricadeRequested : SalvageRequested
{
    private readonly BarricadeDrop _drop;
    private readonly BarricadeData _data;
    private readonly ushort _plant;
    public ushort Plant => _plant;
    public BarricadeDrop Barricade => _drop;
    public BarricadeData ServersideData => _data;
    public BarricadeRegion Region => (BarricadeRegion)RegionObj;
    public override IBuildable Buildable => BuildableCache ??= new BuildableBarricade(Barricade);
    internal SalvageBarricadeRequested(UCPlayer instigator, BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant, BuildableSave? save, UnturnedAssetReference primaryAsset, UnturnedAssetReference secondaryAsset)
        : base(instigator, region, x, y, barricade.instanceID, primaryAsset, secondaryAsset)
    {
        _drop = barricade;
        _data = barricadeData;
        _plant = plant;
        Transform = barricade.model;
        BuildableSave = save;
        BuildableCache = save?.Buildable;
    }
}