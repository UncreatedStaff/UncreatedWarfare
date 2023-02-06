using System.Collections;
using SDG.Unturned;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;
internal static class Deployment
{
    private const float DeployTickSpeed = 0.25f;
    public static void CancelDeploymentsTo(IDeployable location)
    {
        ThreadUtil.assertIsGameThread();
        if (location == null)
            return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.Player.TryGetPlayerData(out UCPlayerData data) && data.PendingDeploy != null && data.PendingDeploy.Equals(location))
            {
                data.CancelDeployment();
            }
        }
    }
    public static void CancelDeployment(UCPlayer player)
    {
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
            data.CancelDeployment();
    }
    public static bool DeployTo(UCPlayer player, IDeployable location, CommandInteraction? ctx, bool cancelOnMove = true, bool cancelOnDamage = false, bool startCooldown = true)
    {
        if (player is null || !player.IsOnline)
        {
            if (ctx is not null)
                throw ctx.SendPlayerNotFound();
            return false;
        }
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
        {
            float delay = location.GetDelay();
            if (data.CurrentTeleportRequest != null && delay > DeployTickSpeed)
            {
                if (ctx is not null)
                    throw ctx.Reply(T.DeployAlreadyActive);
                return false;
            }
            if (!location.CheckDeployable(player, ctx))
                return false;
            if (delay > DeployTickSpeed)
            {
                ctx?.Reply(T.DeployStandby, location, Mathf.CeilToInt(delay));
                data.PendingDeploy = location;
                data.CurrentTeleportRequest = player.Player.StartCoroutine(DeployToCoroutine(player, location, delay, ctx is not null, cancelOnMove, cancelOnDamage, startCooldown));
            }
            else
            {
                ForceDeploy(player, location, ctx is not null, startCooldown);
            }
            return true;
        }
        return false;
    }
    private static IEnumerator DeployToCoroutine(UCPlayer player, IDeployable location, float delay, bool chat, bool cancelOnMove = true, bool cancelOnDamage = false, bool startCooldown = true)
    {
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
        {
            int ticks = Mathf.CeilToInt(delay / DeployTickSpeed);
            Vector3 position = cancelOnMove ? player.Position : default;
            float health = player.Player.life.health;
            for (; ticks >= 0; --ticks)
            {
                yield return new WaitForSeconds(DeployTickSpeed);
                if (cancelOnMove)
                {
                    if (!position.AlmostEquals(player.Position, 0.2f))
                    {
                        if (chat)
                            player.SendChat(T.DeployMoved);
                        data.CurrentTeleportRequest = null;
                        data.PendingDeploy = null;
                        yield break;
                    }
                }
                if (cancelOnDamage)
                {
                    if (player.Player.life.health < health) // healing shouldn't cancel deployment
                    {
                        if (chat)
                            player.SendChat(T.DeployDamaged);
                        data.CurrentTeleportRequest = null;
                        data.PendingDeploy = null;
                        yield break;
                    }
                }
                if (!location.CheckDeployableTick(player, chat))
                {
                    data.CurrentTeleportRequest = null;
                    data.PendingDeploy = null;
                    yield break;
                }
            }
            ForceDeploy(player, location, chat, startCooldown);
            data.CurrentTeleportRequest = null;
            data.PendingDeploy = null;
        }
    }
    public static void ForceDeploy(UCPlayer player, IDeployable location, bool chat, bool startCooldown = true)
    {
        player.Player.teleportToLocationUnsafe(location.Position, location.Yaw);
        location.OnDeploy(player, chat);
        if (startCooldown)
            CooldownManager.StartCooldown(player, CooldownType.Deploy, RapidDeployment.GetDeployTime(player));
    }
}

public interface IDeployable : ITranslationArgument
{
    Vector3 Position { get; }
    float Yaw { get; }
    float GetDelay();
    bool CheckDeployable(UCPlayer player, CommandInteraction? ctx);
    bool CheckDeployableTick(UCPlayer player, bool chat);
    void OnDeploy(UCPlayer player, bool chat);
}
