using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace UncreatedWarfare.Flags
{
    public class FlagManager
    {
        public List<Flag> FlagRotation { get; private set; }
        public string Preset { get; private set; }
        public Dictionary<ulong, int> OnFlag { get; private set; }
        public int ObjectiveT1;
        public int ObjectiveT2;
        public FlagManager(string Preset = "default")
        {
            this.Preset = Preset;
            FlagRotation = new List<Flag>();
            OnFlag = new Dictionary<ulong, int>();
            LoadNewFlags();
        }
        public void AddPlayerOnFlag(Player player, Flag flag) { 
            OnFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
            flag.EnterPlayer(player);
        }
        public void RemovePlayerFromFlag(Player player, Flag flag)
        {
            if (OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID) && OnFlag[player.channel.owner.playerID.steamID.m_SteamID] == flag.ID)
            {
                OnFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                flag.ExitPlayer(player);
            }
        }
        public static int FromMax(int cap) => Math.Abs(cap) >= Flag.MaxPoints ? UCWarfare.Config.FlagSettings.charactersForUI.Length - 1 : ((UCWarfare.Config.FlagSettings.charactersForUI.Length - 1) / Flag.MaxPoints) * Math.Abs(cap);
        public void ClearPlayersOnFlag() => OnFlag.Clear();
        public void LoadNewFlags()
        {
            FlagRotation.Clear();
            List<FlagData> flags = JSONMethods.ReadFlags(Preset);
            int i;
            flags.Sort((FlagData a, FlagData b) => a.id.CompareTo(b.id));
            for (i = 0; i < flags.Count; i++)
            {
                Flag flag = new Flag(flags[i]);
                flag.OnPlayerEntered += PlayerEnteredFlagRadius;
                flag.OnPlayerLeft += PlayerLeftFlagRadius;
                flag.OnOwnerChanged += FlagOwnerChanged;
                flag.OnPointsChanged += FlagPointsChanged;
                FlagRotation.Add(flag);
            }
            CommandWindow.Log("Loaded " + i.ToString() + " flags into memory and cleared any existing old lists.");
            ObjectiveT1 = 0;
            ObjectiveT2 = FlagRotation.Count - 1;
        }

        private void FlagPointsChanged(object sender, CaptureChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            // points value changed
        }

        private void FlagOwnerChanged(object sender, OwnerChangeEventArgs e)
        {
            Flag flag = sender as Flag;
            // owner of flag changed (full caputure or loss)
        }

        private void PlayerLeftFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked out of flag
        }

        private void PlayerEnteredFlagRadius(object sender, PlayerEventArgs e)
        {
            Flag flag = sender as Flag;
            // player walked into flag
        }

        public void ChangePreset(string NewPreset)
        {
            this.Preset = NewPreset;
            LoadNewFlags();
        }
        public void Dispose()
        {
            foreach(Flag flag in FlagRotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
            }
            FlagRotation.Clear();
            GC.SuppressFinalize(this);
        }
    }
}