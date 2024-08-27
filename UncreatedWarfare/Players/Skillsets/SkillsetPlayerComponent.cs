using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Skillsets;
public class SkillsetPlayerComponent : IPlayerComponent
{
    private ILogger<SkillsetPlayerComponent> _logger = null!;
    public WarfarePlayer Player { get; private set; }
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<SkillsetPlayerComponent>>();
    }

    /// <summary>
    /// Set the given skill back to it's default value.
    /// </summary>
    public void ResetSkillset(EPlayerSpeciality speciality, byte skill)
    {
        GameThread.AssertCurrent();

        if (!Player.IsOnline)
            return;

        Skill[][] skills = Player.UnturnedPlayer.skills.skills;
        if ((int)speciality >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(speciality), "Speciality index is out of range.");
        
        if (skill >= skills[(int)speciality].Length)
            throw new ArgumentOutOfRangeException(nameof(skill), "Skill index is out of range.");
        
        Skill skillObj = skills[(int)speciality][skill];
        Skillset[] def = Skillset.DefaultSkillsets;
        for (int d = 0; d < def.Length; ++d)
        {
            Skillset s = def[d];
            if (s.Speciality != speciality || s.SkillIndex != skill)
                continue;
            
            if (s.Level != skillObj.level)
            {
                _logger.LogDebug("Setting server default: {0}.", s);
                s.ServerSet(Player);
            }
            else
                _logger.LogDebug("Server default already set: {0}.", s);

            return;
        }

        byte defaultLvl = GetDefaultSkillLevel(speciality, skill);
        if (skillObj.level != defaultLvl)
        {
            Player.UnturnedPlayer.skills.ServerSetSkillLevel((int)speciality, skill, defaultLvl);
            _logger.LogDebug("Setting game default: {0}.", new Skillset(speciality, skill, defaultLvl));
        }
        else
        {
            _logger.LogDebug("Game default already set: {0}.", new Skillset(speciality, skill, defaultLvl));
        }
    }

    /// <summary>
    /// Update the player's skill value for the given skillset.
    /// </summary>
    public void EnsureSkillset(Skillset skillset)
    {
        GameThread.AssertCurrent();
        if (!Player.IsOnline)
            return;

        Skill[][] skills = Player.UnturnedPlayer.skills.skills;
        if (skillset.SpecialityIndex >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(skillset), "Speciality index is out of range.");

        if (skillset.SkillIndex >= skills[skillset.SpecialityIndex].Length)
            throw new ArgumentOutOfRangeException(nameof(skillset), "Skill index is out of range.");

        Skill skill = skills[skillset.SpecialityIndex][skillset.SkillIndex];
        if (skillset.Level != skill.level)
        {
            skillset.ServerSet(Player);
        }
    }

    /// <summary>
    /// Update the player's skill values to match <see cref="Skillset.DefaultSkillsets"/>.
    /// </summary>
    public void EnsureDefaultSkillsets() => EnsureSkillsets(Array.Empty<Skillset>());

    /// <summary>
    /// Update the player's skill values to match the given list of skillset overrides.
    /// </summary>
    /// <remarks>If a value isn't overridden, the values from <see cref="Skillset.DefaultSkillsets"/> will be used.</remarks>
    public void EnsureSkillsets(IEnumerable<Skillset> skillsets)
    {
        GameThread.AssertCurrent();
        if (!Player.IsOnline)
            return;

        Skillset[] def = Skillset.DefaultSkillsets;
        Skillset[] arr = skillsets as Skillset[] ?? skillsets.ToArray();
        Skill[][] skills = Player.UnturnedPlayer.skills.skills;
        for (int specIndex = 0; specIndex < skills.Length; ++specIndex)
        {
            Skill[] specialtyArr = skills[specIndex];
            for (int skillIndex = 0; skillIndex < specialtyArr.Length; ++skillIndex)
            {
                Skill skill = specialtyArr[skillIndex];
                bool found = false;
                for (int i = 0; i < arr.Length; ++i)
                {
                    ref Skillset s = ref arr[i];
                    if (s.SpecialityIndex != specIndex || s.SkillIndex != skillIndex)
                        continue;

                    if (s.Level != skill.level)
                        s.ServerSet(Player);

                    found = true;
                }

                if (found)
                    continue;

                for (int d = 0; d < def.Length; ++d)
                {
                    ref Skillset s = ref def[d];
                    if (s.SpecialityIndex != specIndex || s.SkillIndex != skillIndex)
                        continue;

                    if (s.Level != skill.level)
                        s.ServerSet(Player);

                    found = true;
                }

                if (found)
                    continue;

                byte defaultLvl = GetDefaultSkillLevel((EPlayerSpeciality)specIndex, (byte)skillIndex);

                if (skill.level != defaultLvl)
                    Player.UnturnedPlayer.skills.ServerSetSkillLevel(specIndex, skillIndex, defaultLvl);
            }
        }
    }

    public byte GetDefaultSkillLevel(EPlayerSpeciality speciality, byte skill)
    {
        Skill[][] skills = Player.UnturnedPlayer.skills.skills;
        if ((int)speciality >= skills.Length)
            throw new ArgumentOutOfRangeException(nameof(speciality), "Speciality index is out of range.");

        if (skill >= skills[(int)speciality].Length)
            throw new ArgumentOutOfRangeException(nameof(skill), "Skill index is out of range.");

        int specIndex = (int)speciality;
        if (Provider.modeConfigData.Players.Spawn_With_Max_Skills ||
            specIndex == (int)EPlayerSpeciality.OFFENSE &&
            (EPlayerOffense)skill is
            EPlayerOffense.CARDIO or EPlayerOffense.EXERCISE or
            EPlayerOffense.DIVING or EPlayerOffense.PARKOUR &&
            Provider.modeConfigData.Players.Spawn_With_Stamina_Skills)
        {
            return skills[(int)speciality][skill].max;
        }

        if (Level.getAsset() is not { skillRules: not null } asset || asset.skillRules.Length <= specIndex || asset.skillRules[specIndex].Length <= skill)
            return 0;

        LevelAsset.SkillRule rule = asset.skillRules[specIndex][skill];
        if (rule != null)
            return (byte)rule.defaultLevel;

        return 0;
    }
}
