using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;
public interface IAssetRedirectKitItem : IKitItem
{
    RedirectType RedirectType { get; }
    string? RedirectVariant { get; }
}