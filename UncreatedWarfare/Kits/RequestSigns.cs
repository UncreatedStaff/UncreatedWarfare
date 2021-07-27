using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDG.Unturned;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits
{
    public class RequestSigns : JSONSaver<RequestSign>
    {
        public RequestSigns() : base(Data.StructureStorage + "request_signs.json") { }
        protected override string LoadDefaults() => "[]";
        public static async Task DropAllSigns()
        {
            foreach(RequestSign sign in ActiveObjects)
            {
                await sign.SpawnCheck(false);
                if (!sign.exists)
                    F.LogError("Failed to spawn sign " + sign.kit_name);
            }
            Save();
        }
        public static bool AddRequestSign(InteractableSign sign, out RequestSign signadded)
        {
            signadded = default;
            if(!ObjectExists(x => x.transform.Position == sign.transform.position && x.transform.Rotation == sign.transform.rotation, out _))
            {
                signadded = new RequestSign(sign);
                AddObjectToSave(signadded);
                return true;
            }
            return false;
        }
        public static async Task RemoveRequestSign(RequestSign sign)
        {
            RemoveWhere(x => x.transform == sign.transform);
            await sign.InvokeUpdate();
        }
        public static void RemoveRequestSigns(string kitname) => RemoveWhere(x => x.kit_name == kitname);
        public static bool SignExists(InteractableSign sign, out RequestSign found) => ObjectExists(s => s != default && sign != default && 
        BarricadeManager.tryGetInfo(sign.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) && s.instance_id == region.drops[index].instanceID, out found);
        public static bool SignExists(uint instance_id, out RequestSign found) => ObjectExists(s => s != default && s.instance_id == instance_id, out found);
        public static bool SignExists(string kitName, out RequestSign sign) => ObjectExists(x => x.kit_name == kitName, out sign);
        public static void UpdateSignsWithName(string kitName, Action<RequestSign> action) => UpdateObjectsWhere(rs => rs.kit_name == kitName, action);
        public static async Task InvokeLangUpdateForSignsOfKit(SteamPlayer player, string kitName)
        {
            List<RequestSign> s = GetObjectsWhere(x => x.kit_name == kitName);
            for (int i = 0; i < s.Count; i++)
            {
                await s[i].InvokeUpdate(player);
            }
        }
        public static async Task InvokeLangUpdateForSignsOfKit(string kitName)
        {
            List<RequestSign> s = GetObjectsWhere(x => x.kit_name == kitName);
            for (int i = 0; i < s.Count; i++)
            {
                await s[i].InvokeUpdate();
            }
        }
        public static void SetOwner(RequestSign sign, ulong newOwner) => SetProperty(sign, nameof(sign.owner), newOwner, out _, out _, out _);
        public static void SetGroupOwner(RequestSign sign, ulong group) => SetProperty(sign, nameof(sign.group), group, out _, out _, out _);
    }
    public class RequestSign
    {
        [JsonIgnore]
        public Kit Kit
        {
            get
            {
                if (KitManager.KitExists(kit_name, out Kit k))
                    return k;
                else return default;
            }
        }
        [JsonIgnore]
        public string SignText { 
            get => "sign_" + kit_name; 
            set
            {
                if(value == default) kit_name = TeamManager.DefaultKit;
                else if (value.Length > 5 && value.StartsWith("sign_"))
                    kit_name = value.Substring(5);
                else kit_name = value;
            }
        }
        [JsonSettable]
        public string kit_name;
        public SerializableTransform transform;
        [JsonSettable]
        public ushort sign_id;
        [JsonSettable]
        public ulong owner;
        [JsonSettable]
        public ulong group;
        [JsonIgnore]
        public Transform barricadetransform;
        public uint instance_id;
        [JsonIgnore]
        public bool exists;
        [JsonConstructor]
        public RequestSign(string kit_name, SerializableTransform transform, ushort sign_id, ulong owner, ulong group, uint instance_id)
        {
            this.kit_name = kit_name;
            this.transform = transform;
            this.sign_id = sign_id;
            this.owner = owner;
            this.group = group;
            this.instance_id = instance_id;
            this.exists = false;
        }
        public RequestSign(InteractableSign sign)
        {
            if (sign == default) throw new ArgumentNullException(nameof(sign));
            if (BarricadeManager.tryGetInfo(sign.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
            {
                this.sign_id = region.barricades[index].barricade.id;
                this.instance_id = region.drops[index].instanceID;
                this.transform = new SerializableTransform(sign.transform);
                this.barricadetransform = sign.transform;
                this.SignText = sign.text;
                this.group = sign.group.m_SteamID;
                this.owner = sign.owner.m_SteamID;
            } else throw new ArgumentNullException(nameof(sign));
        }
        public RequestSign()
        {
            this.kit_name = "default";
            this.transform = SerializableTransform.Zero;
            this.sign_id = 0;
            this.owner = 0;
            this.group = 0;
            this.instance_id = 0;
            this.barricadetransform = default;
            this.exists = false;
        }
        public async Task InvokeUpdate(SteamPlayer player)
        {
            if (barricadetransform != default)
                if (BarricadeManager.tryGetInfo(barricadetransform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    await F.InvokeSignUpdateFor(player, x, y, plant, index, SignText);
                else F.LogError("Failed to find barricade!");
            else
            {
                BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    if (BarricadeManager.tryGetInfo(drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                        await F.InvokeSignUpdateFor(player, x, y, plant, index, region, true, SignText);
                    else F.LogError("Failed to find barricade! bot");
                }
                else F.LogError("Failed to find barricade! 2");
            }
        }
        public async Task InvokeUpdate()
        {
            if (barricadetransform != default)
                if (BarricadeManager.tryGetInfo(barricadetransform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    await F.InvokeSignUpdateForAllKits(x, y, plant, index, SignText);
                else F.LogError("Failed to find barricade!");
            else
            {
                BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    if (BarricadeManager.tryGetInfo(drop.model.transform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                        await F.InvokeSignUpdateForAllKits(x, y, plant, index, SignText);
                    else F.LogError("Failed to find barricade! bot");
                }
                else F.LogError("Failed to find barricade! 2");
            }
        }
        /// <summary>Spawns the sign if it is not already placed.</summary>
        public async Task SpawnCheck(bool save)
        {
            BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
            if (data == default)
            {
                this.barricadetransform = BarricadeManager.dropNonPlantedBarricade(
                    new Barricade(sign_id),
                    transform.position.Vector3, transform.Rotation, owner, group
                    );
                if (BarricadeManager.tryGetInfo(this.barricadetransform, out _, out _, out _, out ushort index, out BarricadeRegion region))
                {
                    F.Log("Replaced lost request sign for " + kit_name);
                    if (region != default)
                    {
                        instance_id = region.drops[index].instanceID;
                        exists = true;
                        await InvokeUpdate();
                        if (save) RequestSigns.Save();
                    }
                    else
                    {
                        exists = false;
                    }
                }
                else
                {
                    exists = false;
                }
            }
            else
            {
                exists = true;
                this.barricadetransform = drop.model.transform;
                this.transform = new SerializableTransform(barricadetransform);
                if (save) RequestSigns.Save();
                await InvokeUpdate();
            }
        }
    }
}
