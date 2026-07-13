using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Globalization;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Players.Skillsets;

/// <summary>
/// Configuration file for the base skills given to all players.
/// </summary>
public sealed class DefaultSkillsetConfiguration : BaseAlternateConfigurationFile
{
    private readonly ILogger<DefaultSkillsetConfiguration> _logger;

    /// <summary>
    /// List of skillsets that should be applied as a base to all kits.
    /// </summary>
    public ImmutableArray<Skillset> DefaultSkillsets { get; private set; }

    public DefaultSkillsetConfiguration(IServiceProvider serviceProvider, ILogger<DefaultSkillsetConfiguration> logger)
        : base(serviceProvider, "Skills.yml", optional: true)
    {
        _logger = logger;
        DefaultSkillsets = ImmutableArray<Skillset>.Empty;

        if (FilePath != null)
            HandleChange();
    }

    protected override void HandleChange()
    {
        ImmutableArray<Skillset>.Builder bldr = ImmutableArray.CreateBuilder<Skillset>();
        foreach (IConfigurationSection skill in GetChildren())
        {
            if (!byte.TryParse(skill.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out byte level))
            {
                _logger.LogWarning($"Missing/invalid level for skill {skill.Key}.");
                continue;
            }

            int index = Skillset.GetSkillsetFromEnglishName(skill.Key, out EPlayerSpeciality speciality);
            if (index < 0)
            {
                _logger.LogWarning($"Unknown skill: {skill.Key}.");
                continue;
            }

            bldr.Add(new Skillset(speciality, (byte)index, level));
        }

        DefaultSkillsets = bldr.DrainToImmutable();
    }
}