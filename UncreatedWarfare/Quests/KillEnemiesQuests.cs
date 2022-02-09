using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Quests.Types;

// Attribute links the kit to the Type
[QuestData(EQuestType.KILL_ENEMIES)]
public class KillEnemiesQuest : BaseQuestData<KillEnemiesQuest.Tracker, KillEnemiesQuest.State, KillEnemiesQuest>
{
    // dynamic int allows for constants, ranges, or sets
    public DynamicIntegerValue KillCount;
    // > 0 will run the Tick function in trackers
    public override int TickFrequencySeconds => 0;
    // just copy paste this couldn't do it with generics.
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    // used to read from JSON, add a case for each property.
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
    }
    // States keep track of a set variation, used to keep quests synced between players and over restarts.
    public struct State : IQuestState<Tracker, KillEnemiesQuest>
    {
        // in this case we store the resulting value of kill threshold in Init(..)
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuest data)
        {
            this.KillThreshold = data.KillCount.GetValue(); // get value picks a random value if its a range or set, otherwise returns the constant.
        }
        // same as above, reading from json, except we're reading the state this time.
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
        }
        // writing state, not sure if this will be used or not.
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
        }
    }
    
    // one tracker is created per player working on the quest. Add the notify interfaces defined in QuestsMisc.cs and add cases for them in QuestManager under the events region
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold;
        private int _kills;
        // loads a tracker from a state instead of randomly picking values each time.
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue(); // insisting for a value asks for ONE value (defined with a $, otherwise it returns 0)
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
            }
        }
        public override void ResetToDefaults() => _kills = 0;
        // translate the description of the quest, pass any data that will show up in the description once we make them
        //                                         in this case, "Kill {_kills}/{KillThreshold} enemies."
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON)]
public class KillEnemiesQuestWeapon : BaseQuestData<KillEnemiesQuestWeapon.Tracker, KillEnemiesQuestWeapon.State, KillEnemiesQuestWeapon>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemWeaponAsset> Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("weapons", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Weapon))
                Weapon = new DynamicAssetValue<ItemWeaponAsset>(EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ALL);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeapon>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        public void Init(KillEnemiesQuestWeapon data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Weapon.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("weapons", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemWeaponAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("weapons", Weapon);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly DynamicAssetValue<ItemWeaponAsset>.Choice Weapon;
        private string translationCache1;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Weapon = questState.Weapon;
            translationCache1 = ((KillEnemiesQuestWeapon)QuestData).Weapon.ToString(); // TODO: Look better
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && kill.item != default)
            {
                if (Weapon.IsMatch(kill.item))
                {
                    _kills++;
                    if (_kills >= KillThreshold)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, translationCache1);
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
            if (!reader.TryReadStringValue(out Kits, true))
                Kits = new DynamicStringValue(true, EDynamicValueType.ANY, EChoiceBehavior.ALLOW_ONE);
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestKit>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<string>.IChoice Kit;
        public void Init(KillEnemiesQuestKit data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Kit = data.Kits.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("kit", StringComparison.Ordinal))
                Kit = DynamicStringValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("kit", Kit);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<string>.IChoice Kit;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Kit = questState.Kit;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && Kit.IsMatch(kit.Name))
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
            return QuestData.Translate(_player, _kills, KillThreshold, Kit.ToString());
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
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EClass>.IChoice Class;
        public void Init(KillEnemiesQuestKitClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<EClass>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EClass>.IChoice Class;
        private int _kills;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Class = questState.Class;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public override void ResetToDefaults() => _kills = 0;
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() &&
                KitManager.HasKit(kill.killer, out Kit kit) && Class.IsMatch(kit.Class))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_WEAPON_CLASS)]
public class KillEnemiesQuestWeaponClass : BaseQuestData<KillEnemiesQuestWeaponClass.Tracker, KillEnemiesQuestWeaponClass.State, KillEnemiesQuestWeaponClass>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EWeaponClass> Class;
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
                Class = new DynamicEnumValue<EWeaponClass>(EWeaponClass.UNKNOWN);
                L.LogWarning("Invalid weapon class in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestWeaponClass>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EWeaponClass>.IChoice Class;
        public void Init(KillEnemiesQuestWeaponClass data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Class = data.Class.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("class", StringComparison.Ordinal))
                Class = DynamicEnumValue<EWeaponClass>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("class", Class);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EWeaponClass>.IChoice Class;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Class = questState.Class;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam() && Class.IsMatch(kill.item.GetWeaponClass()))
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Class.ToString());
    }
}

[QuestData(EQuestType.KILL_ENEMIES_WITH_BRANCH)]
public class KillEnemiesQuestBranch : BaseQuestData<KillEnemiesQuestBranch.Tracker, KillEnemiesQuestBranch.State, KillEnemiesQuestBranch>
{
    public DynamicIntegerValue KillCount;
    public DynamicEnumValue<EBranch> Branch;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("branch", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Branch))
            {
                Branch = new DynamicEnumValue<EBranch>(EBranch.DEFAULT);
                L.LogWarning("Invalid branch in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestBranch>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public IDynamicValue<EBranch>.IChoice Branch;
        public void Init(KillEnemiesQuestBranch data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Branch = data.Branch.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("branch", StringComparison.Ordinal))
                Branch = DynamicEnumValue<EBranch>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("branch", Branch);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly IDynamicValue<EBranch>.IChoice Branch;
        private int _kills;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Branch = questState.Branch;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && Branch.IsMatch(_player.Branch) && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, Branch.ToString());
    }
}
[QuestData(EQuestType.KILL_ENEMIES_WITH_TURRET)]
public class KillEnemiesQuestTurret : BaseQuestData<KillEnemiesQuestTurret.Tracker, KillEnemiesQuestTurret.State, KillEnemiesQuestTurret>
{
    public DynamicIntegerValue KillCount;
    public DynamicAssetValue<ItemGunAsset> Turrets;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("kills", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out KillCount))
                KillCount = new DynamicIntegerValue(20);
        }
        else if (propertyname.Equals("turret", StringComparison.Ordinal))
        {
            if (!reader.TryReadAssetValue(out Turrets))
            {
                L.LogWarning("Invalid turret GUID(s) in quest " + QuestType);
            }
        }
    }
    public struct State : IQuestState<Tracker, KillEnemiesQuestTurret>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public DynamicAssetValue<ItemGunAsset>.Choice Weapon;
        public void Init(KillEnemiesQuestTurret data)
        {
            this.KillThreshold = data.KillCount.GetValue();
            this.Weapon = data.Turrets.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("turret", StringComparison.Ordinal))
                Weapon = DynamicAssetValue<ItemGunAsset>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", KillThreshold);
            writer.WriteProperty("turret", Weapon.ToString());
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnKill
    {
        private readonly int KillThreshold = 0;
        private readonly DynamicAssetValue<ItemGunAsset>.Choice Weapon;
        private int _kills;
        private string translationCache1;
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
            Weapon = questState.Weapon;
            translationCache1 = Weapon.ToStringAssetNames();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                InteractableVehicle veh = kill.killer.movement.getVehicle();
                if (veh == null) return;
                for (int i = 0; i < veh.turrets.Length; i++)
                {
                    if (veh.turrets[i] != null && veh.turrets[i].player?.player.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && 
                        veh.turrets[i].turret != null && Weapon.IsMatch(veh.turrets[i].turret.itemID))
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
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold, translationCache1);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_SQUAD)]
public class KillEnemiesQuestSquad : BaseQuestData<KillEnemiesQuestSquad.Tracker, KillEnemiesQuestSquad.State, KillEnemiesQuestSquad>
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
    public struct State : IQuestState<Tracker, KillEnemiesQuestSquad>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuestSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && _player.Squad != null && _player.Squad.Members.Count > 1 && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_IN_FULL_SQUAD)]
public class KillEnemiesQuestFullSquad : BaseQuestData<KillEnemiesQuestFullSquad.Tracker, KillEnemiesQuestFullSquad.State, KillEnemiesQuestFullSquad>
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
    public struct State : IQuestState<Tracker, KillEnemiesQuestFullSquad>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuestFullSquad data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && _player.Squad != null && _player.Squad.IsFull() && kill.dead.GetTeam() != kill.killer.GetTeam())
            {
                _kills++;
                if (_kills >= KillThreshold)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_DEFENSE)]
public class KillEnemiesQuestDefense : BaseQuestData<KillEnemiesQuestDefense.Tracker, KillEnemiesQuestDefense.State, KillEnemiesQuestDefense>
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
    public struct State : IQuestState<Tracker, KillEnemiesQuestDefense>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuestDefense data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 1 && fr.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position)) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 2 && fr.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position)))
                        {
                            goto add;
                        }
                    }
                    else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                    {
                        if (inv.DefendingTeam == team)
                        {
                            if (inv.AttackingTeam == 1)
                            {
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                        }
                    }
                    else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                    {
                        if (ins.DefendingTeam == team)
                        {
                            for (int i = 0; i < ins.Caches.Count; i++)
                            {
                                Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                                if (cache != null && cache.IsActive && (cache.Cache.Position - kill.killer.transform.position).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
            add:
            _kills++;
            if (_kills >= KillThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}
[QuestData(EQuestType.KILL_ENEMIES_ON_POINT_ATTACK)]
public class KillEnemiesQuestAttack : BaseQuestData<KillEnemiesQuestAttack.Tracker, KillEnemiesQuestAttack.State, KillEnemiesQuestAttack>
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
    public struct State : IQuestState<Tracker, KillEnemiesQuestAttack>
    {
        public IDynamicValue<int>.IChoice KillThreshold;
        public void Init(KillEnemiesQuestAttack data)
        {
            this.KillThreshold = data.KillCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("kills", StringComparison.Ordinal))
                KillThreshold = DynamicIntegerValue.ReadChoice(ref reader);
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
        public override void ResetToDefaults() => _kills = 0;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            KillThreshold = questState.KillThreshold.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("kills", StringComparison.Ordinal))
                _kills = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("kills", _kills);
        }
        public void OnKill(UCWarfare.KillEventArgs kill)
        {
            ulong team = kill.killer.GetTeam();
            if (kill.killer.channel.owner.playerID.steamID.m_SteamID == _player.Steam64 && kill.dead.GetTeam() != team)
            {
                if (Data.Is(out Gamemodes.Interfaces.IFlagTeamObjectiveGamemode fr))
                {
                    if (Data.Is<Gamemodes.Flags.TeamCTF.TeamCTF>(out _))
                    {
                        if (
                            (team == 1 && fr.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == 2 && fr.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position)) ||
                            (team == 2 && fr.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == 1 && fr.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position)))
                        {
                            goto add;
                        }
                    }
                    else if (Data.Is(out Gamemodes.Flags.Invasion.Invasion inv))
                    {
                        if (inv.AttackingTeam == team)
                        {
                            if (inv.AttackingTeam == 1)
                            {
                                if (inv.ObjectiveTeam1 != null && fr.ObjectiveTeam1.Owner == TeamManager.Other(team) && inv.ObjectiveTeam1.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                            else if (inv.AttackingTeam == 2)
                            {
                                if (inv.ObjectiveTeam2 != null && fr.ObjectiveTeam2.Owner == TeamManager.Other(team) && inv.ObjectiveTeam2.PlayerInRange(kill.killer.transform.position))
                                    goto add;
                            }
                        }
                    }
                    else if (Data.Is(out Gamemodes.Insurgency.Insurgency ins))
                    {
                        if (ins.AttackingTeam == team)
                        {
                            for (int i = 0; i < ins.Caches.Count; i++)
                            {
                                Gamemodes.Insurgency.Insurgency.CacheData cache = ins.Caches[i];
                                if (cache != null && cache.IsActive && (cache.Cache.Position - kill.killer.transform.position).sqrMagnitude > 3600) // 60m
                                    goto add;
                            }
                        }
                    }
                }
            }
            return;
            add:
            _kills++;
            if (_kills >= KillThreshold)
                TellCompleted();
            else
                TellUpdated();
        }
        public override string Translate() => QuestData.Translate(_player, _kills, KillThreshold);
    }
}