using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Squads
{
    public class Orders
    {
        private static List<Order> orders;
    }

    public class Order : MonoBehaviour
    {
        public UCPlayer Commander { get; private set; }
        public Squad Squad { get; private set; }

        private void Initialize(Squad squad, UCPlayer commander)
        {
            Squad = squad;
            Commander = commander;
        }

        
    }
}
