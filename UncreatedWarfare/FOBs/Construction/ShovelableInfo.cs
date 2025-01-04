using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.FOBs.Construction;
public class ShovelableInfo : ITranslationArgument
{
    public required IAssetLink<ItemPlaceableAsset> Foundation { get; set; }
    public required ShovelableType ConstuctionType { get; set; }
    public required int SupplyCost { get; set; }
    public int? MaxAllowedPerFob { get; set; }
    public IAssetLink<ItemPlaceableAsset>? CompletedStructure { get; set; }
    public IAssetLink<EffectAsset>? CompletedEffect { get; set; }
    public EmplacementInfo? Emplacement { get; set; }
    public override string ToString()
    {
        return $"ShovelableInfo:\n" +
               $"  Foundation: {Foundation}\n" +
               $"  ConstructionType: {ConstuctionType}\n" +
               $"  RequiredHits: {SupplyCost}\n" +
               $"  CompletedStructure: {(CompletedStructure?.ToString() ?? "None")}\n" +
               $"  CompletedEffect: {(CompletedEffect?.ToString() ?? "None")}\n" +
               $"  Emplacement: {(Emplacement?.ToString() ?? "None")}";
    }

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        if (Emplacement != null)
            return formatter.Format(Emplacement.Vehicle.GetAssetOrFail(), in parameters);

        return formatter.Format(CompletedStructure.GetAssetOrFail(), in parameters);
    }
}
