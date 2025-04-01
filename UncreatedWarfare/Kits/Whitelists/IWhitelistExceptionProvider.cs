using Uncreated.Framework.UI;

namespace Uncreated.Warfare.Kits.Whitelists;
public interface IWhitelistExceptionProvider
{
    /// <summary>
    /// Get the amount value on whitelists.
    /// </summary>
    /// <returns>0 if the item isn't whitelisted, -1 if it's infinite, otherwise the amount.</returns>
    ValueTask<int> GetWhitelistAmount(IAssetContainer assetContainer);
}
