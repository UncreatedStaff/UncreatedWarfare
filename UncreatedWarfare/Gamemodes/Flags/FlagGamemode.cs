using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SDG.Unturned;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public abstract class FlagGamemode : Gamemode
    {
        public List<Flag> Rotation = new List<Flag>();
        public List<Flag> AllFlags = new List<Flag>();
        public Dictionary<ulong, int> OnFlag = new Dictionary<ulong, int>();
        protected int _counter;
        protected abstract bool TimeToCheck();
        public FlagGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {

        }
        public override async Task Init()
        {
            this.State = EState.PAUSED;
            await base.Init();
        }
        protected override async Task EventLoopAction()
        {
            bool ttc = TimeToCheck();
            for (int i = 0; i < Rotation.Count; i++)
            {
                if (Rotation[i] == null) continue;
                List<Player> playersGone = Rotation[i].GetUpdatedPlayers(Provider.clients, out List<Player> newPlayers);
                foreach (Player player in playersGone)
                    RemovePlayerFromFlag(player, Rotation[i]);
                foreach (Player player in newPlayers)
                    AddPlayerOnFlag(player, Rotation[i]);
            }
            if(ttc)
                await EvaluatePoints();
        }
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
        public abstract Task LoadRotation();
        protected virtual async Task EvaluatePoints()
        {
            if (State == EState.ACTIVE)
                for (int i = 0; i < Rotation.Count; i++)
                    await Rotation[i].EvaluatePoints();
        }
        public virtual void InitFlag(Flag flag)
        {
            flag.OnPlayerEntered += PlayerEnteredFlagRadius;
            flag.OnPlayerLeft += PlayerLeftFlagRadius;
            flag.OnOwnerChanged += FlagOwnerChanged;
            flag.OnPointsChanged += FlagPointsChanged;
        }
        public virtual async Task ResetFlags()
        {
            foreach (Flag flag in Rotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                await flag.ResetFlag();
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
        protected abstract Task PlayerEnteredFlagRadius(Flag flag, Player player);
        protected abstract Task PlayerLeftFlagRadius(Flag flag, Player player);
        protected abstract Task FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag);
        protected abstract Task FlagPointsChanged(int NewPoints, int OldPoints, Flag flag);
        public override async Task OnLevelLoaded()
        {
            LoadAllFlags();
            await StartNextGame();
            await base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            base.Dispose();
            ResetFlags().GetAwaiter().GetResult();
            OnFlag.Clear();
            Rotation.Clear();
            _counter = 0;
        }
    }
}
