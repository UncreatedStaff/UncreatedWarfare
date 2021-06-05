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
        public static List<RequestSign> ActiveSigns;
        public RequestSigns() : base(Data.StructureStorage + "request_signs.json") 
        {
            ActiveSigns = GetExistingObjects();
        }
        protected override string LoadDefaults() => "[]";
        public static void DropAllSigns()
        {
            foreach(RequestSign sign in ActiveSigns)
            {
                Transform barricade = BarricadeManager.dropNonPlantedBarricade(new Barricade(sign.sign_id), sign.transform.Position, sign.transform.Rotation, sign.owner, sign.group);
                if (barricade == default)
                    F.LogError($"Failed to spawn sign of id {sign.sign_id}: \"{sign.kit_name}\" at ({Math.Round(sign.transform.position.x, 2)}, " +
                        $"{Math.Round(sign.transform.position.y, 2)}, {Math.Round(sign.transform.position.z, 2)}.");
                else
                {
                    if(barricade.TryGetComponent(out InteractableSign signobj))
                    {
                        signobj.updateText(sign.SignText);
                        sign.barricadetransform = barricade;
                        if (BarricadeManager.tryGetInfo(barricade, out byte x, out byte y, out ushort plant, out ushort index, out _))
                            F.InvokeSignUpdateForAllKits(x, y, plant, index, sign.SignText);
                    } else
                    {
                        F.LogError(sign.kit_name + " is not using a valid sign's id.");
                    }
                }
            }
        }
        public static bool AddRequestSign(InteractableSign sign, out RequestSign signadded)
        {
            signadded = default;
            if(!ObjectExists(x => x.transform.Position == sign.transform.position && x.transform.Rotation == sign.transform.rotation, out _))
            {
                signadded = new RequestSign(sign);
                ActiveSigns.Add(signadded);
                AddObjectToSave(signadded);
                return true;
            }
            return false;
        }
        public static void RemoveRequestSign(RequestSign sign)
        {
            int i = ActiveSigns.FindIndex(x => x.transform == sign.transform);
            if (i != -1) ActiveSigns.RemoveAt(i);
            RemoveFromSaveWhere(x => x.transform == sign.transform);
            sign.InvokeUpdate();
        }
        public static bool SignExists(InteractableSign sign, out RequestSign found, bool secondTime = false)
        {
            if(sign == default)
            {
                found = default;
                return false;
            }
            IEnumerable<RequestSign> matches = ActiveSigns.Where(s => s.transform == sign.transform);
            int amt = matches.Count();
            if (amt >= 1)
            {
                found = matches.ElementAt(0);
                return found != default;
            } else if (!secondTime)
            {
                ActiveSigns = GetExistingObjects();
                return SignExists(sign, out found, true);
            } else 
            {
                found = default;
                return false;
            }
        }
        public static bool SignExists(string kitName, out List<RequestSign> signs)
        {
            signs = ActiveSigns.Where(x => x.kit_name == kitName).ToList();
            return signs.Count > 0;
        } 
        public static void UpdateSignsWithName(SteamPlayer player, string kitName)
        {
            if (SignExists(kitName, out List<RequestSign> signs))
            {
                foreach (RequestSign sign in signs)
                    sign.InvokeUpdate(player);
            }
        }
        public static void UpdateSignsWithName(string kitName)
        {
            if (SignExists(kitName, out List<RequestSign> signs))
            {
                foreach (RequestSign sign in signs)
                    sign.InvokeUpdate();
            }
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
        public string kit_name;
        public SerializableTransform transform;
        public ushort sign_id;
        public ulong owner;
        public ulong group;
        [JsonIgnore]
        public Transform barricadetransform = default;

        [JsonConstructor]
        public RequestSign(string kit_name, SerializableTransform transform, ushort sign_id, ulong owner, ulong group)
        {
            this.kit_name = kit_name;
            this.transform = transform;
            this.sign_id = sign_id;
            this.owner = owner;
            this.group = group;
        }
        public RequestSign(InteractableSign sign)
        {
            if (sign == default) throw new ArgumentNullException("sign");
            this.transform = new SerializableTransform(sign.transform);
            this.SignText = sign.text;
            this.group = sign.group.m_SteamID;
            this.owner = sign.owner.m_SteamID;
            if (BarricadeManager.tryGetInfo(sign.transform, out _, out _, out ushort plant, out ushort index, out BarricadeRegion region))
            {
                this.sign_id = region.barricades[index].barricade.id;
            } else if (ushort.TryParse(sign.name, out ushort id))
            {
                sign_id = id;
            }
            else sign_id = 0;
            this.barricadetransform = sign.transform;
        }
        public void InvokeUpdate(SteamPlayer player)
        {
            if (barricadetransform != default)
                if (BarricadeManager.tryGetInfo(barricadetransform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    F.InvokeSignUpdateFor(player, x, y, plant, index, SignText);
        }
        public void InvokeUpdate()
        {
            if (barricadetransform != default)
                if (BarricadeManager.tryGetInfo(barricadetransform, out byte x, out byte y, out ushort plant, out ushort index, out _))
                    F.InvokeSignUpdateForAllKits(x, y, plant, index, SignText);
        }
    }
}
