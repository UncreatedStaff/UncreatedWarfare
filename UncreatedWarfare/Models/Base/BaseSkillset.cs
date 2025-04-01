using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Players.Skillsets;

namespace Uncreated.Warfare.Models.Base;
public abstract class BaseSkillset : ICloneable
{
    [NotMapped]
    public Skillset Skillset { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Column("Skill")]
    [StringLength(20)]
    public string Skill
    {
        get => Skillset.Speciality switch
        {
            EPlayerSpeciality.OFFENSE => Skillset.Offense.ToString(),
            EPlayerSpeciality.DEFENSE => Skillset.Defense.ToString(),
            EPlayerSpeciality.SUPPORT => Skillset.Support.ToString(),
            _ => throw new InvalidOperationException($"Invalid speciality: {Skillset.Speciality}.")
        };
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            int skillIndex = Skillset.GetSkillsetFromEnglishName(value, out EPlayerSpeciality speciality);
            if (skillIndex == -1)
                throw new ArgumentException("Unable to parse skill as a valid player skill.", nameof(value));
            Skillset = new Skillset(speciality, (byte)skillIndex, Skillset.Level);
        }
    }

    [Column("Level")]
    public byte Level
    {
        get => Skillset.Level;
        set => Skillset = new Skillset(Skillset.Speciality, Skillset.SkillIndex, value);
    }

    protected BaseSkillset() { }
    protected BaseSkillset(BaseSkillset other)
    {
        Skillset = other.Skillset;
    }

    /// <inheritdoc />
    public abstract object Clone();
}
