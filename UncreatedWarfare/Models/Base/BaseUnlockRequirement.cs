using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Models.Base;
public abstract class BaseUnlockRequirement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Json { get; set; } = null!;
    public UnlockRequirement? CreateRuntimeRequirement(ILogger logger, IServiceProvider serviceProvider)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Json);
        Utf8JsonReader reader = new Utf8JsonReader(bytes, ConfigurationSettings.JsonReaderOptions);
        UnlockRequirement? requirement = UnlockRequirement.Read(logger, serviceProvider, ref reader);
        if (requirement != null)
            requirement.PrimaryKey = Id;
        return requirement;
    }
}
