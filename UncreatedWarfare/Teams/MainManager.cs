using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Teams
{
    public static class MainManager
    {

    }

    public class MainBase
    {
        public float x;
        public float y;
        public float z;
        public float rotation;

        public MainBase(float x, float y, float z, float rotation)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.rotation = rotation;
        }
        public Vector3 GetPosition() => new Vector3(x, y, z);
        public void SetPosition(Vector3 position)
        {
            x = position.x;
            y = position.y;
            z = position.z;
        }
    }
}
