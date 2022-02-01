using System;
using System.Collections.Generic;

namespace Uncreated.Warfare
{
    public class CooldownManager
    {
        public static Config<CooldownConfig> config;
        private readonly static List<Cooldown> cooldowns = new List<Cooldown>();

        public CooldownManager()
        {
            config = new Config<CooldownConfig>(Data.CooldownStorage, "config.json");
        }

        public static void StartCooldown(UCPlayer player, ECooldownType type, float seconds, params object[] data)
        {
            if (HasCooldown(player, type, out Cooldown existing))
                existing.timeAdded = DateTime.Now;
            else
                cooldowns.Add(new Cooldown(player, type, seconds, data));
        }
        public static bool HasCooldown(UCPlayer player, ECooldownType type, out Cooldown cooldown, params object[] data)
        {
            cooldowns.RemoveAll(c => c.player == null || c.Timeleft.TotalSeconds <= 0);
            cooldown = cooldowns.Find(c => c.player.CSteamID == player.CSteamID && c.type == type && c.data.Equals(data));
            return cooldown != null;
        }
        public static void RemoveCooldown(UCPlayer player, ECooldownType type) => cooldowns.RemoveAll(c => c.player == null || c.player.CSteamID == player.CSteamID && c.type == type);
        public static void RemoveCooldown(UCPlayer player) => cooldowns.RemoveAll(c => c.player.CSteamID == player.CSteamID);
        public static void RemoveCooldown(ECooldownType type) => cooldowns.RemoveAll(x => x.player == null || x.type == type);
        public static void OnGameStarting()
        {
            RemoveCooldown(ECooldownType.REQUEST_KIT);
            RemoveCooldown(ECooldownType.PREMIUM_KIT);
            RemoveCooldown(ECooldownType.REQUEST_VEHICLE);
        }


        public class CooldownConfig : ConfigData
        {
            public bool EnableCombatLogger;
            public float CombatCooldown;
            public float DeployMainCooldown;
            public float DeployFOBCooldown;
            public float RequestKitCooldown;
            public override void SetDefaults()
            {
                EnableCombatLogger = true;
                CombatCooldown = 120;
                DeployMainCooldown = 3;
                DeployFOBCooldown = 30;
                RequestKitCooldown = 120;
            }
            public CooldownConfig() { }
        }
    }
    public class Cooldown
    {
        public UCPlayer player;
        public ECooldownType type;
        public DateTime timeAdded;
        public float seconds;
        public object[] data;
        public TimeSpan Timeleft
        {
            get => TimeSpan.FromSeconds((seconds - (DateTime.Now - timeAdded).TotalSeconds) >= 0 ? (seconds - (DateTime.Now - timeAdded).TotalSeconds) : 0);
        }

        public Cooldown(UCPlayer player, ECooldownType type, float seconds, params object[] data)
        {
            this.player = player;
            this.type = type;
            timeAdded = DateTime.Now;
            this.seconds = seconds;
            this.data = data;
        }
        public override string ToString()
        {
            TimeSpan time = Timeleft;

            string line = string.Empty;
            if (time.Hours > 0)
                line += time.Hours + "h ";
            if (time.Minutes > 0)
                line += time.Minutes + "m ";
            if (time.Seconds > 0)
                line += time.Seconds + "s";
            return line;
        }
    }
    public enum ECooldownType
    {
        COMBAT,
        DEPLOY,
        AMMO,
        PREMIUM_KIT,
        REQUEST_KIT,
        REQUEST_VEHICLE,
        COMMAND_BOOST,
        COMMAND_WARN,
        WARNINGS,
        AMMO_VEHICLE,
        CHANGE_TEAMS
    }
}
