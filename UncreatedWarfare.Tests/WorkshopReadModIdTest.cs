using NUnit.Framework;
using Uncreated.Warfare.Steam;

namespace Uncreated.Warfare.Tests;
public class WorkshopReadModIdTest
{
    [Test]
    public void TestReadModId()
    {
        const ulong modId = 3404855950;

        const string fileContents =
                            """
                            "workshopitem"
                            {
                            	"appid"		"304930"
                            	"publishedfileid"		"3404855950"
                            	"contentfolder"		"C:\\SteamCMD\\steamapps\\common\\U3DS\\Servers\\UncreatedSeason4\\Warfare\\Quests\\UncreatedDailyQuests"
                            	"previewfile"		"C:\\\\SteamCMD\\\\steamapps\\\\common\\\\U3DS\\\\Servers\\\\UncreatedSeason4\\\\Warfare\\\\uncreated_logo.jpg"
                            	"visibility"		"0"
                            	"title"		"Uncreated Daily Quests"
                            	"description"		"Automatically generated workshop item that is filled with automatically generated daily quests for the next week."
                            	"changenote"		"Added this week's quests."
                            }
                            """;

        Assert.That(WorkshopUploader.ReadModId(fileContents), Is.EqualTo(modId));
    }
}
