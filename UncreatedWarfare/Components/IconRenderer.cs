using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public static class IconManager
    {
        private static readonly List<IconRenderer> icons = new List<IconRenderer>();
        
        public static void OnLevelLoaded()
        {
            foreach (BarricadeDrop barricade in UCBarricadeManager.AllBarricades)
                OnBarricadePlaced(barricade);
        }
        public static void OnBarricadePlaced(BarricadeDrop drop, bool isFOBRadio = false)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (drop.model.TryGetComponent(out IconRenderer _))
                return;

            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            // FOB radio
            if (isFOBRadio)
                AttachIcon(Gamemode.Config.UI.MarkerRadio, drop.model, data.group, 3.5F);

            // FOB radio damaged
            if (Gamemode.Config.Barricades.FOBRadioDamagedGUID == data.barricade.asset.GUID)
                AttachIcon(Gamemode.Config.UI.MarkerRadioDamaged, drop.model, data.group, 3.5F);

            // FOB bunker
            if (Gamemode.Config.Barricades.FOBGUID == data.barricade.asset.GUID)
                AttachIcon(Gamemode.Config.UI.MarkerBunker, drop.model, data.group, 5.5F);

            // ammo bag
            if (Gamemode.Config.Barricades.AmmoBagGUID == data.barricade.asset.GUID)
                AttachIcon(Gamemode.Config.UI.MarkerAmmo, drop.model, data.group, 1);

            // ammo crate
            if (Gamemode.Config.Barricades.AmmoCrateGUID == data.barricade.asset.GUID)
                AttachIcon(Gamemode.Config.UI.MarkerAmmo, drop.model, data.group, 1.75F);

            // repair station
            if (Gamemode.Config.Barricades.RepairStationGUID == data.barricade.asset.GUID)
                AttachIcon(Gamemode.Config.UI.MarkerRepair, drop.model, data.group, 4.5F);

            if (Data.Is(out Insurgency _))
            {
                // cache
                if (Gamemode.Config.Barricades.InsurgencyCacheGUID == data.barricade.asset.GUID)
                    AttachIcon(Gamemode.Config.UI.MarkerCacheDefend, drop.model, data.group, 2.25F);
            }

            // buildable
            if (FOBManager.config.data.Buildables.Exists(b => b.foundationID == drop.asset.GUID && b.type != EBuildableType.FORTIFICATION))
                AttachIcon(Gamemode.Config.UI.MarkerBuildable, drop.model, data.group, 2F);
        }
        public static void DrawNewMarkers(UCPlayer player, bool clearOld)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            List<Guid> seenTypes = new List<Guid>();

            foreach (IconRenderer icon in icons)
            {
                if (clearOld)
                {
                    if (!seenTypes.Contains(icon.EffectGUID))
                    {
                        seenTypes.Add(icon.EffectGUID);
                        EffectManager.askEffectClearByID(icon.EffectID, player.connection);
                    }
                }

                if (icon.Team == 0 || (icon.Team != 0 && icon.Team == player.GetTeam()))
                {
                    EffectManager.sendEffect(icon.EffectID, player.connection, icon.Point);
                }
            }
        }
        public static void AttachIcon(Guid effectGUID, Transform transform, ulong team = 0, float yOffset = 0)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            IconRenderer icon = transform.gameObject.AddComponent<IconRenderer>();
            icon.Initialize(effectGUID, new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z), team);

            foreach (var player in PlayerManager.OnlinePlayers)
            {
                if (icon.Team == 0 || (icon.Team != 0 && icon.Team == player.GetTeam()))
                {
                    EffectManager.sendEffect(icon.EffectID, player.connection, icon.Point);
                }
            }

            icons.Add(icon);

            //IconRenderer[] components = transform.gameObject.GetComponents<IconRenderer>();
        }
        public static void DeleteIcon(IconRenderer icon)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (icon.Team == 0 || (icon.Team != 0 && icon.Team == player.GetTeam()))
                {
                    EffectManager.askEffectClearByID(icon.EffectID, player.connection);
                }
            }
            icons.Remove(icon);
            icon.Destroy();

            SpawnNewIconsOfType(icon.EffectGUID);
        }
        private static void SpawnNewIconsOfType(Guid effectGUID)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                foreach (IconRenderer icon in icons)
                {
                    if (icon.EffectGUID == effectGUID && icon.Team == 0 || (icon.Team != 0 && icon.Team == player.GetTeam()))
                    {
                        icon.SpawnNewIcon(player);
                    }
                }
            }
        }
    }


    public class IconRenderer : MonoBehaviour
    {
        public Guid EffectGUID { get; private set; }
        public ushort EffectID { get; private set; }
        public ulong Team { get; private set; }
        public Vector3 Point { get; private set; }
        public void Initialize(Guid effectGUID, Vector3 point, ulong team = 0)
        {
            Point = point;

            EffectGUID = effectGUID;

            Team = team;

            if (Assets.find(EffectGUID) is EffectAsset effect)
            {
                EffectID = effect.id;
            }
            else
                L.LogWarning("IconSpawner could not start: Effect asset not found: " + effectGUID);
        }

        public void Destroy()
        {
            Destroy(this);
        }
        public void SpawnNewIcon(UCPlayer player)
        {
            EffectManager.sendEffect(EffectID, player.connection, Point);
        }
    }
}
