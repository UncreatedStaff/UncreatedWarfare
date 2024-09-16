using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Traits.Buffs;
/// <summary>
/// Incoming enemy mortars near a player under this effect will trigger a notification.
/// </summary>
public class BadOmen : Buff
{
    private static TraitData? DATA;
    public static TraitData DefaultData = new TraitData
    {
        TypeName = nameof(BadOmen),
        NameTranslations = new TranslationList("Bad Omen"),
        DescriptionTranslations = new TranslationList("6th sense for incoming shells."),
        CreditCost = 350,
        Icon = "¯",
        EffectDuration = 600,
        Cooldown = 1200,
        UnlockRequirements = new UnlockRequirement[] { new LevelUnlockRequirement() { UnlockLevel = 6 } },
        EffectDistributedToSquad = false,
        Data = "15" // max notice given in seconds
    };
    private float _maxNotice;
    private float _squadMaxNotice;
    protected override void StartEffect(bool onStart)
    {
        if (onStart)
        {
            if (Data.Data is null || !float.TryParse(Data.Data, NumberStyles.Number, Warfare.CultureInfo.InvariantCulture, out _maxNotice))
                _maxNotice = 15f;

            _squadMaxNotice = Data.EffectDistributedToSquad
                ? _maxNotice *
                  (TargetPlayer.IsSquadLeader()
                      ? Data.SquadLeaderDistributedMultiplier
                      : Data.SquadDistributedMultiplier)
                : 0f;
        }
        base.StartEffect(onStart);
    }
    internal override void SquadLeaderDemoted()
    {
        if (!Data.EffectDistributedToSquad)
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
        TraitData? d = DATA ??= TraitManager.GetData(typeof(BadOmen));
        return d != null && TraitManager.IsAffected(d, player, out _);
    }
    public static void TryWarn(UCPlayer owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType, bool warnFriendlies, bool warnEnemies)
    {
        if (!warnEnemies && !warnFriendlies)
            return;
        UCWarfare.I.StartCoroutine(WarnCoroutine(owner, position, impactTime, gun, ammoType, warnFriendlies, warnEnemies));
    }

    private static IEnumerator WarnCoroutine(UCPlayer owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType, bool warnFriendlies, bool warnEnemies)
    {
        float blastRadius = gun.range;
        if (ammoType != null)
            blastRadius *= ammoType.projectileBlastRadiusMultiplier;
        blastRadius += 5;
        blastRadius *= blastRadius;
        List<UCPlayer> warned = new List<UCPlayer>(8);
        ulong team = owner.GetTeam();
        float secsLeft = impactTime - Time.realtimeSinceStartup;
        while (secsLeft >= 0f)
        {
            for (int j = 0; j < PlayerManager.OnlinePlayers.Count; ++j)
            {
                UCPlayer target = PlayerManager.OnlinePlayers[j];
                bool friendly = team == target.GetTeam();
                if (friendly)
                {
                    if (!warnFriendlies)
                        continue;
                }
                else if (!warnEnemies)
                    continue;

                if ((target.Position - position).sqrMagnitude < blastRadius)
                {
                    L.LogDebug("[BAD OMEN] Checking " + target.Name.PlayerName);
                    if (!warned.Contains(target))
                    {
                        float warnTime = !friendly ? GetBadOmenWarn(target) : float.PositiveInfinity;
                        L.LogDebug("[BAD OMEN] warning " + target.Name.PlayerName + ", warn time: " + warnTime);
                        if (friendly || warnTime > 0f && secsLeft <= warnTime)
                        {
                            WarnPlayer(target);
                            warned.Add(target);
                        }
                    }
                }
                else if (warned.RemoveFast(target))
                {
                    Clear(target);
                }
            }
            yield return new WaitForSecondsRealtime(1f);
            secsLeft = impactTime - Time.realtimeSinceStartup;
        }
        for (int i = 0; i < warned.Count; ++i)
        {
            Clear(warned[i]);
        }

        void Clear(UCPlayer player)
        {
            if (!player.IsOnline) return;
            L.LogDebug("[BAD OMEN] Clearing " + player.Name.PlayerName);
            ++player.MortarWarningCount;
            if (player.MortarWarningCount >= 0)
            {
                player.MortarWarningCount = 0;
                player.Toasts.SkipExpiration(ToastMessageStyle.FlashingWarning);
            }
        }
    }
    private static void WarnPlayer(UCPlayer player)
    {
        --player.MortarWarningCount;
        if (player.MortarWarningCount == -1)
            ToastMessage.QueueMessage(player, new ToastMessage(ToastMessageStyle.FlashingWarning, T.MortarWarning.Translate(player)) { OverrideDuration = 25f });
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
}
