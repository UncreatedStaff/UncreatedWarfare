using Uncreated.Framework;

namespace Uncreated.Warfare.Moderation.Reports;

[ModerationEntry(ModerationEntryType.Report)]
public class Report : ModerationEntry
{
    public EReportType Type { get; set; }
    public byte[]? ScreenshotJpgData { get; set; }
    public override string GetDisplayName() => "Report";
}