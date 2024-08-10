namespace Uncreated.Warfare.Translations.Collections;

/// <summary>
/// All translations meant for signs must go in here and be of type <see cref="SignTranslation"/>.
/// </summary>
public class SignTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Signs";

    [TranslationData("Server rules")]
    public static readonly SignTranslation SignRules = new SignTranslation("rules", "Rules\nNo suicide vehicles.\netc.");

    [TranslationData("Shown on new seasons when elite kits and loadouts are locked.")]
    public static readonly SignTranslation SignKitDelay = new SignTranslation("kitdelay", "<#e6e6e6>All <#3bede1>Elite Kits</color> and <#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <#d8addb>April 2nd, 2023</color>.");

    public static readonly SignTranslation SignClassDescriptionSquadleader = new SignTranslation("class_desc_squadleader", "\n\n<#cecece>Help your squad by supplying them with <#f0a31c>rally points</color> and placing <#f0a31c>FOB radios</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionRifleman = new SignTranslation("class_desc_rifleman", "\n\n<#cecece>Resupply your teammates in the field with an <#f0a31c>Ammo Bag</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionMedic = new SignTranslation("class_desc_medic", "\n\n<#cecece><#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionBreacher = new SignTranslation("class_desc_breacher", "\n\n<#cecece>Use <#f0a31c>high-powered explosives</color> to take out <#f01f1c>enemy FOBs</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionAutoRifleman = new SignTranslation("class_desc_autorifleman", "\n\n<#cecece>Equipped with a high-capacity and powerful <#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionMachineGunner = new SignTranslation("class_desc_machinegunner", "\n\n<#cecece>Equipped with a powerful <#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionLAT = new SignTranslation("class_desc_lat", "\n\n<#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy <#f01f1c>armored enemy vehicles</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionHAT = new SignTranslation("class_desc_hat", "\n\n<#cecece>Equipped with multiple powerful <#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionGrenadier = new SignTranslation("class_desc_grenadier", "\n\n<#cecece>Equipped with a <#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionMarksman = new SignTranslation("class_desc_marksman", "\n\n<#cecece>Equipped with a <#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionSniper = new SignTranslation("class_desc_sniper", "\n\n<#cecece>Equipped with a high-powered <#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionAPRifleman = new SignTranslation("class_desc_aprifleman", "\n\n<#cecece>Equipped with <#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionEngineer = new SignTranslation("class_desc_engineer", "\n\n<#cecece>Features 200% <#f0a31c>build speed</color> and are equipped with <#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionCrewman = new SignTranslation("class_desc_crewman", "\n\n<#cecece>The only kits than can man <#f0a31c>armored vehicles</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionPilot = new SignTranslation("class_desc_pilot", "\n\n<#cecece>The only kits that can fly <#f0a31c>aircraft</color>.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignClassDescriptionSpecOps = new SignTranslation("class_desc_specops", "\n\n<#cecece>Equipped with <#f0a31c>night-vision</color> to help see at night.</color>\n<#f01f1c>\\/</color>");

    public static readonly SignTranslation SignBundleMisc = new SignTranslation("bundle_misc", "<#fff>Misc.");

    public static readonly SignTranslation SignBundleCanada = new SignTranslation("bundle_caf", "<#fff>Canadian Bundle");

    public static readonly SignTranslation SignBundleFrance = new SignTranslation("bundle_fr", "<#fff>French Bundle");

    public static readonly SignTranslation SignBundleGermany = new SignTranslation("bundle_ger", "<#fff>German Bundle");

    public static readonly SignTranslation SignBundleUSMC = new SignTranslation("bundle_usmc", "<#fff>USMC Bundle");

    public static readonly SignTranslation SignBundleUSA = new SignTranslation("bundle_usa", "<#fff>USA Bundle");

    public static readonly SignTranslation SignBundlePoland = new SignTranslation("bundle_pl", "<#fff>Polish Bundle");

    public static readonly SignTranslation SignBundleIsrael = new SignTranslation("bundle_idf", "<#fff>IDF Bundle");

    public static readonly SignTranslation SignBundleMilitia = new SignTranslation("bundle_militia", "<#fff>Militia Bundle");

    public static readonly SignTranslation SignBundleRussia = new SignTranslation("bundle_ru", "<#fff>Russia Bundle");

    public static readonly SignTranslation SignBundleSoviet = new SignTranslation("bundle_soviet", "<#fff>Soviet Bundle");

    public static readonly SignTranslation SignBundleSpecial = new SignTranslation("bundle_special", "<#fff>Special Kits");

    [TranslationData("Information on how to obtain a loadout.")]
    public static readonly SignTranslation SignLoadoutInfo = new SignTranslation("loadout_info", "<#cecece>Loadouts and elite kits can be purchased\nin our <#7483c4>Discord</color> server.</color>\n\n<#7483c4>/discord</color>");

    [TranslationData("Soloing warning positioned near attack heli and jet.")]
    public static readonly SignTranslation SignAirSoloingWarning = new SignTranslation("air_solo_warning", "<color=#f01f1c><b>Do not exit main without another <#cedcde>PILOT</color> for the Jet or Attack Heli\n\n\n<color=#ff6600>YOU WILL BE BANNED FOR 6 DAYS WITHOUT WARNING!<b></color>");

    [TranslationData("Soloing warning positioned near armor requests.")]
    public static readonly SignTranslation SignArmorSoloingWarning = new SignTranslation("armor_solo_warning", "<color=#f01f1c><b>Do not exit main without another <#cedcde>CREWMAN</color> while driving any vehicles that require a <#cedcde>CREWMAN</color> kit!\n\n\n<color=#ff6600>YOU WILL BE BANNED FOR 6 DAYS WITHOUT WARNING!<b></color>");

    [TranslationData("Warning about waiting for vehicles while the server is full.")]
    public static readonly SignTranslation SignWaitingWarning = new SignTranslation("waiting_warning", "<color=#f01f1c>Waiting for vehicles to spawn when the server is full for more than 2 minutes will result in a KICK or BAN</color>");

    [TranslationData("Change notice about waiting for vehicles while the server is full (part 1).")]
    public static readonly SignTranslation SignWaitingNoticePart1 = new SignTranslation("waiting_notice_1", "<color=yellow>Warning:</color>\n<color=white>Due to players sitting at base waiting for air assets, we've decided that if the server capacity is full and air assets aren't available for</color>");

    [TranslationData("Change notice about waiting for vehicles while the server is full (part 2).")]
    public static readonly SignTranslation SignWaitingNoticePart2 = new SignTranslation("waiting_notice_2", "<color=white>a considerable amount of time, we reserve the right to warn you not to. If you continue to sit around we will kick you to allow another player in the queue to play.</color>");

    [TranslationData("Notice about soloing (part 1).")]
    public static readonly SignTranslation SignSoloNoticePart1 = new SignTranslation("solo_notice_1", "<color=#f01f1c>You are not allowed to take out the following vehicles without a passenger:</color>");

    [TranslationData("Notice about soloing (part 2, team 1).")]
    public static readonly SignTranslation SignSoloNoticePart2T1 = new SignTranslation("solo_notice_2_t1", "<#f01f1c>- Abrams\n- LAV\n- Stryker\n- Attack Heli\n- Fighter Jet\n</color>\n<#ec8100>You will get banned for <#ff6600>3 days</color> if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).</color>");

    [TranslationData("Notice about soloing (part 2, team 2).")]
    public static readonly SignTranslation SignSoloNoticePart2T2 = new SignTranslation("solo_notice_2_t2", "<#f01f1c>- T-90\n- BTR-82A\n- BDRM\n- BMP-2\n- Attack Heli\n- Fighter Jet\n</color>\n<#ec8100>You will get banned for <#ff6600>3 days</color> if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).</color>");

    [TranslationData("Points to the tutorial with a caption.")]
    public static readonly SignTranslation SignTutorialArrow = new SignTranslation("tutorial_arrow", "<#2df332>Small tutorial this way!\n<#ff6600><b><---</b>");

    [TranslationData("Tells the player about kits and how to request them (part 1).")]
    public static readonly SignTranslation SignTutorialGetKitPart1 = new SignTranslation("tutorial_get_kit_1", "<#ff6600><b>How do I get a kit?</b>");

    [TranslationData("Tells the player about kits and how to request them (part 2).")]
    public static readonly SignTranslation SignTutorialGetKitPart2 = new SignTranslation("tutorial_get_kit_2", "<#cEcEcE>Look at kit sign and type <#2df332>/req</color> in chat to recieve the kit.");

    [TranslationData("Tells the player about kits and how to request them (part 3).")]
    public static readonly SignTranslation SignTutorialGetKitPart3 = new SignTranslation("tutorial_get_kit_3", "<#cEcEcE>Some kits are unlocked using <#c$credits$>credits</color>. Look at the sign and do <#2df332>/buy</color> to unlock the kit.");

    [TranslationData("Tells the player about vehicles and how to request them (part 1).")]
    public static readonly SignTranslation SignTutorialGetVehiclePart1 = new SignTranslation("tutorial_get_vehicle_1", "<#ff6600><b>How do I get a vehicle?</b>");

    [TranslationData("Tells the player about vehicles and how to request them (part 2).")]
    public static readonly SignTranslation SignTutorialGetVehiclePart2 = new SignTranslation("tutorial_get_vehicle_2", "<#cEcEcE>Look at the vehicle you'd like to request and type in chat <#2df332>/req</color> to unlock the vehicle.");

    [TranslationData("Tells the player about vehicles and how to request them (part 3).")]
    public static readonly SignTranslation SignTutorialGetVehiclePart3 = new SignTranslation("tutorial_get_vehicle_3", "<#cEcEcE>Some vehicles require a special kit. Request a <#cedcde>CREWMAN</color> or <#cedcde>PILOT</color> kit to gain acces to them!");

    [TranslationData("Header for the FAQ (frequently asked questions) section of the tutorial.")]
    public static readonly SignTranslation SignTutorialFAQHeader = new SignTranslation("tutorial_faq_header", "<#ff6600><b>FAQ</b>");

    [TranslationData("(question) This FAQ explains how to Give Up after being injured.")]
    public static readonly SignTranslation SignTutorialFAQGiveUpQ = new SignTranslation("tutorial_faq_give_up_q", "<#2df332>Q: Help! I can't reset when downed!");

    [TranslationData("(answer) This FAQ explains how to Give Up after being injured.")]
    public static readonly SignTranslation SignTutorialFAQGiveUpA = new SignTranslation("tutorial_faq_give_up_a", "<#cEcEcE>A: Press the '/' button on your keyboard to give up when injured. If this doesn't work, Head to your <#2df332>keybind settings</color> and set <#f32d2d>Code Hotkey #3</color> to your preference!");

    [TranslationData("Has the discord link.")]
    public static readonly SignTranslation SignDiscordLink = new SignTranslation("discord_link", "<color=#CECECE>Need help? Join our <color=#7483c4>Discord</color> server!\n<#6796ce>discord.gg/ucn</color>");

    [TranslationData("Saddam Hussein.", IsPriorityTranslation = false)]
    public static readonly SignTranslation SignSaddamHussein = new SignTranslation("saddam_hussein", "<color=red>Saddam Hussein\n ▇▅▆▇▆▅▅█</color>");

    [TranslationData("Points to the building with elite kits.")]
    public static readonly SignTranslation SignEliteKitPointer = new SignTranslation("elite_kit_pointer", "<color=#f0a31c>Elite kits found in this building     --></color>");


    public static readonly SignTranslation SignFAQ1Part1 = new SignTranslation("faq1_1", """
                                                                                         <#fa6122>Giving up or Flaring
                                                                                         """);

    public static readonly SignTranslation SignFAQ1Part2 = new SignTranslation("faq1_2", """
                                                                                         <#2df332>Q: Help! I can't give up when I'm downed or flare in a air vehicle </color>
                                                                                         """);

    public static readonly SignTranslation SignFAQ1Part3 = new SignTranslation("faq1_3", """
                                                                                         <#FFFFFF>A: Press <#2df332>/</color> on your keyboard to give up when downed or flare. If this doesn't work head to</color>
                                                                                         <#f01f1c>Controls -> Mods/Plugins -> Code Hotkey#3
                                                                                         """);

    public static readonly SignTranslation SignFAQ2Part1 = new SignTranslation("faq2_1", """
                                                                                         <#fa6122>Squad Commands
                                                                                         """);

    public static readonly SignTranslation SignFAQ2Part2 = new SignTranslation("faq2_2", """
                                                                                         <#fa6122>To Join a Squad
                                                                                         <#FFFFFF>/Squad join <#2df332>(Name)</color>
                                                                                         <#fa6122>To Create a Squad
                                                                                         <#FFFFFF>/Squad Create
                                                                                         """);

    public static readonly SignTranslation SignFAQ2Part3 = new SignTranslation("faq2_3", """
                                                                                         <#fa6122>Other Squad Commands
                                                                                         <#FFFFFF>/squad leave
                                                                                         /squad disband
                                                                                         /squad promote <#2df332>(player)</color>
                                                                                         /squad kick <#2df332>(player)
                                                                                         </color>/squad lock / unlock
                                                                                         """);

    public static readonly SignTranslation SignFAQ3Part1 = new SignTranslation("faq3_1", """
                                                                                         <#fa6122>How do I get a kit?
                                                                                         """);

    public static readonly SignTranslation SignFAQ3Part2 = new SignTranslation("faq3_2", """
                                                                                         <#FFFFFF>If you are new to the server look at a kit sign that says free, and type <#2df332>/request</color> to receive it
                                                                                         """);

    public static readonly SignTranslation SignFAQ3Part3 = new SignTranslation("faq3_3", """
                                                                                         <#FFFFFF>Some kits are not free and will need to be purchased with in-game credits, do <#2df332>/buy</color> on the kit sign to purchase it
                                                                                         """);

    public static readonly SignTranslation SignFAQ4Part1 = new SignTranslation("faq4_1", """
                                                                                         <#fa6122>How do I deploy to the battlefield?
                                                                                         """);

    public static readonly SignTranslation SignFAQ4Part2 = new SignTranslation("faq4_2", """
                                                                                         <#FFFFFF>On the left side of your screen, you can see the FOBs that belong to your team. Each fob displays a different number <#6698FF>(FOB#)</color>, and location.
                                                                                         """);

    public static readonly SignTranslation SignFAQ4Part3 = new SignTranslation("faq4_3", """
                                                                                         <#FFFFFF>To deploy you have to type: /dep fob(number) <#2df332>e.g. /deploy fob1</color> Check its location before deploying.
                                                                                         """);

    public static readonly SignTranslation SignFAQ5Part1 = new SignTranslation("faq5_1", """
                                                                                         <#fa6122>How do I get a vehicle?
                                                                                         """);

    public static readonly SignTranslation SignFAQ5Part2 = new SignTranslation("faq5_2", """
                                                                                         <#FFFFFF>Look at a vehicle you would like to request, and type <#2df332>/request</color> to unlock the vehicle
                                                                                         """);

    public static readonly SignTranslation SignFAQ5Part3 = new SignTranslation("faq5_3", """
                                                                                         <#FFFFFF>Some Vehicles require a special kit: Like Helicopters APCs, IFVs, and MBTs. You'll need to request and/or buy, <#2df332>the crewman or pilot kit to gain access to them
                                                                                         """);
}
