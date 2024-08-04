using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models;

[Table("item_whitelists")]
public class ItemWhitelist
{
    [Column("pk")]
    public int PrimaryKey { get; set; }

    public UnturnedAssetReference Item { get; set; }

    /// <summary>
    /// Maximum number of items a player can have on them at once.
    /// </summary>
    public int Amount { get; set; }
}