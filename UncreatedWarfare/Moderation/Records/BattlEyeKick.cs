using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.BattlEyeKick)]
public class BattlEyeKick : ModerationEntry
{
    public override string GetDisplayName() => "BattlEye Kick";
}
