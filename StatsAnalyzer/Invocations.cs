using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Stats;
using Windows.UI.Xaml;

namespace StatsAnalyzer
{
    internal static class Invocations
    {
        internal static class WarfareStatsAnalyzer
        {




            internal static readonly NetCallRaw<WarfareTeam> SendTeamData =
                new NetCallRaw<WarfareTeam>(2005, WarfareTeam.Read, WarfareTeam.Write);
            [NetCall(ENetCall.FROM_SERVER, 2005)]
            internal static void ReceiveTeamData(in IConnection connection, WarfareTeam team)
            {

            }


            internal static readonly NetCallRaw<WarfareWeapon, string> SendWeaponData =
                new NetCallRaw<WarfareWeapon, string>(2007, WarfareWeapon.Read, R => R.ReadString(), WarfareWeapon.Write, (W, S) => W.Write(S));

            

            internal static readonly NetCallRaw<WarfareVehicle, string> SendVehicleData =
                new NetCallRaw<WarfareVehicle, string>(2009, WarfareVehicle.Read, R => R.ReadString(), WarfareVehicle.Write, (W, S) => W.Write(S));

        }
    }
    public enum EClass : byte
    {
        NONE, //0 
        UNARMED, //1
        SQUADLEADER, //2
        RIFLEMAN, //3
        MEDIC, //4
        BREACHER, //5
        AUTOMATIC_RIFLEMAN, //6
        GRENADIER, //7
        MACHINE_GUNNER, //8
        LAT, //9
        HAT, //10
        MARKSMAN, //11
        SNIPER, //12
        AP_RIFLEMAN, //13
        COMBAT_ENGINEER, //14
        CREWMAN, //15
        PILOT, //16
        SPEC_OPS // 17
    }
}
