using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using UnityEngine;
using Uncreated.Warfare.Gamemodes.Flags;

namespace Uncreated.Warfare.Squads
{
    public class Orders
    {
        public static List<Order> orders = new List<Order>();

        public static Order GiveOrder(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, string message)
        {
            Order  order = squad.Leader.Player.gameObject.AddComponent<Order>();
            order.Initialize(squad, commander, type, marker);
            orders.Add(order);

            order.UpdateUI();

            commander.Message("order_s_sent", message);
            foreach (var player in squad.Members)
                player.Message("order_s_received", commander.CharacterName, message);

            commander.Player.quests.sendSetMarker(false, marker);

            return order;
        }
        public static bool HasOrder(Squad squad, out Order order)
        {
            order = null;
            return (bool)(squad?.Leader.Player.TryGetComponent(out order));
        }
        public static bool CancelOrder(Order order)
        {
            bool success = orders.Remove(order);
            order.Cancel();
            return success;
        }
        public static void OnFOBBunkerBuilt(FOB fob, BuildableComponent buildable)
        {
            foreach (var pair in buildable.PlayerHits)
            {
                var player = UCPlayer.FromID(pair.Key);
                if (player != null &&
                    (float)pair.Value / buildable.Buildable.requiredHits >= 0.1F &&
                    HasOrder(player.Squad, out var order) &&
                    order.Type == EOrder.BUILDFOB &&
                    (fob.Position - order.Marker).sqrMagnitude <= Math.Pow(80, 2)
                )
                {
                    order.Fulfill();
                }
            }
        }
    }

    public class Order : MonoBehaviour
    {
        public UCPlayer Commander { get; private set; }
        public Squad Squad { get; private set; }
        public EOrder Type { get; private set; }
        public Vector3 Marker { get; private set; }
        public int TimeLeft { get; private set; }
        public bool IsActive { get; private set; }
        public Flag Flag { get; private set; }

        private OrderCondition Condition;

        private Coroutine loop;
         

        public void Initialize(Squad squad, UCPlayer commander, EOrder type, Vector3 marker, Flag flag = null)
        {
            Squad = squad;
            Commander = commander;
            Type = type;
            Marker = marker;
            Flag = flag;

            switch (Type)
            {
                case EOrder.ATTACK:
                    TimeLeft = 300;
                    break;
                case EOrder.DEFEND:
                    TimeLeft = 420;
                    break;
                case EOrder.BUILDFOB:
                    TimeLeft = 420;
                    break;
                case EOrder.MOVE:
                    TimeLeft = 240;
                    break;
            }

            Condition = new OrderCondition(type, squad, marker);
            IsActive = true;

            loop = StartCoroutine(Tick());

        }
        public void Fulfill()
        {
            if (!IsActive) return;

            int amount = 0;
            switch (Type)
            {
                case EOrder.ATTACK:
                    amount = 0;
                    break;
                case EOrder.DEFEND:
                    amount = 0;
                    break;
                case EOrder.BUILDFOB:
                    amount = 400;
                    foreach (var player in Squad.Members)
                    {
                        // TODO: send toast: "cancelled"
                        // TODO: send Objective UI effect
                        XP.XPManager.AddXP(player.Player, amount, "ORDER FULFILLED");
                    }
                    break;
                case EOrder.MOVE:
                    amount = 200;

                    foreach (var player in Condition.FullfilledPlayers)
                    {
                        if (player.IsOnline)
                        {
                            // TODO: send toast: "cancelled"
                            // TODO: send Objective UI effect
                            XP.XPManager.AddXP(player.Player, amount, "ORDER FULFILLED");
                        }
                    }

                    break;
            }

            if (Commander.IsOnline)
            {
                // TODO: send toast: "cancelled"
                // TODO: send Objective UI effect
                XP.XPManager.AddXP(Commander.Player, amount, "ORDER FULFILLED");
            }

            IsActive = false;
            StartCoroutine(Delete());
        }

        public void Cancel()
        {
            // TODO: clear Objective UI
            // TODO: send toast: "cancelled"
            IsActive = false;
            StartCoroutine(Delete());
        }
        public void TimeOut()
        {
            // TODO: clear Objective UI
            // TODO: send toast: "time out"
            IsActive = false;
            Destroy(this);
        }

        public void UpdateUI()
        {
            foreach (var player in Squad.Members)
            {
                // TODO: send Objective UI effect
            }
        }

        public IEnumerator<WaitForSeconds> Tick()
        {
            int counter = 0;
            float tickFrequency = 1;

            while (true)
            {
                // every 1 second

                TimeLeft--;

                if (counter % (5 / tickFrequency) == 0) // every 5 seconds
                {
                    if (Type == EOrder.MOVE)
                    {
                        Condition.UpdateData();
                        if (Condition.Check())
                            Fulfill();
                    }
                }

                if (counter % (30 / tickFrequency) == 0) // every 30 seconds
                {

                }
                if (counter % (60 / tickFrequency) == 0) // every 60 seconds
                {
                    
                }


                if (TimeLeft <= 0)
                {
                    TimeOut();
                }

                counter++;
                if (counter >= 60 / tickFrequency)
                    counter = 0;
                yield return new WaitForSeconds(tickFrequency);
            }
        }
        public IEnumerator<WaitForSeconds> Delete()
        {
            yield return new WaitForSeconds(20);
            // TODO: Clear UI
            Destroy(this);
        }
    }


    public class OrderCondition
    {
        EOrder Type;
        Squad Squad;
        Vector3 Marker;
        public List<UCPlayer> FullfilledPlayers;


        public OrderCondition(EOrder type, Squad squad, Vector3 marker)
        {
            Type = type;
            Squad = squad;
            Marker = marker;
            FullfilledPlayers = new List<UCPlayer>();
        }
        public bool Check()
        {
            if (Type == EOrder.MOVE)
            {
                return FullfilledPlayers.Count >= 0.75F * Squad.Members.Count;
            }
            return false;
        }
        public void UpdateData()
        {
            if (Type == EOrder.MOVE)
            {
                foreach (var player in Squad.Members)
                {
                    if ((player.Position - Marker).sqrMagnitude <= Math.Pow(40, 2))
                    {
                        if (!FullfilledPlayers.Contains(player))
                            FullfilledPlayers.Add(player);
                    }
                }
            }
        }
    }

    public enum EOrder
    {
        ATTACK,
        DEFEND,
        BUILDFOB,
        MOVE
    }
}
