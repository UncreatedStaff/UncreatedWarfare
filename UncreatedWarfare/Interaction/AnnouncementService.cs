using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Interaction;

/// <summary>
/// Handles broadcasting announcements on a timer.
/// </summary>
public class AnnouncementService : IHostedService, IDisposable
{
    private readonly ILoopTicker _ticker;
    private readonly ChatService _chatService;
    private readonly AnnouncementTranslations _translations;
    private int _index = -1;
    public AnnouncementService(ILoopTickerFactory tickerFactory, IConfiguration systemConfig, ChatService chatService, TranslationInjection<AnnouncementTranslations> translations)
    {
        _chatService = chatService;
        _translations = translations.Value;

        _ticker = tickerFactory.CreateTicker(TimeSpan.FromSeconds(systemConfig.GetValue<float>("interactions:announcement_time_sec")), false, true, HandleAnnouncementTick);
    }

    private void HandleAnnouncementTick(ILoopTicker ticker, TimeSpan timesincestart, TimeSpan deltatime)
    {
        if (_translations.Translations.Count == 0)
            return;

        _index = (_index + 1) % _translations.Translations.Count;

        Translation translation = _translations.Translations.Values.ElementAt(_index);

        _chatService.Broadcast(translation);
    }

    public void Dispose()
    {
        _ticker.Dispose();
    }

    UniTask IHostedService.StartAsync(CancellationToken token) => UniTask.CompletedTask;
    UniTask IHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;
}

public class AnnouncementTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Announcements";

    [TranslationData("Announcement telling people to join the discord by typing /discord.")]
    public readonly Translation AnnouncementDiscord = new Translation("<#b3b3b3>Have you joined our <#7483c4>Discord</color> server yet? Type <#7483c4>/discord</color> to join.");

    [TranslationData("Announcement telling people how to return to base from FOBs.")]
    public readonly Translation AnnouncementDeployMain = new Translation("<#c2b7a5>You can deploy back to main by doing <#ffffff>/deploy main</color> while near a friendly FOB.");

    [TranslationData("Announcement telling people the best ways to earn XP.")]
    public readonly Translation AnnouncementRankUp = new Translation("<#92a692>Capture <#ffffff>flags</color> and build <#ffffff>FOBs</color> to rank up and earn respect amongst your team.");

    [TranslationData("Announcement telling people not to waste assets.")]
    public readonly Translation AnnouncementDontWasteAssets = new Translation("<#c79675>Do not waste vehicles, ammo, build, or other assets! You may risk punishment if you're reported or caught.");

    [TranslationData("Announcement telling people to communicate and listen to higher-ups.")]
    public readonly Translation AnnouncementListenToSuperiors = new Translation("<#a2a7ba>Winning requires coordination and teamwork. Listen to your superior officers, and communicate!");

    [TranslationData("Announcement telling people to build FOBs to help their team.")]
    public readonly Translation AnnouncementBuildFOBs = new Translation("<#9da6a6>Building <color=#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!");

    [TranslationData("Announcement telling people to join or create a squad.")]
    public readonly Translation AnnouncementSquads = new Translation("<#c2b7a5>Join a squad with <#ffffff>/squad join</color> or create one with <#ffffff>/squad create</color> to earn extra XP among other benefits.");

    [TranslationData("Announcement telling people about the different way our chat works.")]
    public readonly Translation AnnouncementChatChanges = new Translation("<#a2a7ba>Use area chat while in a squad to communicate with only them or group chat to communicate with your entire <#54e3ff>team</color>.");

    [TranslationData("Announcement telling people about the abandon command.")]
    public readonly Translation AnnouncementAbandon = new Translation("<#b3b3b3>Done with your vehicle? Type <#ffffff>/abandon</color> while in main base to get some credits back and free up the vehicle for your team.");

    [TranslationData("Announcement telling people about soloing.")]
    public readonly Translation AnnouncementSoloing = new Translation("<#c79675>Soloing armor vehicles, attack helis, and jets is against the rules. Make sure you have a passenger for these vehicles.");

    [TranslationData("Announcement telling people about reporting with /report.")]
    public readonly Translation AnnouncementReport = new Translation("<#c2b7a5>See someone breaking rules? Use the <#ffffff>/report</color> command to help admins see context about the report.</color>");
}