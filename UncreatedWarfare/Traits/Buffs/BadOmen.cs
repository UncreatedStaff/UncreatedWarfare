using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using UnityEngine;

namespace Uncreated.Warfare.Traits.Buffs;
public class BadOmen : Buff
{
    public static TraitData DEFAULT_DATA = new TraitData()
    {
        TypeName = nameof(BadOmen),
        NameTranslations = new TranslationList("Bad Omen"),
        DescriptionTranslations = new TranslationList("6th sense for incoming shells."),
        CreditCost = 350,
        Icon = "¯",
        EffectDuration = 600,
        UnlockRequirements = new BaseUnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 6 } },
        EffectDistributedToSquad = false,
        Data = "15" // max notice given in seconds
    };
    private float _maxNotice;
    private float _squadMaxNotice;
    protected override void StartEffect()
    {
        if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.Data.Locale, out _maxNotice))
            _maxNotice = 15f;

        _squadMaxNotice = Data.EffectDistributedToSquad
            ? _maxNotice *
              (TargetPlayer.IsSquadLeader()
                ? Data.SquadLeaderDistributedMultiplier
                : Data.SquadDistributedMultiplier)
            : 0f;
        base.StartEffect();
    }
    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadMaxNotice = _maxNotice * Data.SquadDistributedMultiplier;
    }
    internal override void SquadLeaderPromoted()
    {
        if (!Data.EffectDistributedToSquad || TargetPlayer.Squad is null)
            return;
        _squadMaxNotice = _maxNotice * Data.SquadLeaderDistributedMultiplier;
    }
    public static bool CanSeeIncomingMortars(UCPlayer player)
    {
        TraitData? d = TraitManager.GetData(typeof(BadOmen));
        return d != null && TraitManager.IsAffected(d, player, out _);
    }
    public static void WarnEnemies(UCPlayer owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType)
    {
        UCWarfare.I.StartCoroutine(WarnEnemiesCoroutine(owner, position, impactTime, gun, ammoType));
    }

    private static IEnumerator WarnEnemiesCoroutine(UCPlayer owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType)
    {
        float secsLeft = impactTime - Time.realtimeSinceStartup;

        float blastRadius = gun.range;
        if (ammoType != null)
            blastRadius *= ammoType.projectileBlastRadiusMultiplier;
        blastRadius += 5;
        L.Log("blast radius: " + blastRadius);
        blastRadius *= blastRadius;
        List<ulong> warned = new List<ulong>(8);
        ulong team = owner.GetTeam();
        while (impactTime - Time.realtimeSinceStartup >= 0f)
        {
            for (int j = 0; j < PlayerManager.OnlinePlayers.Count; ++j)
            {
                UCPlayer target = PlayerManager.OnlinePlayers[j];
                if ((target.Position - position).sqrMagnitude < blastRadius)
                {
                    L.LogDebug("[BAD OMEN] Checking " + target.Name.PlayerName);
                    if (team != target.GetTeam())
                        continue;
                    for (int k = 0; k < warned.Count; ++k)
                        if (warned[k] == target.Steam64)
                            goto next;

                    float warnTime = GetBadOmenWarn(target);
                    if (warnTime > 0f && secsLeft <= warnTime)
                    {
                        WarnPlayer(target, secsLeft);
                        warned.Add(target.Steam64);
                    }
                }
                next: ;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }
    private static void WarnPlayer(UCPlayer player, float timeLeft)
    {
        ToastMessage msg = new ToastMessage(T.BadOmenMortarWarning.Translate(player, timeLeft), EToastMessageSeverity.SEVERE);
        ToastMessage.QueueMessage(player, msg, true);
        player.Player.StartCoroutine(WarnTimer(player, timeLeft, msg));
    }
    private static IEnumerator WarnTimer(UCPlayer player, float timeLeft, ToastMessage msg)
    {
        float endTime = Time.realtimeSinceStartup + timeLeft;
        uint id = msg.InstanceID;
        UCPlayerData.ToastMessageInfo info = UCPlayerData.ToastMessageInfo.Nil;
        for (int i = 0; i < UCPlayerData.TOASTS.Length; i++)
        {
            if (UCPlayerData.TOASTS[i].type == msg.Severity)
            {
                info = UCPlayerData.TOASTS[i];
                break;
            }
        }
        if (info.guid == Guid.Empty)
            yield break;
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
        {
            float t;
            while ((t = endTime - Time.realtimeSinceStartup) >= 0f)
            {
                if (data.channels[info.channel].message.InstanceID != id)
                    yield break;
                EffectManager.sendUIEffectText(unchecked((short)info.id), player.Connection, true, "Text", T.BadOmenMortarWarning.Translate(player, t));
                yield return new WaitForSecondsRealtime(1f);
            }
            EffectManager.askEffectClearByID(info.id, player.Connection);
        }
    }
    private static float GetBadOmenWarn(UCPlayer player)
    {
        float highest = 0f;
        for (int i = 0; i < player.ActiveBuffs.Length; ++i)
        {
            if (player.ActiveBuffs[i] is BadOmen b && b.IsActivated)
            {
                if (b.TargetPlayer.Steam64 == player.Steam64)
                    return b._maxNotice;
                if (highest < b._squadMaxNotice)
                    highest = b._squadMaxNotice;
            }
        }

        return highest;
    }

    internal static void WarnFreindlies(UCPlayer player, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset ammoType)
    {
        return;
    }
}
