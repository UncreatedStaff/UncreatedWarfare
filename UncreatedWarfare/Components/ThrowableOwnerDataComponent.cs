using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class ThrowableOwnerDataComponent : MonoBehaviour
    {
        public Player owner;
        public ItemThrowableAsset asset;
        public GameObject projectile;
        public PlaytimeComponent ownerObj;
        public void Set(UseableThrowable throwable, GameObject projectile, PlaytimeComponent owner)
        {
            this.asset = throwable.equippedThrowableAsset;
            this.owner = throwable.player;
            this.projectile = projectile;
            this.ownerObj = owner;
        }
        void OnDestroy()
        {
            UCWarfare.I.StartCoroutine(RemoveFromList(ownerObj.thrown.FindIndex(x => x.GetInstanceID() == this.GetInstanceID()), ownerObj));
        }
        private static IEnumerator<WaitForSeconds> RemoveFromList(int index, PlaytimeComponent owner)
        {
            yield return new WaitForSeconds(1.0f);
            if (index < owner.thrown.Count)
            {
                if (owner.thrown[index] == null) owner.thrown.RemoveAt(index);
                else owner.thrown.RemoveAll(x => x == null);
            }
        }
    }
}
