using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Kits;

[Translatable("Squad Level", Description = "Rank level associated with a Kit.")]
public enum SquadLevel : byte
{
    [Translatable("Member", Description = "Normal member.")]
    Member = 0,
    [Translatable("Commander", Description = "Can request UAVs.")]
    Commander = 4
}