using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_hotkeys")]
public class KitHotkey
{
    public ulong Steam64 { get; set; }

    [Required]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }
    public byte Slot { get; set; }
    public Page Page { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public UnturnedAssetReference? Item { get; set; }
    public RedirectType? Redirect { get; set; }
}