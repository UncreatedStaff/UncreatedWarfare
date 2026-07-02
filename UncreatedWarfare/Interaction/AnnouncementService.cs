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

    private void HandleAnnouncementTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
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

public class AnnouncementTranslations : TranslationCollection
{
    public override string Name => "Announcements";

    [TranslationData("Announcement telling people to join the discord by typing /discord.")]
    public readonly Translation AnnouncementDiscord = new Translation("<#9da6a6>Have you joined our <#7483c4>Discord</color> server yet? Type <#7483c4>/discord</color> to join.");

    [TranslationData("Announcement telling people the best ways to earn XP.")]
    public readonly Translation AnnouncementRankUp = new Translation("<#92a692>Capture <#fff>flags</color> and build <#fff>FOBs</color> to rank up and earn respect amongst your team.");

    [TranslationData("Announcement telling people not to waste assets.")]
    public readonly Translation AnnouncementDontWasteAssets = new Translation("<#a2a7ba>Do not waste <#fff>vehicles, ammo, build, or other assets!</color> You may risk punishment if you're reported or caught.");

    [TranslationData("Announcement telling people to communicate.")]
    public readonly Translation AnnouncementListenToSuperiors = new Translation("<#a2a7ba>Winning requires <#fff>coordination and teamwork</color>. Listen to each other and <#fff>communicate</color>!");

    [TranslationData("Announcement telling people to build FOBs to help their team.")]
    public readonly Translation AnnouncementBuildFOBs = new Translation("<#9da6a6>Building <#54e3ff>FOBs</color> is vital for advancing operations. Grab a logistics truck and go build one!");
    
    [TranslationData("Announcement telling people how to request a kit.")]
    public readonly Translation AnnouncementKits = new Translation("<#9da6a6>If you are having trouble trying to request a kit, try <#fff>punching the sign</color> of the kit you wish to request.");
    
    [TranslationData("Announcement telling people to grab ammo.")]
    public readonly Translation AnnouncementAmmo = new Translation("<#9da6a6>Out of ammo? You can refill any empty magazines, medical supplies, and extra equipment by grabing ammo from <#54e3ff>FOBs</color> or deploying to main base.");

    [TranslationData("Announcement telling people to join or create a squad.")]
    public readonly Translation AnnouncementSquads = new Translation("<#9da6a6>Join or create a squad by talking to the <#fff>Squads NPC</color> or use <#fff>/squads</color> to team up and earn extra XP.");

    [TranslationData("Announcement telling people to place rally points to help their team.")]
    public readonly Translation AnnouncementPlaceRallys = new Translation("<#9da6a6><#54e3ff>Rally Points</color> can be used as a strong tool for advancing operations. Ask your <#fff>Squad Leader</color> to place one!");

    [TranslationData("Announcement telling people about the different way our chat works.")]
    public readonly Translation AnnouncementChatChanges = new Translation("<#a2a7ba>When in a squad, <#54e3ff>area chat</color> also includes your squad, no matter where they are.");

    [TranslationData("Announcement telling people about the abandon command.")]
    public readonly Translation AnnouncementAbandon = new Translation("<#9da6a6>Done with your vehicle? Use <#fff>/abandon</color> while in main base to get some credits back and free up the vehicle for your team.");

    [TranslationData("Announcement telling people about soloing.")]
    public readonly Translation AnnouncementSoloing = new Translation("<#9da6a6>Any vehicle requiring a <#fff>Crewman or Pilot kit</color> must have a crew of at least 2 people.");

    [TranslationData("Announcement telling people about the armor zones.")]
    public readonly Translation AnnouncementArmorZones = new Translation("<#9da6a6>Armor vehicles use <#54e3ff>armor zones</color>, so make your hits count. Aim for the rear to do the most damage. <#fff>Crewman</color>, don't get flanked.");

    [TranslationData("Announcement telling people about reporting with /report.")]
    public readonly Translation AnnouncementReport = new Translation("<#9da6a6>See someone breaking rules? Use the <#fff>/report</color> command to help admins see context about the report.</color>");
}