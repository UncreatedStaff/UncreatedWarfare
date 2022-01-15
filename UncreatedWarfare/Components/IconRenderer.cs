using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class IconManager
    {
        private static List<IconRenderer> icons = new List<IconRenderer>();

        public static void OnGameTick(int counter)
        {
            if (Data.Gamemode.EveryMinute)
            {
                foreach (var icon in icons)
                {
                    
                }
            }
        }

        public static void AddIcon(IconRenderer icon)
        {
            icons.Add(icon);
        }
        public static void RemoveIcon(IconRenderer icon)
        {
            icons.Remove(icon);
        }
    }


    public class IconRenderer : MonoBehaviour
    {
        public static List<IconRenderer> icons = new List<IconRenderer>();
        public Guid EffectGUID { get; private set; }
        public ushort EffectID { get; private set; }
        public float Range { get; private set; }
        public ulong Team { get; private set; }
        private float refreshTime;
        public void Initialize(Guid effectGUID, Vector3 point, float visibleRange, float refreshTimeSeconds, ulong team = 0)
        {
            transform.position = point;

            EffectGUID = effectGUID;
            refreshTime = refreshTimeSeconds;

            Team = team;

            Range = visibleRange;

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
        private IEnumerable<UCPlayer> GetPlayerlist()
        {
            var playerList = PlayerManager.OnlinePlayers.Where(p => (p.Position - transform.position).sqrMagnitude < Math.Pow(Range, 2));

            if (Team != 0)
            {
                playerList = playerList.Where(p => p.GetTeam() == Team);
            }

            return playerList;
        }
        public void SendIconToPlayers()
        {
            foreach (var player in GetPlayerlist())
                EffectManager.sendEffect(EffectID, player.connection, transform.position);
        }
    }
}
