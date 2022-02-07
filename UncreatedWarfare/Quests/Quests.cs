using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Quests.Types;

public abstract class BaseQuestData
{
    private EQuestType _type;
    private Dictionary<string, string> _translations;

    public int XPReward { get => 1; }
    public abstract int TickFrequencySeconds { get; }
    public EQuestType QuestType { get => _type; internal set => _type = value; }
    public Dictionary<string, string> Translations { get => _translations; internal set => _translations = value; }
    public virtual bool ResetOnGameEnd => false;
    public string Translate(string language, params object[] formatting)
    {
        if (Translations.TryGetValue(language, out string v) || (!language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal) && Translations.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out v)))
        {
            try
            {
                return string.Format(v, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Error formatting quest " + _type.ToString());
                L.LogError(ex);
            }
        }
        return string.Join(", ", formatting);
    }
    public string Translate(UCPlayer player, params object[] formatting) =>
        Translate(Data.Languages.TryGetValue(player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE, formatting);
    public abstract void OnPropertyRead(string propertyname, ref Utf8JsonReader reader);
    public abstract BaseQuestTracker CreateTracker(UCPlayer player);
}

public abstract class BaseQuestData<TTracker, TState, TDataParent> : BaseQuestData where TTracker : BaseQuestTracker where TState : IQuestState<TTracker, TDataParent>, new() where TDataParent : BaseQuestData<TTracker, TState, TDataParent>
{
    public TState GetNewState()
    {
        TState state = new TState();
        state.Init((TDataParent)this);
        return state;
    }
    public abstract TTracker CreateQuestTracker(UCPlayer player, ref TState state);
    public TTracker CreateQuestTracker(UCPlayer player)
    {
        TState state = GetNewState();
        return CreateQuestTracker(player, ref state);
    }
    public override BaseQuestTracker CreateTracker(UCPlayer player) => CreateQuestTracker(player);
}

public abstract class BaseQuestTracker : IDisposable
{
    public readonly UCPlayer Player;
    public BaseQuestData QuestData;
    protected bool isDisposed;
    protected bool _isCompleted;
    public bool IsCompleted { get; }
    public BaseQuestTracker(UCPlayer target)
    {
        this.Player = target;
    }
    public virtual void Tick() { }
    protected virtual void Cleanup() { }
    public virtual void ResetToDefaults() { }
    public abstract string Translate();
    public void OnGameEnd()
    {
        if (QuestData.ResetOnGameEnd)
        {
            _isCompleted = false;
            ResetToDefaults();
        }
    }
    public void TellCompleted()
    {
        _isCompleted = true;
        QuestManager.OnQuestCompleted(this);
    }
    public void TellUpdated()
    {
        QuestManager.OnQuestUpdated(this);
    }
    public void Dispose()
    {
        if (!isDisposed)
        {
            Cleanup();
            isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
[QuestData(EQuestType.KILL_ENEMIES)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    public DynamicIntegerValue KillCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuest>
    {
        public int KillThreshold;
        public void Init(KillEnemiesQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read() && prop.Equals("kills", StringComparison.Ordinal))
                    KillThreshold = reader.GetInt32();
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == Player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
            }
        }
        public override string Translate() => QuestData.Translate(Player, _kills, KillThreshold);
    }
}


[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Weapons;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("weapon_guids", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Weapons))
                Weapons = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeapon>
    {
        public int KillThreshold;
        public DynamicStringValue Weapons;
        public void Init(KillEnemiesQuestWeapon data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapons = data.Weapons;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("weapon_guids", StringComparison.Ordinal))
                    {
                        if (!reader.TryReadStringValue(out Weapons))
                            Weapons = new DynamicStringValue(null);
                    }
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("weapon_guids", Weapons.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly Guid[] weapons;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            string[] weapons = questState.Weapons.GetSetValue();
            this.weapons = new Guid[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                if (!Guid.TryParse(weapons[i].ToString(), out this.weapons[i]))
                    L.LogWarning("Failed to parse " + weapons[i] + " as a GUID in KILL_ENEMIES_WITH_WEAPON quest.");
            }
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == Player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && weapons.Contains(kill.item))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(Player, _kills, KillThreshold, string.Join(", ", weapons.Select(x => Assets.find(x) is ItemAsset asset ? asset.itemName : x.ToString())));
    }
}

[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT)]
public class KillEnemiesQuestKit : BaseQuestData<KillEnemiesQuestKit.Tracker, KillEnemiesQuestKit.State, KillEnemiesQuestKit>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kit", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits))
                Kits = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKit>
    {
        public int KillThreshold;
        public string Kit;
        public void Init(KillEnemiesQuestKit data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kit = data.Kits.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("kit", StringComparison.Ordinal))
                        Kit = reader.GetString();
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit ?? string.Empty);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly string Kit;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Kit = questState.Kit;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == Player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && 
                KitManager.HasKit(kill.killer, out Kit kit) && kit.Name.Equals(Kit, StringComparison.OrdinalIgnoreCase))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate()
        {
            string lang = Data.Languages.TryGetValue(Player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE;
            string signText = KitManager.KitExists(Kit, out Kit kit) ? (kit.SignTexts.TryGetValue(lang, out string st) ? st :
                (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase) && kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out st) ? st : kit.Name)) : Kit;
            return QuestData.Translate(Player, _kills, KillThreshold, signText);
        }
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KITS)]
public class KillEnemiesQuestKits : BaseQuestData<KillEnemiesQuestKits.Tracker, KillEnemiesQuestKits.State, KillEnemiesQuestKits>
{
    public DynamicIntegerValue KillCount;
    public DynamicStringValue Kits;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("kits", StringComparison.Ordinal))
        {
            if (!reader.TryReadStringValue(out Kits))
                Kits = new DynamicStringValue(null);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKits>
    {
        public int KillThreshold;
        public DynamicStringValue Kits;
        public void Init(KillEnemiesQuestKits data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kits = data.Kits;
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("kits", StringComparison.Ordinal))
                        if (!reader.TryReadStringValue(out Kits))
                            Kits = new DynamicStringValue(null);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kits", Kits.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly string[] Kits;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Kits = questState.Kits.GetSetValue();
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == Player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && 
                KitManager.HasKit(kill.killer, out Kit kit))
            {
                for (int i = 0; i < Kits.Length; i++)
                {
                    if (kit.Name.Equals(Kits[i], StringComparison.OrdinalIgnoreCase))
                    {
                        _kills++;
                        if (_kills >= KillThreshold)
                            TellCompleted();
                        else
                            TellUpdated();
                        return;
                    }
                }
            }
        }
        public override string Translate()
        {
            string kits = string.Join(", ", Kits.Select(Kit =>
            {
                string lang = Data.Languages.TryGetValue(Player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE;
                return KitManager.KitExists(Kit, out Kit kit) ? (kit.SignTexts.TryGetValue(lang, out string st) ? st :
                    (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase) && kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out st) ? st : kit.Name)) : Kit;
            }));
            return QuestData.Translate(Player, _kills, KillThreshold, kits);
        }
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_KIT_CLASS)]
public class KillEnemiesQuestKitClass : BaseQuestData<KillEnemiesQuestKitClass.Tracker, KillEnemiesQuestKitClass.State, KillEnemiesQuestKitClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EClass> Class;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("class", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Class))
            {
                Class = new DynamicEnumValue<EClass>(EClass.NONE);
                L.LogWarning("Invalid class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKitClass>
    {
        public int KillThreshold;
        public EClass Class;
        public void Init(KillEnemiesQuestKitClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void ReadQuestState(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                if (reader.Read())
                {
                    if (prop.Equals("kills", StringComparison.Ordinal))
                        KillThreshold = reader.GetInt32();
                    else if (prop.Equals("class", StringComparison.Ordinal))
                        reader.TryReadEnumValue(out Class);
                }
            }
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly EClass Class;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold;
            Class = questState.Class;
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == Player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && 
                KitManager.HasKit(kill.killer, out Kit kit) && kit.Class == Class)
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(Player, _kills, KillThreshold, Class.ToString());
    }
}