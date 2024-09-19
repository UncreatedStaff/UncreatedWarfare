using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models;

[Table("item_whitelists")]
public class ItemWhitelist
{
    [Key]
    [Column("pk")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PrimaryKey { get; set; }

    public UnturnedAssetReference Item { get; set; }

    /// <summary>
    /// Maximum number of items a player can have on them at once.
    /// </summary>
    public int Amount { get; set; }
}