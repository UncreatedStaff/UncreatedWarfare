namespace Uncreated.Warfare.Translations.Collections;

/// <summary>
/// All translations meant for signs must go in here and be of type <see cref="SignTranslation"/>.
/// </summary>
public class SignTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Signs";

    // Kit Classes

    public readonly SignTranslation SignClassDescriptionSquadleader = new SignTranslation("class_desc_squadleader", "\n\n<#cecece>Help your squad by supplying them with <#f0a31c>rally points</color> and placing <#f0a31c>FOB radios</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionRifleman = new SignTranslation("class_desc_rifleman", "\n\n<#cecece>Resupply your teammates in the field with an <#f0a31c>Ammo Bag</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionMedic = new SignTranslation("class_desc_medic", "\n\n<#cecece><#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionBreacher = new SignTranslation("class_desc_breacher", "\n\n<#cecece>Use <#f0a31c>high-powered explosives</color> to take out <#f01f1c>enemy FOBs</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionAutoRifleman = new SignTranslation("class_desc_autorifleman", "\n\n<#cecece>Equipped with a high-capacity and powerful <#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionMachineGunner = new SignTranslation("class_desc_machinegunner", "\n\n<#cecece>Equipped with a powerful <#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionLAT = new SignTranslation("class_desc_lat", "\n\n<#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy <#f01f1c>armored enemy vehicles</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionHAT = new SignTranslation("class_desc_hat", "\n\n<#cecece>Equipped with multiple powerful <#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionGrenadier = new SignTranslation("class_desc_grenadier", "\n\n<#cecece>Equipped with a <#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionMarksman = new SignTranslation("class_desc_marksman", "\n\n<#cecece>Equipped with a <#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionSniper = new SignTranslation("class_desc_sniper", "\n\n<#cecece>Equipped with a high-powered <#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionAPRifleman = new SignTranslation("class_desc_aprifleman", "\n\n<#cecece>Equipped with <#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionEngineer = new SignTranslation("class_desc_engineer", "\n\n<#cecece>Features 200% <#f0a31c>build speed</color> and are equipped with <#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionCrewman = new SignTranslation("class_desc_crewman", "\n\n<#cecece>Gives users the ability to operate <#f0a31c>armored vehicles</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionPilot = new SignTranslation("class_desc_pilot", "\n\n<#cecece>Gives users the ability to fly <#f0a31c>aircraft</color>.</color>\n<#f01f1c>\\/");

    public readonly SignTranslation SignClassDescriptionSpecOps = new SignTranslation("class_desc_specops", "\n\n<#cecece>Equipped with <#f0a31c>night-vision</color> to help see at night.</color>\n<#f01f1c>\\/");

    // Loadout & Elite Kits

    [TranslationData("Shown on new seasons when elite kits and loadouts are locked.")]
    public readonly SignTranslation SignKitDelay = new SignTranslation("kitdelay", "<#e6e6e6>All <#3bede1>Elite Kits</color> and <#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <#d8addb>April 2nd, 2023.");

    [TranslationData("Information on how to obtain a loadout.")]
    public readonly SignTranslation SignLoadoutInfo = new SignTranslation("loadout_info", "<#cecece>Loadouts and elite kits can be purchased\nin our <#7483c4>Discord</color> server.</color>\n\n<#7483c4>/discord");

    public readonly SignTranslation SignBundleUSA = new SignTranslation("bundle_usa", "<#fff>USA Bundle");

    public readonly SignTranslation SignBundleUSMC = new SignTranslation("bundle_usmc", "<#fff>USMC Bundle");

    public readonly SignTranslation SignBundleRussia = new SignTranslation("bundle_ru", "<#fff>Russia Bundle");

    public readonly SignTranslation SignBundleSoviet = new SignTranslation("bundle_sov", "<#fff>Soviet Bundle");

    public readonly SignTranslation SignBundlePoland = new SignTranslation("bundle_pl", "<#fff>Polish Bundle");

    public readonly SignTranslation SignBundleMilitia = new SignTranslation("bundle_mi", "<#fff>Militia Bundle");

    public readonly SignTranslation SignBundleIsrael = new SignTranslation("bundle_idf", "<#fff>IDF Bundle");

    public readonly SignTranslation SignBundleGermany = new SignTranslation("bundle_ger", "<#fff>German Bundle");

    public readonly SignTranslation SignBundleFrance = new SignTranslation("bundle_fr", "<#fff>French Bundle");

    public readonly SignTranslation SignBundleCanada = new SignTranslation("bundle_caf", "<#fff>Canadian Bundle");

    public readonly SignTranslation SignBundleAfrica = new SignTranslation("bundle_afr", "<#fff>Africa Bundle");

    public readonly SignTranslation SignBundleSpecial = new SignTranslation("bundle_special", "<#fff>Special Kits");

    // Vehicle Warnings

    [TranslationData("Soloing warning positioned near attack heli and jet.")]
    public readonly SignTranslation SignAirSoloingWarning = new SignTranslation("air_solo_warning", "<color=#f01f1c><b>Do not exit main without another <#cedcde>PILOT</color> while flying any vehicles that require a <#cedcde>PILOT</color> kit!");

    [TranslationData("Soloing warning positioned near armor requests.")]
    public readonly SignTranslation SignArmorSoloingWarning = new SignTranslation("armor_solo_warning", "<color=#f01f1c><b>Do not exit main without another <#cedcde>CREWMAN</color> while driving any vehicles that require a <#cedcde>CREWMAN</color> kit!");

    [TranslationData("Warning about waiting for vehicles while the server is full.")]
    public readonly SignTranslation SignWaitingWarning = new SignTranslation("waiting_warning", "<color=#f01f1c>Waiting for vehicles to spawn when the server is full for more than 2 minutes will result in a KICK or BAN");

    [TranslationData("Notice about soloing (part 1).")]
    public readonly SignTranslation SignSoloNoticePart1 = new SignTranslation("solo_notice_1", "<color=#f01f1c>You are not allowed to take out the following vehicles without another crewman:");

    [TranslationData("Notice about soloing (part 2,USA).")]
    public readonly SignTranslation SignSoloNoticeUSA = new SignTranslation("solo_notice_armor_usa", "<#f01f1c>- M1A2 Abrams\n- M2A4 Bradley\n- LAV-25\n- M1126 Stryker\n</color>\n<#efa311>You can get banned if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).");

    [TranslationData("Notice about soloing (part 2,GER).")]
    public readonly SignTranslation SignSoloNoticeGermany = new SignTranslation("solo_notice_armor_ger", "<#f01f1c>- Leopard 2A6\n- Puma\n- Boxer\n</color>\n<#efa311>You can get banned if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).");

    [TranslationData("Notice about soloing (part 2,CAF).")]
    public readonly SignTranslation SignSoloNoticeCanada = new SignTranslation("solo_notice_armor_caf", "<#f01f1c>- Leopard 2A6M\n- LAV 6\n- Coyote\n</color>\n<#efa311>You can get banned if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).");

    [TranslationData("Notice about soloing (part 2, RU).")]
    public readonly SignTranslation SignSoloNoticeRussia = new SignTranslation("solo_notice_armor_ru", "<#f01f1c>- T-90A\n- BMP-2\n- BTR-82a\n- BRDM-2\n</color>\n<#efa311>You can get banned if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).");

    [TranslationData("Notice about soloing (part 2, CH).")]
    public readonly SignTranslation SignSoloNoticeChina = new SignTranslation("solo_notice_armor_ch", "<#f01f1c>- ZTZ-98\n- Type 86G\n- WZ-551\n</color>\n<#efa311>You can get banned if you do!</color>\n<#f01f1c>If your passenger leaves, return to base (RTB).");

    // FAQs

    [TranslationData("Points to the tutorial with a caption.")]
    public readonly SignTranslation SignTutorialArrow = new SignTranslation("tutorial_arrow", "<#2df332>Small tutorial this way!\n<#ff6600><b><---</b>");


    // FAQ Kit Selection

    public readonly SignTranslation SignFAQ1Title = new SignTranslation("faq1_T", """
                                                                                  <#fa6122>Kits
                                                                                  """);

    public readonly SignTranslation SignFAQ1Question = new SignTranslation("faq1_Q", """
                                                                                   <#2df332>Q: Help! How do I Equip a Kit?
                                                                                   """);

    public readonly SignTranslation SignFAQ1Answer = new SignTranslation("faq1_A", """
                                                                                  <#fa6122>A: Look at a kit sign and <#2df332>punch</color> it to equip it. You may need to buy the kit first, so make sure you have enough credits!
                                                                                  """);

    // FAQ Squads

    public readonly SignTranslation SignFAQ2Title = new SignTranslation("faq2_T", """
                                                                                  <#fa6122>Squads
                                                                                  """);

    public readonly SignTranslation SignFAQ2Question = new SignTranslation("faq2_Q", """
                                                                                  <#2df332>Q: Help! How do I join a Squad?
                                                                                  """);

    public readonly SignTranslation SignFAQ2Answer = new SignTranslation("faq2_A", """
                                                                                  <#fa6122>A: To join a <#2df332>Squad</color> you can talk to the Squads NPC in HQ and Main Base or do /squads. 
                                                                                  """);

    // FAQ Deployment

    public readonly SignTranslation SignFAQ3Title = new SignTranslation("faq3_T", """
                                                                                  <#fa6122>Deployment
                                                                                  """);

    public readonly SignTranslation SignFAQ3Question = new SignTranslation("faq3_Q", """
                                                                                     <#2df332>Q: Help! How do I deploy to the battlefield?
                                                                                     """);

    public readonly SignTranslation SignFAQ3Answer = new SignTranslation("faq3_A", """
                                                                                  <#fa6122>A: You can deploy to a friendly <#2df332>FOB</color> by pressing <#2df332>F</color> on the <#2df332>Map Tacks</color> that show up on the strategy map In HQ.
                                                                                  """);

    // FAQ Vehicles

    public readonly SignTranslation SignFAQ4Title = new SignTranslation("faq4_T", """
                                                                                  <#fa6122>Vehicles
                                                                                  """);

    public readonly SignTranslation SignFAQ4Question = new SignTranslation("faq4_Q", """
                                                                                  <#2df332>Q: Help! How do I Resupply Vehicle Ammo and Flares?
                                                                                  """);

    public readonly SignTranslation SignFAQ4Answer = new SignTranslation("faq4_A", """
                                                                                  <#fa6122>A: Grab a <#2df332>Heavy Ordinance Crate</color> from a <#2df332>Repair Station</color> and throw it at a vehicle.
                                                                                  """);

    // FAQ Giving Up or Flaring

    public readonly SignTranslation SignFAQ5Title = new SignTranslation("faq5_T", """
                                                                                  <#fa6122>Giving up or Flaring
                                                                                  """);

    public readonly SignTranslation SignFAQ5Question = new SignTranslation("faq5_Q", """
                                                                                  <#2df332>Q: Help! I can't give up when I'm downed or flare in an air vehicle.
                                                                                  """);

    public readonly SignTranslation SignFAQ5Answer = new SignTranslation("faq5_A", """
                                                                                  <#fa6122>A: Press <#2df332>/</color> on your keyboard to give up when downed or flare. If this doesn't work head to</color>
                                                                                  <#2df332>Controls -> Mods/Plugins -> Code Hotkey#3
                                                                                  """);

    // Misc

    [TranslationData("Has the discord link.")]
    public readonly SignTranslation SignDiscordLink = new SignTranslation("discord_link", "<color=#cecece>Need help? Join our <color=#7483c4>Discord</color> server!\n<#6796ce>discord.gg/ucn");

    [TranslationData("Server rules")]
    public readonly SignTranslation SignRules = new SignTranslation("rules", "<#cecece>Server Rules can be found in our <color=#7483c4>Discord</color> server!\n<#6796ce>discord.gg/ucn.");

    [TranslationData("Saddam Hussein.", IsPriorityTranslation = false)]
    public readonly SignTranslation SignSaddamHussein = new SignTranslation("saddam_hussein", "<#f01f1c>Saddam Hussein\n ▇▅▆▇▆▅▅█");
}
