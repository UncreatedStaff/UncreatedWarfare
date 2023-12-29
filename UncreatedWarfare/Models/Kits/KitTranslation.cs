using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_sign_text")]
public class KitTranslation : BaseTranslation, ICloneable
{
    [Required]
    [JsonIgnore]
    public Kit Kit { get; set; }

    [ForeignKey(nameof(Kit))]
    [Required]
    [Column("Kit", Order = 1)]
    public uint KitId { get; set; }

    public object Clone()
    {
        return new KitTranslation
        {
            KitId = KitId,
            Kit = Kit,
            Value = Value,
            Language = Language,
            LanguageId = LanguageId
        };
    }
}
