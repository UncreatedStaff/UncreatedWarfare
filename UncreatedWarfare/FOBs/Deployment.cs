using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;
internal static class Deployment
{
    private const float TICK_SPEED = 0.25f;
    public static bool DeployTo(UCPlayer player, IDeployable location, CommandInteraction? ctx, float delay)
    {
        if (player is null || !player.IsOnline)
        {
            if (ctx is not null)
                throw ctx.SendPlayerNotFound();
            return false;
        }
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
        {
            if (data.CurrentTeleportRequest != null && delay > TICK_SPEED)
            {
                if (ctx is not null)
                    throw ctx.Reply(T.DeployAlreadyActive);
                return false;
            }
            if (!location.CheckDeployable(player, ctx))
                return false;
            if (delay > TICK_SPEED)
            {
                if (ctx is not null)
                {
                    ctx.Reply(T.DeployStandby, location, Mathf.CeilToInt(delay));
                }
                data.CurrentTeleportRequest = player.Player.StartCoroutine(DeployToCoroutine(player, location, delay, ctx is not null));
            }
            else
            {
                Deploy(player, location, ctx is not null);
            }
            return true;
        }
        return false;
    }
    private static IEnumerator DeployToCoroutine(UCPlayer player, IDeployable location, float delay, bool chat)
    {
        if (player.Player.TryGetPlayerData(out UCPlayerData data))
        {
            int ticks = Mathf.CeilToInt(delay / TICK_SPEED);
            for (; ticks >= 0; --ticks)
            {
                yield return new WaitForSeconds(TICK_SPEED);
                if (!location.CheckDeployableTick(player, chat))
                {
                    data.CurrentTeleportRequest = null;
                    yield break;
                }
            }
            Deploy(player, location, chat);
            data.CurrentTeleportRequest = null;
        }
    }
    private static void Deploy(UCPlayer player, IDeployable location, bool chat)
    {
        player.Player.teleportToLocationUnsafe(location.Position, location.Yaw);
        location.OnDeploy(player, chat);
    }
}

public interface IDeployable : ITranslationArgument
{
    Vector3 Position { get; }
    float Yaw { get; }
    bool CheckDeployable(UCPlayer player, CommandInteraction? ctx);
    bool CheckDeployableTick(UCPlayer player, bool chat);
    void OnDeploy(UCPlayer player, bool chat);
}
