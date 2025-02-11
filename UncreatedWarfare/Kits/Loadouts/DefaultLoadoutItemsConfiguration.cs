using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Loadouts;

/// <summary>
/// Stores default items for loadout types.
/// </summary>
public sealed class DefaultLoadoutItemsConfiguration : BaseAlternateConfigurationFile
{
    private const Class MinimumClass = Class.Unarmed;

    private readonly ILogger<DefaultLoadoutItemsConfiguration> _logger;
    private readonly IReadOnlyList<IItem>[] _configuration;

    public DefaultLoadoutItemsConfiguration(ILogger<DefaultLoadoutItemsConfiguration> logger)
        : base(Path.Combine("Kits", "Default Items.yml"))
    {
        _logger = logger;
        _configuration = new IReadOnlyList<IItem>[EnumUtility.GetMaximumValue<Class>() - MinimumClass + 1];
        HandleChange();
    }

    /// <summary>
    /// Get a list of default items for a loadout given a starting class.
    /// </summary>
    public IReadOnlyList<IItem> GetDefaultsForClass(Class @class)
    {
        int index = (int)@class - (int)MinimumClass;
        if (index < 0 || index >= _configuration.Length)
            return Array.Empty<IItem>();

        return _configuration[index];
    }

    protected override void HandleChange()
    {
        List<IItem> buffer = new List<IItem>(16);
        Array.Clear(_configuration, 0, _configuration.Length);

        using IDisposable? scope = _logger.BeginScope("HandleChange");
        _logger.LogDebug("Reloading default loadout configuration...");

        foreach (IConfigurationSection section in UnderlyingConfiguration.GetChildren())
        {
            if (!ClassExtensions.TryParseClass(section.Key, out Class @class))
            {
                _logger.LogWarning($"Unknown class at {section.Path}.");
                continue;
            }

            int index = (int)@class - (int)MinimumClass;
            if (index < 0 || index >= _configuration.Length)
            {
                _logger.LogWarning($"Invalid class at {section.Path}.");
                continue;
            }

            _configuration[index] = new ReadOnlyCollection<IItem>(ReadConfiguration(section, buffer));
            buffer.Clear();
        }

        for (int i = 0; i < _configuration.Length; ++i)
        {
            ref IReadOnlyList<IItem>? items = ref _configuration[i];

            Class @class = (Class)((int)MinimumClass + i);

            if (items is { Count: > 0 } || !EnumUtility.ValidateValidField(@class))
                continue;

            items = Array.Empty<IItem>();
            _logger.LogWarning($"Class {@class} missing from configuration.");
        }
    }

    private IItem[] ReadConfiguration(IConfigurationSection section, List<IItem> buffer)
    {
        foreach (IConfigurationSection item in section.GetChildren())
        {
            IItem? kitItem = KitItemUtility.ReadItem(item);
            if (kitItem != null)
                buffer.Add(kitItem);
            else
                _logger.LogWarning($"Invalid item at {item.Path}.");
        }

        return buffer.ToArray();
    }
}