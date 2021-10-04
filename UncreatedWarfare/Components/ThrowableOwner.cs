using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class ThrowableOwner : MonoBehaviour
    {
        public ulong ownerID;
        public Player owner;
        public GameObject projectile;
        public PlaytimeComponent ownerObj;
        public ushort ThrowableID;
        public void Set(UseableThrowable throwable, GameObject projectile, PlaytimeComponent owner)
        {
            this.ThrowableID = throwable.equippedThrowableAsset.id;
            this.owner = throwable.player;
            this.ownerID = throwable.player.channel.owner.playerID.steamID.m_SteamID;
            this.projectile = projectile;
            this.ownerObj = owner;
        }
        void OnDestroy()
        {
            if (ownerObj != null)
                UCWarfare.I.StartCoroutine(RemoveFromList(ownerObj));
        }
        private static IEnumerator<WaitForSeconds> RemoveFromList(PlaytimeComponent owner)
        {
            yield return new WaitForSeconds(0.1f);
            owner.thrown.RemoveAll(x => x == null);
        }
    }
}
