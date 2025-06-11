using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners.Delays;

namespace Uncreated.Warfare.Models.Base;

public abstract class BaseDelay : ICloneable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required, StringLength(512)]
    public string Data { get; set; }

    [Required, StringLength(128)]
    public string Type { get; set; }

    protected BaseDelay() { }

    [SetsRequiredMembers]
    protected BaseDelay(BaseDelay other)
    {
        Id = other.Id;
        Type = other.Type;
        Data = other.Data;
    }

    /// <summary>
    /// Read this delay and deserialize the data for it.
    /// </summary>
    /// <exception cref="JsonException">Failed to read JSON data.</exception>
    /// <exception cref="FormatException">Unknown or invalid type.</exception>
    public ILayoutDelay<LayoutDelayContext>? CreateDelay()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Data);
        Utf8JsonReader reader = new Utf8JsonReader(bytes, ConfigurationSettings.JsonReaderOptions);
    
        Type? delayType = ContextualTypeResolver.ResolveType(Type, typeof(ILayoutDelay<LayoutDelayContext>));

        if (delayType == null)
            return null;

        return (ILayoutDelay<LayoutDelayContext>?)JsonSerializer.Deserialize(ref reader, delayType, ConfigurationSettings.JsonCondensedSerializerSettings);
    }

    /// <inheritdoc />
    public abstract object Clone();
}