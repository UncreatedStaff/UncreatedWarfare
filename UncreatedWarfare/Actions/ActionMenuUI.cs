using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Actions;

public class ActionMenuUI : UnturnedUI
{
    public readonly UnturnedButton NeedMedic = new UnturnedButton("AC_NeedMedic");
    public readonly UnturnedButton NeedAmmo = new UnturnedButton("AC_NeedAmmo");
    public readonly UnturnedButton NeedRide = new UnturnedButton("AC_NeedRide");
    public readonly UnturnedButton NeedSupport = new UnturnedButton("AC_NeedSupport");
    public readonly UnturnedButton HeliPickup = new UnturnedButton("AC_HeliPickup");
    public readonly UnturnedButton HeliDropoff = new UnturnedButton("AC_HeliDropoff");
    public readonly UnturnedButton SuppliesBuild = new UnturnedButton("AC_SuppliesBuild");
    public readonly UnturnedButton SuppliesAmmo = new UnturnedButton("AC_SuppliesAmmo");
    public readonly UnturnedButton AirSupport = new UnturnedButton("AC_AirSupport");
    public readonly UnturnedButton ArmorSupport = new UnturnedButton("AC_ArmorSupport");

    public readonly UnturnedUIElement SquadSection = new UnturnedUIElement("AC_SquadLeader");
    public readonly UnturnedUIElement LogiSection = new UnturnedUIElement("AC_Logi");

    public readonly UnturnedButton Cancel = new UnturnedButton("AC_Cancel");

    public ActionMenuUI() : base(16051, Gamemode.Config.UI.ActionMenuGUID)
    {

    }   
}
