using Newtonsoft.Json;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits
{
    public class RequestSigns : JSONSaver<RequestSign>
    {
        public RequestSigns() : base(Data.StructureStorage + "request_signs.json") { }
        protected override string LoadDefaults() => "[]";
        public static void DropAllSigns()
        {
            foreach (RequestSign sign in ActiveObjects)
            {
                sign.SpawnCheck(false);
                if (!sign.exists)
                    L.LogError("Failed to spawn sign " + sign.kit_name);
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
                    return true;
                }
            }
            found = null;
            return false;
        }
        public static bool SignExists(uint instance_id, out RequestSign found) => ObjectExists(s => s != default && s.instance_id == instance_id, out found);
        public static bool SignExists(string kitName, out RequestSign sign) => ObjectExists(x => x.kit_name == kitName, out sign);
        public static void UpdateSignsWithName(string kitName, Action<RequestSign> action) => UpdateObjectsWhere(rs => rs.kit_name == kitName, action);
        public static void InvokeLangUpdateForSignsOfKit(SteamPlayer player, string kitName)
        {
            if (KitManager.KitExists(kitName, out Kit kitobj))
            {
                if (kitobj.IsLoadout)
                {
                    for (int i = 0; i < ActiveObjects.Count; i++)
                    {
                        if (ActiveObjects[i].kit_name.StartsWith("loadout_"))
                            ActiveObjects[i].InvokeUpdate(player);
                    }
                    return;
                }
            }
            List<RequestSign> s = GetObjectsWhere(x => x.kit_name == kitName);
            for (int i = 0; i < s.Count; i++)
            {
                s[i].InvokeUpdate(player);
            }
        }
        public static void InvokeLangUpdateForSignsOfKit(string kitName)
        {
            if (KitManager.KitExists(kitName, out Kit kitobj))
            {
                if (kitobj.IsLoadout)
                {
                    for (int i = 0; i < ActiveObjects.Count; i++)
                    {
                        if (ActiveObjects[i].kit_name.StartsWith("loadout_"))
                            ActiveObjects[i].InvokeUpdate();
                    }
                    return;
                }
            }
            List<RequestSign> s = GetObjectsWhere(x => x.kit_name == kitName);
            for (int i = 0; i < s.Count; i++)
            {
                s[i].InvokeUpdate();
            }
        }
        public static void InvokeLangUpdateForAllSigns(SteamPlayer player)
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                ActiveObjects[i].InvokeUpdate(player);
            }
        }
        public static void InvokeLangUpdateForAllSigns()
        {
            for (int i = 0; i < ActiveObjects.Count; i++)
            {
                ActiveObjects[i].InvokeUpdate();
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
        public string SignText
        {
            get => "sign_" + kit_name;
            set
            {
                if (value == default) kit_name = TeamManager.DefaultKit;
                else if (value.Length > 5 && value.StartsWith("sign_"))
                    kit_name = value.Substring(5);
                else kit_name = value;
            }
        }
        [JsonSettable]
        public string kit_name;
        public SerializableTransform transform;
        [JsonSettable]
        public Guid sign_id;
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
        public RequestSign(string kit_name, SerializableTransform transform, Guid sign_id, ulong owner, ulong group, uint instance_id)
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
                this.sign_id = drop.GetServersideData().barricade.asset.GUID;
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
            this.sign_id = Guid.Empty;
            this.owner = 0;
            this.group = 0;
            this.instance_id = 0;
            this.barricadetransform = default;
            this.exists = false;
        }
        public void InvokeUpdate(SteamPlayer player)
        {
            if (barricadetransform != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null && drop.model.TryGetComponent(out InteractableSign sign))
                {
                    F.InvokeSignUpdateFor(player, sign, SignText);
                }
                else L.LogError("Failed to find barricade from saved transform!");
            }
            else
            {
                SDG.Unturned.BarricadeData data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                    if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign))
                        F.InvokeSignUpdateFor(player, sign, true, SignText);
                    else L.LogError("Failed to find barricade after respawning again!");
                }
                else L.LogError("Failed to find barricade after respawn!");
            }
        }
        public void InvokeUpdate()
        {
            if (barricadetransform != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null && drop.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                {
                    F.InvokeSignUpdateForAllKits(sign, x, y, SignText);
                }
                else L.LogError("Failed to find barricade from saved transform!");
            }
            else
            {
                SDG.Unturned.BarricadeData data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
                if (data != null && drop != null)
                {
                    BarricadeDrop drop2 = BarricadeManager.FindBarricadeByRootTransform(drop.model.transform);
                    if (drop2 != null && drop2.model.TryGetComponent(out InteractableSign sign) && Regions.tryGetCoordinate(drop.model.position, out byte x, out byte y))
                        F.InvokeSignUpdateForAllKits(sign, x, y, SignText);
                    else L.LogError("Failed to find barricade after respawning again!");
                }
                else L.LogError("Failed to find barricade after respawn!");
            }
        }
        /// <summary>Spawns the sign if it is not already placed.</summary>
        public void SpawnCheck(bool save)
        {
            SDG.Unturned.BarricadeData data = UCBarricadeManager.GetBarricadeFromInstID(instance_id, out BarricadeDrop drop);
            if (drop == null || data == null)
            {
                if (!(Assets.find(sign_id) is ItemBarricadeAsset asset))
                {
                    L.LogError("Failed to find barricade with " + sign_id.ToString("N"));
                    return;
                }
                this.barricadetransform = BarricadeManager.dropNonPlantedBarricade(
                    new Barricade(asset),
                    transform.position.Vector3, transform.Rotation, owner, group
                    );
                drop = BarricadeManager.FindBarricadeByRootTransform(barricadetransform);
                if (drop != null)
                {
                    L.Log("Replaced lost request sign for " + kit_name, ConsoleColor.Black);
                    instance_id = drop.instanceID;
                    exists = true;
                    InvokeUpdate();
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
                InvokeUpdate();
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
