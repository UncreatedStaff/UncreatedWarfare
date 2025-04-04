﻿using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Layouts.Insurgency;
internal static class CacheLocationsEditCommand
{
    internal const string Syntax = "/dev caches <add|next|start|nearest|remove|disable|enable|move|stop>";
    internal static readonly Dictionary<CacheLocation, BarricadeDrop> Drops = new Dictionary<CacheLocation, BarricadeDrop>(EqualityComparer<CacheLocation>.Default);
    internal static UniTask Execute(CommandContext ctx)
    {
        return UniTask.CompletedTask;
#if false
        await UniTask.SwitchToMainThread();

        ctx.AssertRanByPlayer();

        CacheLocationStore locations;
        if (Data.Is(out Gamemodes.Insurgency ins))
            locations = ins.Locations;
        else
        {
            locations = new CacheLocationStore();
            locations.Reload();
        }

        if (ctx.MatchParameter(0, "add", "create"))
        {
            CacheLocation location = new CacheLocation
            {
                Placer = ctx.CallerId.m_SteamID
            };

            if (ctx.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                location.Position = barricade.model.position;
                location.Rotation = barricade.model.rotation.eulerAngles;
                Drops[location] = barricade;
            }
            else
            {
                location.Position = ctx.Player.Position;
                location.Rotation = ctx.Player.Player.transform.rotation.eulerAngles;
            }

            if (ctx.TryGetRange(1, out string? name))
            {
                location.Name = name;
            }
            else
            {
                location.Name = "[" + new GridLocation(location.Position) + "] " + F.GetClosestLocationName(location.Position, true, true);
            }
            

            locations.AddCacheLocation(location);
            ctx.ReplyString($"<#ff99ff>Added cache location: <#0092d8>{location.Name}</color> at <#00716e>{location.Position:0.#}</color>, <#00716e>{location.Rotation:0.#}</color>.");
            if (ctx.Player.CacheLocationIndex < 0)
                ctx.ReplyString($"<#ffe7ff>Entered cache editor mode at: <#a77aa5>{location}</color> (<#ff99ff>{locations.Locations.Count - 1}</color>). <#a77aa5>/dev caches stop</color> to exit.");
            else
                ctx.ReplyString($"<#ffe7ff>Selected cache: <#a77aa5>{location.Name}</color> (<#ff99ff>{locations.Locations.Count - 1}</color>).");
            UpdateCacheIndex(ctx.Player, locations.Locations.Count - 1, locations);
        }
        else if (ctx.MatchParameter(0, "next", "continue", "start"))
        {
            if (locations.Locations.Count == 0)
                throw ctx.ReplyString("<#ffe7ff>No caches are in the cache file, use <#a77aa5>/dev caches add [name]</color> to add some.");

            if (ctx.Player.CacheLocationIndex < 0)
            {
                UpdateCacheIndex(ctx.Player, 0, locations);
                CacheLocation location = locations.Locations[0];
                ctx.ReplyString($"<#ffe7ff>Entered cache editor mode at: <#a77aa5>{location}</color> (<#ff99ff>{ctx.Player.CacheLocationIndex}</color>).");
            }
            else
            {
                if (ctx.Player.CacheLocationIndex >= locations.Locations.Count - 1)
                {
                    UpdateCacheIndex(ctx.Player, 0, locations);
                    CacheLocation location = locations.Locations[0];
                    ctx.ReplyString($"<#ffe7ff>Selected first cache: <#a77aa5>{location}</color> (<#ff99ff>0</color>).");
                }
                else
                {
                    UpdateCacheIndex(ctx.Player, ctx.Player.CacheLocationIndex + 1, locations);
                    CacheLocation location = locations.Locations[ctx.Player.CacheLocationIndex];
                    ctx.ReplyString($"<#ffe7ff>Selected next cache: <#a77aa5>{location}</color> (<#ff99ff>{ctx.Player.CacheLocationIndex}</color>).");
                }

            }
        }
        else if (ctx.MatchParameter(0, "nearest", "closest", "near"))
        {
            Vector3 pos = ctx.Player.Position;
            int index = -1;
            float smallest = -1f;
            for (int i = 0; i < locations.Locations.Count; ++i)
            {
                float amt = (pos - locations.Locations[i].Position).sqrMagnitude;
                if (smallest < 0f || amt < smallest)
                {
                    index = i;
                    smallest = amt;
                }
            }

            if (index == -1)
                throw ctx.ReplyString("<#ffe7ff>No caches are in the cache file, use <#a77aa5>/dev caches add [name]</color> to add some.");
            CacheLocation location = locations.Locations[index];
            UpdateCacheIndex(ctx.Player, index, locations);
            ctx.ReplyString($"<#ffe7ff>Selected cache: <#a77aa5>{location.Name}</color> (<#ff99ff>{locations.Locations.Count - 1}</color>).");
        }
        else if (ctx.MatchParameter(0, "remove", "delete"))
        {
            CheckEditOperation(ctx, locations);
            int index = ctx.Player.CacheLocationIndex;
            CacheLocation location = locations.Locations[index];
            locations.RemoveCacheLocation(location);
            ctx.ReplyString($"<#ff99ff>Deleted cache location: <#0092d8>{location}</color> at <#00716e>{location.Position:0.#}</color>, <#00716e>{location.Rotation:0.#}</color>.");
            if (Drops.TryGetValue(location, out BarricadeDrop drop) && !drop.GetServersideData().barricade.isDead && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
                BarricadeManager.destroyBarricade(drop, x, y, plant);

            if (locations.Locations.Count <= ctx.Player.CacheLocationIndex - 1)
            {
                ctx.Player.CacheLocationIndex = 0;
                location = locations.Locations[0];
                ctx.ReplyString($"<#ffe7ff>Selected first cache: <#a77aa5>{location}</color> (<#ff99ff>0</color>).");
            }
            else
            {
                ++ctx.Player.CacheLocationIndex;
                location = locations.Locations[ctx.Player.CacheLocationIndex];
                ctx.ReplyString($"<#ffe7ff>Selected next cache: <#a77aa5>{location}</color> (<#ff99ff>{ctx.Player.CacheLocationIndex}</color>).");
            }
            UpdateCacheIndex(ctx.Player, ctx.Player.CacheLocationIndex, locations);
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.CacheLocationIndex == index)
                {
                    ctx.ReplyString("<#ffe7ff>Closed cache editor mode (selected cache was deleted).");
                    UpdateCacheIndex(player, -1, locations);
                }
                else
                    --player.CacheLocationIndex;
            }
        }
        else if (ctx.MatchParameter(0, "disable"))
        {
            CheckEditOperation(ctx, locations);
            CacheLocation location = locations.Locations[ctx.Player.CacheLocationIndex];
            if (location.IsDisabled)
                throw ctx.ReplyString($"<#ffe7ff>{location} is already disabled.");
            location.IsDisabled = true;
            ctx.ReplyString($"<#ff99ff>Disabled cache location: <#0092d8>{location}</color>.");
            locations.Save();
        }
        else if (ctx.MatchParameter(0, "goto"))
        {
            CheckEditOperation(ctx, locations);
            CacheLocation location = locations.Locations[ctx.Player.CacheLocationIndex];
            ctx.ReplyString($"<#ff99ff>Teleported to: <#0092d8>{location}</color> at <#00716e>{location.Position:0.#}</color>, <#00716e>{location.Rotation.y:0.#}°</color>.");
            ctx.Player.Player.teleportToLocationUnsafe(location.Position + Vector3.up * 2, location.Rotation.y);
        }
        else if (ctx.MatchParameter(0, "enable"))
        {
            CheckEditOperation(ctx, locations);
            CacheLocation location = locations.Locations[ctx.Player.CacheLocationIndex];
            if (!location.IsDisabled)
                throw ctx.ReplyString($"<#ffe7ff>{location} is already enabled.");
            location.IsDisabled = false;
            ctx.ReplyString($"<#ff99ff>Enabled cache location: <#0092d8>{location}</color>.");
            locations.Save();
        }
        else if (ctx.MatchParameter(0, "move", "stloc", "stpos"))
        {
            CheckEditOperation(ctx, locations);
            CacheLocation location = locations.Locations[ctx.Player.CacheLocationIndex];
            string oldDefaultName = "[" + new GridLocation(location.Position) + "] " + F.GetClosestLocationName(location.Position, true, true);
            if (ctx.TryGetBarricadeTarget(out BarricadeDrop? barricade))
            {
                location.Position = barricade.model.position;
                location.Rotation = barricade.model.rotation.eulerAngles;
                if (Drops.TryGetValue(location, out BarricadeDrop drop) && !drop.GetServersideData().barricade.isDead && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
                    BarricadeManager.destroyBarricade(drop, x, y, plant);
                Drops[location] = barricade;
            }
            else
            {
                location.Position = ctx.Player.Position;
                location.Rotation = ctx.Player.Player.transform.rotation.eulerAngles;
                if (Drops.TryGetValue(location, out BarricadeDrop? drop) && !drop.GetServersideData().barricade.isDead)
                    BarricadeManager.ServerSetBarricadeTransform(drop.model, location.Position, location.GetPlacementAngle());
                else if (Gamemode.Config.BarricadeInsurgencyCache.TryGetAsset(out ItemBarricadeAsset? asset))
                {
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset), location.Position, location.GetPlacementAngle(), ctx.CallerId.m_SteamID, ctx.Player.Player.quests.groupID.m_SteamID);
                    if (t != null)
                    {
                        drop = BarricadeManager.FindBarricadeByRootTransform(t);
                        if (drop != null)
                            Drops.Add(location, drop);
                    }
                }
            }
            if (location.Name == null || location.Name.Equals(oldDefaultName, StringComparison.Ordinal))
                location.Name = "[" + new GridLocation(location.Position) + "] " + F.GetClosestLocationName(location.Position, true, true);
            
            ctx.ReplyString($"<#ff99ff>Moved cache location: <#0092d8>{location.Name}</color> to <#00716e>{location.Position:0.#}</color>, <#00716e>{location.Rotation:0.#}</color>.");
            locations.Save();
        }
        else if (ctx.MatchParameter(0, "stop", "break"))
        {
            UpdateCacheIndex(ctx.Player, -1, locations);
            ctx.ReplyString("<#ffe7ff>Closed cache editor mode.");
        }
        else throw ctx.SendCorrectUsage(Syntax);
#endif
    }

#if false
    private static void CheckEditOperation(CommandContext ctx, CacheLocationStore locations)
    {
        if (ctx.Player.CacheLocationIndex == -1)
            throw ctx.ReplyString("<#ffe7ff>You must be in edit mode to select caches. <#a77aa5>/dev caches start</color> to iterate through them.");

        if (locations.Locations.Count <= ctx.Player.CacheLocationIndex)
        {
            ctx.Player.CacheLocationIndex = 0;
            CacheLocation location = locations.Locations[0];
            throw ctx.ReplyString($"<#ffe7ff>Selected cache: <#a77aa5>{location}</color> (<#ff99ff>0</color>), please re-enter the command to confirm.");
        }
    }

    private static void UpdateCacheIndex(UCPlayer player, int index, CacheLocationStore locations)
    {
        int oldIndex = player.CacheLocationIndex;
        player.CacheLocationIndex = index;
        if (oldIndex != index && oldIndex >= 0 && oldIndex < locations.Locations.Count)
        {
            CacheLocation oldLocation = locations.Locations[oldIndex];
            if (Drops.TryGetValue(oldLocation, out BarricadeDrop drop))
            {
                if (!drop.GetServersideData().barricade.isDead && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
                    BarricadeManager.destroyBarricade(drop, x, y, plant);
            }
        }
        if (index >= 0 && index < locations.Locations.Count)
        {
            CacheLocation newLocation = locations.Locations[index];
            if (Gamemode.Config.BarricadeInsurgencyCache.TryGetAsset(out ItemBarricadeAsset? asset) && (!Drops.TryGetValue(newLocation, out BarricadeDrop drop) || drop.GetServersideData().barricade.isDead))
            {
                Transform? barricade = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset), newLocation.Position, newLocation.GetPlacementAngle(), player.Steam64, player.Player.quests.groupID.m_SteamID);
                if (barricade == null)
                    return;
                drop = BarricadeManager.FindBarricadeByRootTransform(barricade);
                if (drop != null)
                    Drops.Add(newLocation, drop);
                else
                    Drops.Remove(newLocation);
            }

            if (Drops.TryGetValue(newLocation, out BarricadeDrop drop2) && !drop2.GetServersideData().barricade.isDead && Gamemode.Config.EffectMarkerCacheAttack.TryGetAsset(out EffectAsset? effect))
                IconManager.AttachIcon(effect.GUID, drop2.model, player: player.Steam64);
        }
    }
#endif
}
