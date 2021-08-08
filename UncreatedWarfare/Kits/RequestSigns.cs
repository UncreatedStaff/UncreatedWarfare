using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
            BarricadeDrop drop = UCBarricadeManager.GetSignFromInteractable(sign);
            if (drop != null && !ObjectExists(x => x.instance_id == drop.instanceID, out signadded))
            {
                signadded = new RequestSign(sign);
                AddObjectToSave(signadded);
                return true;
            }
            return false;
        }
        public static void RemoveRequestSign(RequestSign sign)
        {
            RemoveWhere(x => x.instance_id == sign.instance_id);
            //await sign.InvokeUpdate();
        }
        public static void RemoveRequestSigns(string kitname) => RemoveWhere(x => x.kit_name == kitname);
        public static bool SignExists(InteractableSign sign, out RequestSign found)
        {
            if (sign == null)
            {
                found = null;
                return false;
            }
            BarricadeDrop drop = UCBarricadeManager.GetSignFromInteractable(sign);
            if (drop == null)
            {
                found = null;
                return false;
            }
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                if (drop != null && drop.instanceID == ActiveObjects[i].instance_id)
                {
                    found = ActiveObjects[i];
                    F.Log(found.instance_id.ToString());
                    return true;
                }
            }
            found = null;
            return false;
        }
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
        public static async Task InvokeLangUpdateForAllSigns(SteamPlayer player)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                await ActiveObjects[i].InvokeUpdate(player);
            }
        }
        public static async Task InvokeLangUpdateForAllSigns()
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                await ActiveObjects[i].InvokeUpdate();
            }
        }
        public static void SetSignTextSneaky(InteractableSign sign, string text)
        {
            BarricadeDrop barricadeByRootFast = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
            byte[] state = barricadeByRootFast.GetServersideData().barricade.state;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] numArray1 = new byte[17 + bytes.Length];
            byte[] numArray2 = numArray1;
            Buffer.BlockCopy(state, 0, numArray2, 0, 16);
            numArray1[16] = (byte)bytes.Length;
            if (bytes.Length != 0)
                Buffer.BlockCopy(bytes, 0, numArray1, 17, bytes.Length);
            barricadeByRootFast.GetServersideData().barricade.state = numArray1;
        }
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
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(sign.transform);
            if (drop != null)
            {
                this.sign_id = drop.GetServersideData().barricade.id;
                this.instance_id = drop.instanceID;
                this.transform = new SerializableTransform(sign.transform);
                this.barricadetransform = sign.transform;
                this.SignText = sign.text;
                this.group = sign.group.m_SteamID;
                this.owner = sign.owner.m_SteamID;
            }
            else throw new ArgumentNullException(nameof(sign));
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
            if (barricadetransform != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null && drop.model.TryGetComponent(out InteractableSign sign))
                {
                    await F.InvokeSignUpdateFor(player, sign, SignText);
                }
                else F.LogError("Failed to find barricade from saved transform!");
            }
            else
            {
                BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                    if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign))
                        await F.InvokeSignUpdateFor(player, sign, true, SignText);
                    else F.LogError("Failed to find barricade after respawning again!");
                }
                else F.LogError("Failed to find barricade after respawn!");
            }
        }
        public async Task InvokeUpdate()
        {
            if (barricadetransform != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null && drop.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                {
                    await F.InvokeSignUpdateForAllKits(sign, x, y, SignText);
                }
                else F.LogError("Failed to find barricade from saved transform!");
            }
            else
            {
                BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                    if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                        await F.InvokeSignUpdateForAllKits(sign, x, y, SignText);
                    else F.LogError("Failed to find barricade after respawning again!");
                }
                else F.LogError("Failed to find barricade after respawn!");
            }
        }
        /// <summary>Spawns the sign if it is not already placed.</summary>
        public async Task SpawnCheck(bool save)
        {
            BarricadeData data = F.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
            if (drop == null || data == null)
            {
                this.barricadetransform = BarricadeManager.dropNonPlantedBarricade(
                    new Barricade(sign_id),
                    transform.position.Vector3, transform.Rotation, owner, group
                    );
                drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null)
                {
                    F.Log("Replaced lost request sign for " + kit_name, ConsoleColor.Black);
                    instance_id = drop.instanceID;
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
                exists = true;
                this.barricadetransform = drop.model.transform;
                this.transform = new SerializableTransform(barricadetransform);
                if (save) RequestSigns.Save();
                await InvokeUpdate();
            }
            if (exists && barricadetransform != null && barricadetransform.TryGetComponent(out InteractableSign sign))
            {
                if (sign.text != SignText)
                {
                    RequestSigns.SetSignTextSneaky(sign, SignText);
                    sign.updateText(SignText);
                }
            }
        }
    }
}
