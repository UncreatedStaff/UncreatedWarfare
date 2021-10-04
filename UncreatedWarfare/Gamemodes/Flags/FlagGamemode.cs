using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public abstract class FlagGamemode : Gamemode
    {
        public List<Flag> Rotation = new List<Flag>();
        public List<Flag> AllFlags = new List<Flag>();
        public Dictionary<ulong, int> OnFlag = new Dictionary<ulong, int>();
        protected int _counter;
        protected int _counter2;
        protected abstract bool TimeToCheck();
        protected abstract bool TimeToTicket();
        public FlagGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        { }
        public override void Init()
        {
            this.State = EState.PAUSED;
            base.Init();
        }
        protected override void EventLoopAction()
        {
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer current = Provider.clients[i];
                try
                {
                    _ = current.player.transform;
                }
                catch (NullReferenceException)
                {
                    F.Log($"Kicking {F.GetPlayerOriginalNames(current).PlayerName} ({current.playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                    Provider.kick(current.playerID.steamID,
                        $"Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{UCWarfare.Config.DiscordInviteCode}).");
                }
            }
            bool ttc = TimeToCheck();

            FOBManager.OnGameTick(TicketCounter);
            for (int i = 0; i < Rotation.Count; i++)
            {
                if (Rotation[i] == null) continue;
                List<Player> playersGone = Rotation[i].GetUpdatedPlayers(Provider.clients, out List<Player> newPlayers);
                foreach (Player player in playersGone)
                    RemovePlayerFromFlag(player, Rotation[i]);
                foreach (Player player in newPlayers)
                    AddPlayerOnFlag(player, Rotation[i]);
            }
            if (TimeToTicket())
                EvaluateTickets();
            if (ttc)
            {
                EvaluatePoints();
                OnEvaluate();
            }

            TicketCounter++;
            if (TicketCounter >= 60)
                TicketCounter = 0;
        }
        protected uint TicketCounter = 0;
        public virtual void EvaluateTickets()
        { }
        public virtual void OnEvaluate()
        { }
        public void LoadAllFlags()
        {
            AllFlags.Clear();
            List<FlagData> flags = JSONMethods.ReadFlags();
            flags.Sort((FlagData a, FlagData b) => a.id.CompareTo(b.id));
            int i;
            for (i = 0; i < flags.Count; i++)
            {
                AllFlags.Add(new Flag(flags[i], this) { index = i });
            }
            F.Log("Loaded " + i.ToString(Data.Locale) + " flags into memory and cleared any existing old flags.", ConsoleColor.Magenta);
        }
        public virtual void PrintFlagRotation()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Rotation.Count; i++)
            {
                sb.Append(i.ToString(Data.Locale) + ") " + Rotation[i].Name + '\n');
            }
            F.Log(sb.ToString(), ConsoleColor.Green);
        }
        public abstract void LoadRotation();
        protected virtual void EvaluatePoints()
        {
            if (State == EState.ACTIVE)
                for (int i = 0; i < Rotation.Count; i++)
                    Rotation[i].EvaluatePoints();
        }
        public virtual void InitFlag(Flag flag)
        {
            flag.OnPlayerEntered += PlayerEnteredFlagRadius;
            flag.OnPlayerLeft += PlayerLeftFlagRadius;
            flag.OnOwnerChanged += FlagOwnerChanged;
            flag.OnPointsChanged += FlagPointsChanged;
        }
        public virtual void ResetFlags()
        {
            foreach (Flag flag in Rotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                flag.ResetFlag();
            }
            Rotation.Clear();
        }
        protected virtual void RemovePlayerFromFlag(Player player, Flag flag)
        {
            if (OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID) && OnFlag[player.channel.owner.playerID.steamID.m_SteamID] == flag.ID)
            {
                OnFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                flag.ExitPlayer(player);
            }
        }
        public virtual void AddPlayerOnFlag(Player player, Flag flag)
        {
            if (OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (OnFlag[player.channel.owner.playerID.steamID.m_SteamID] != flag.ID)
                {
                    Flag oldFlag = Rotation.FirstOrDefault(f => f.ID == OnFlag[player.channel.owner.playerID.steamID.m_SteamID]);
                    if (oldFlag == default(Flag)) OnFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                    else RemovePlayerFromFlag(player, oldFlag); // remove the player from their old flag first in the case of teleporting from one flag to another.
                    OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
                }
            }
            else OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
            flag.EnterPlayer(player);
        }
        protected abstract void PlayerEnteredFlagRadius(Flag flag, Player player);
        protected abstract void PlayerLeftFlagRadius(Flag flag, Player player);
        protected abstract void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag);
        protected abstract void FlagPointsChanged(int NewPoints, int OldPoints, Flag flag);
        public override void OnLevelLoaded()
        {
            LoadAllFlags();
            StartNextGame(true);
            base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            base.Dispose();
            ResetFlags();
            OnFlag.Clear();
            Rotation.Clear();
            _counter = 0;
        }
    }
}
