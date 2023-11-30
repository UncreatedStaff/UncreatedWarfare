namespace Uncreated.Warfare.Kits.Items;
public interface IPageKitItem : IKitItem
{
    byte X { get; }
    byte Y { get; }
    byte Rotation { get; }
    Page Page { get; }
}