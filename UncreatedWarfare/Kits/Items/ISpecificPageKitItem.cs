namespace Uncreated.Warfare.Kits.Items;
public interface ISpecificPageKitItem : ISpecificKitItem, IPageKitItem
{
    byte Amount { get; }
}