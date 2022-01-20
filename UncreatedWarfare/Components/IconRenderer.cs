using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public static class IconManager
    {
        private static List<IconRenderer> icons = new List<IconRenderer>();
        

        public static void DrawNewMarkers(UCPlayer player, bool clearOld)
        {
            List<Guid> seenTypes = new List<Guid>();

            foreach (var icon in icons)
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
            var icon = transform.gameObject.AddComponent<IconRenderer>();
            icon.Initialize(effectGUID, new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z), team);

            foreach (var player in PlayerManager.OnlinePlayers)
            {
                if (icon.Team == 0 || (icon.Team != 0 && icon.Team == player.GetTeam()))
                {
                    EffectManager.sendEffect(icon.EffectID, player.connection, icon.Point);
                }
            }

            icons.Add(icon);
        }
        public static void DeleteIcon(IconRenderer icon)
        {
            foreach (var player in PlayerManager.OnlinePlayers)
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
            foreach (var player in PlayerManager.OnlinePlayers)
            {
                foreach (var icon in icons)
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
        public static List<IconRenderer> icons = new List<IconRenderer>();
        public Guid EffectGUID { get; private set; }
        public ushort EffectID { get; private set; }
        public float Range { get; private set; }
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
