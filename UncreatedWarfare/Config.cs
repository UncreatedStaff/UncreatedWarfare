﻿using Rocket.API;
using System.Xml.Serialization;

namespace UncreatedWarfare
{
    public class Config : IRocketPluginConfiguration
    {
        [XmlElement("Modules")]
        public Modules Modules;
        [XmlElement("Flags")]
        public FlagSettings FlagSettings;
        [XmlElement("AdminLogger")]
        public AdminLoggerSettings AdminLoggerSettings;
        public ulong Team1ID;
        public ulong Team2ID;
        public void LoadDefaults()
        {
            Modules = new Modules();
            FlagSettings = new FlagSettings();
            AdminLoggerSettings = new AdminLoggerSettings();
            Team1ID = 1;
            Team2ID = 2;
        }
    }
    public class Modules
    {
        public bool PlayerList;
        public bool Kits;
        public bool Rename;
        public bool VehicleSpawning;
        public bool FOBs;
        public bool Roles;
        public bool UI;
        public bool AdminLogging;
        public bool MainCampPrevention;
        public bool Flags;
        public Modules()
        {
            this.PlayerList = true;
            this.Kits = true;
            this.Rename = true;
            this.VehicleSpawning = true;
            this.FOBs = true;
            this.Roles = true;
            this.UI = true;
            this.AdminLogging = true;
            this.MainCampPrevention = true;
            this.Flags = true;
        }
    }
    public class FlagSettings
    {
        public string NeutralColor;
        public string CurrentGamePreset;
        public float PlayerCheckSpeedSeconds;

        public bool UseUI;
        public bool UseChat;
        public ushort UIID;
        public string charactersForUI;

        public int RequiredPlayerDifferenceToCapture;
        public FlagSettings()
        {
            NeutralColor = "ffffff";
            CurrentGamePreset = "default";
            PlayerCheckSpeedSeconds = 0.5f;

            UseUI = true;
            UseChat = true;
            UIID = 32366;
            charactersForUI = "456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            RequiredPlayerDifferenceToCapture = 2;
        }
    }
    public class AdminLoggerSettings
    {
        public string AdminOffDutyGroup;
        public string AdminOnDutyGroup;
        public string InternOffDutyGroup;
        public string InternOnDutyGroup;
        public AdminLoggerSettings()
        {
            this.InternOnDutyGroup = "intern";
            this.InternOffDutyGroup = "intern-od";
            this.AdminOnDutyGroup = "admin";
            this.AdminOffDutyGroup = "admin-od";
        }
    }
}