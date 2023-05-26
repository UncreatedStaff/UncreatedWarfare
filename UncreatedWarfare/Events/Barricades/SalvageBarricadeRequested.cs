using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Events.Structures;
public sealed class SalvageBarricadeRequested : SalvageRequested
{
    private readonly BarricadeDrop _drop;
    private readonly BarricadeData _data;
    private readonly ushort _plant;
    public ushort Plant => _plant;
    public BarricadeDrop Barricade => _drop;
    public BarricadeData ServersideData => _data;
    public BarricadeRegion Region => (BarricadeRegion)RegionObj;
    public override IBuildable Buildable => BuildableCache ??= new UCBarricade(Barricade);
    internal SalvageBarricadeRequested(UCPlayer instigator, BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant, SqlItem<SavedStructure>? save)
        : base(instigator, region, x, y, barricade.instanceID)
    {
        _drop = barricade;
        _data = barricadeData;
        _plant = plant;
        Transform = barricade.model;
        StructureSave = save;
        ListSqlConfig<SavedStructure>? m = save?.Manager;
        if (m is not null)
        {
            m.WriteWait();
            try
            {
                if (save!.Item != null)
                {
                    BuildableCache = save.Item.Buildable;
                    IsStructureSaved = true;
                }
            }
            finally
            {
                m.WriteRelease();
            }
        }
    }
}