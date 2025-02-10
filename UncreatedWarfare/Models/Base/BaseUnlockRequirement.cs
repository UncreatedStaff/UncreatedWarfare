using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Models.Base;

public abstract class BaseUnlockRequirement : ICloneable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    [StringLength(128)]
    public required string Type { get; set; }

    [Required]
    [StringLength(512)]
    public required string Data { get; set; }

    protected BaseUnlockRequirement() { }

    /// <summary>
    /// Read this requirement and deserialize the data for it.
    /// </summary>
    /// <exception cref="JsonException">Failed to read JSON data.</exception>
    /// <exception cref="FormatException">Unknown or invalid type.</exception>
    public UnlockRequirement? CreateUninitializedRequirement()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Data);
        Utf8JsonReader reader = new Utf8JsonReader(bytes, ConfigurationSettings.JsonReaderOptions);

        Type? unlockRequirement = ContextualTypeResolver.ResolveType(Type, typeof(UnlockRequirement));

        UnlockRequirement? requirement = unlockRequirement != null
            ? (UnlockRequirement?)JsonSerializer.Deserialize(ref reader, unlockRequirement, ConfigurationSettings.JsonCondensedSerializerSettings)
            : null;

        if (requirement == null)
            return null;

        requirement.PrimaryKey = Id;
        return requirement;
    }

    /// <inheritdoc />
    public abstract object Clone();
}