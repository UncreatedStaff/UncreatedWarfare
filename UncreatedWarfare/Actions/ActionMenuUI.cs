using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Actions;

[UnturnedUI(BasePath = "Canvas")]
public class ActionMenuUI : UnturnedUI
{
    public readonly UnturnedButton NeedMedic       = new UnturnedButton("AC_DefaultMenu/AC_NeedMedic");
    public readonly UnturnedButton NeedAmmo        = new UnturnedButton("AC_DefaultMenu/AC_NeedAmmo");
    public readonly UnturnedButton NeedRide        = new UnturnedButton("AC_DefaultMenu/AC_NeedRide");
    public readonly UnturnedButton NeedSupport     = new UnturnedButton("AC_DefaultMenu/AC_NeedSupport");
    public readonly UnturnedButton ThankYou        = new UnturnedButton("AC_DefaultMenu/AC_ThankYou");
    public readonly UnturnedButton Sorry           = new UnturnedButton("AC_DefaultMenu/AC_Sorry");
    public readonly UnturnedButton HeliPickup      = new UnturnedButton("AC_RequestMenu/AC_HeliPickup");
    public readonly UnturnedButton HeliDropoff     = new UnturnedButton("AC_RequestMenu/AC_HeliDropoff");
    public readonly UnturnedButton SuppliesBuild   = new UnturnedButton("AC_RequestMenu/AC_SuppliesBuild");
    public readonly UnturnedButton SuppliesAmmo    = new UnturnedButton("AC_RequestMenu/AC_SuppliesAmmo");
    public readonly UnturnedButton AirSupport      = new UnturnedButton("AC_RequestMenu/AC_AirSupport");
    public readonly UnturnedButton ArmorSupport    = new UnturnedButton("AC_RequestMenu/AC_ArmorSupport");

    public readonly UnturnedButton Attack          = new UnturnedButton("AC_OrderMenu/AC_Attack");
    public readonly UnturnedButton Defend          = new UnturnedButton("AC_OrderMenu/AC_Defend");
    public readonly UnturnedButton Move            = new UnturnedButton("AC_OrderMenu/AC_Move");
    public readonly UnturnedButton Build           = new UnturnedButton("AC_OrderMenu/AC_Build");

    public readonly UnturnedButton AttackMarker    = new UnturnedButton("AC_OrderMenu/AC_AttackMarker");
    public readonly UnturnedButton DefendMarker    = new UnturnedButton("AC_OrderMenu/AC_DefendMarker");
    public readonly UnturnedButton MoveMarker      = new UnturnedButton("AC_OrderMenu/AC_MoveMarker");
    public readonly UnturnedButton BuildMarker     = new UnturnedButton("AC_OrderMenu/AC_BuildMarker");

    public readonly UnturnedButton LoadBuild       = new UnturnedButton("AC_DefaultMenu/AC_Logi/AC_LoadBuild10");
    public readonly UnturnedButton LoadAmmo        = new UnturnedButton("AC_DefaultMenu/AC_Logi/AC_LoadAmmo10");
    public readonly UnturnedButton UnloadBuild     = new UnturnedButton("AC_DefaultMenu/AC_Logi/AC_UnloadBuild10");
    public readonly UnturnedButton UnloadAmmo      = new UnturnedButton("AC_DefaultMenu/AC_Logi/AC_UnloadAmmo10");

    public readonly UnturnedButton Cancel          = new UnturnedButton("AC_Cancel");

    public readonly UnturnedUIElement SquadSection = new UnturnedUIElement("AC_DefaultMenu/AC_SquadLeader");
    public readonly UnturnedUIElement LogiSection  = new UnturnedUIElement("AC_DefaultMenu/AC_Logi");

    public ActionMenuUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:ActionMenu")) { }
}