using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Kits
{
    public class KitSaver : JSONSaver<KitSave>
    {
        public List<KitSave> ActiveKits;
        public KitSaver()
            : base(UCWarfare.KitsStorage + "savedkits.json")
        {
            ActiveKits = new List<KitSave>();
        }
        protected override string LoadDefaults() => "[]";
        public void AddSaveForPlayer(UnturnedPlayer player, string kitName) => AddObjectToSave(new KitSave(player.CSteamID.m_SteamID, kitName));
        public void RemoveSaveOfPlayer(UnturnedPlayer player) => RemoveFromSaveWhere(ks => ks.Steam64 == player.CSteamID.m_SteamID);
        public bool HasSave(CSteamID steamID, out string KitName)
        {
            bool result = ObjectExists(ks => ks.Steam64 == steamID.m_SteamID, out var save);
            KitName = save.KitName;
            return result;
        }
    }

    public class KitSave
    {
        public ulong Steam64;
        public string KitName;

        public KitSave(ulong steam64, string kitName)
        {
            Steam64 = steam64;
            KitName = kitName;
        }
    }
}
