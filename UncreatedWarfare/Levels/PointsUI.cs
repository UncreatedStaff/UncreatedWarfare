using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Levels;

[UnturnedUI(BasePath = "Canvas/Image")]
public class XPUI : UnturnedUI
{
    public readonly UnturnedUIElement Parent = new UnturnedUIElement("~/Canvas/Image");
    public readonly UnturnedLabel Rank = new UnturnedLabel("Image/Rank");
    public readonly UnturnedLabel XP = new UnturnedLabel("Image/XP");
    public readonly UnturnedLabel Next = new UnturnedLabel("Image/Next");
    public readonly UnturnedLabel Progress = new UnturnedLabel("Image/Progress");

    public XPUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:XP"), reliable: false) { }
//    public void SendTo(WarfarePlayer player)
//    {
//        GameThread.AssertCurrent();

//        ITransportConnection c = player.Connection;
//        L.LogDebug("Sending xp ui to " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");

//        LevelData data = player.Level;
//        SendToPlayer(c,
//            data.Abbreviation,
//            data.CurrentXP.ToString(player.Locale.CultureInfo) + "/" + data.RequiredXP.ToString(player.Locale.CultureInfo),
//            data.NextAbbreviation,
//            data.ProgressBar);
//        if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
//        {
//            Parent.SetVisibility(c, false);
//            player.PointsDirtyMask |= 0b00100111;
//            return;
//        }

//        player.PointsDirtyMask &= unchecked((byte)~0b10000111);
//    }

//    public void Update(UCPlayer player, bool full)
//    {
//        L.LogDebug("Updating xp ui for " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
//        GameThread.AssertCurrent();
//        if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
//            return;
//        if ((player.PointsDirtyMask & 0b10000000) == 0)
//        {
//            SendTo(player);
//            return;
//        }
//        ITransportConnection c = player.Connection;
//        LevelData data = player.Level;
//        if (full || (player.PointsDirtyMask & 0b00000001) > 0)
//        {
//            XP.SetText(c, data.CurrentXP.ToString(player.Locale.CultureInfo) + "/" + data.RequiredXP.ToString(player.Locale.CultureInfo));
//        }
//        if (full || (player.PointsDirtyMask & 0b00000010) > 0)
//        {
//            Rank.SetText(c, data.Abbreviation);
//            Next.SetText(c, data.NextAbbreviation);
//        }
//        player.PointsDirtyMask &= unchecked((byte)~0b00100111);
//    }

//    public void Clear(UCPlayer player)
//    {
//        L.LogDebug("Clearing xp ui for " + player);
//        player.PointsDirtyMask |= 0b10100000;
//        Parent.SetVisibility(player.Connection, false);
//    }
}
public class CreditsUI : UnturnedUI
{
    public readonly UnturnedLabel Credits = new UnturnedLabel("Canvas/Image/Credits");
    public readonly UnturnedUIElement Parent = new UnturnedUIElement("Canvas/Image");
    private static string? _creditsColor;
    public string CreditsColor => _creditsColor ??= "ffffff"; // todo use credit color
    public CreditsUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Credits")) { }
    //public void SendTo(UCPlayer player)
    //{
    //    L.LogDebug("Sending creds ui to " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
    //    GameThread.AssertCurrent();
    //    SendToPlayer(player.Connection, GetCreditsString(player));
    //    player.PointsDirtyMask &= unchecked((byte)~0b01011000);
    //    if (player.HasUIHidden || Data.Gamemode.LeaderboardUp())
    //    {
    //        Parent.SetVisibility(player.Connection, false);
    //        player.PointsDirtyMask |= 0b00010000;
    //    }
    //}

    //public void Update(UCPlayer player, bool full)
    //{
    //    L.LogDebug("Updating creds ui for " + player + " (" + Convert.ToString(player.PointsDirtyMask, 2) + ")");
    //    GameThread.AssertCurrent();
    //    if ((!full && (player.PointsDirtyMask & 0b00001000) == 0) || player.HasUIHidden || Data.Gamemode.LeaderboardUp())
    //        return;
    //    if ((player.PointsDirtyMask & 0b10000000) == 0)
    //    {
    //        SendTo(player);
    //        return;
    //    }
    //    if ((player.PointsDirtyMask & 0b00010000) > 0)
    //    {
    //        Parent.SetVisibility(player.Connection, true);
    //    }
    //    Credits.SetText(player.Connection, GetCreditsString(player));
    //    player.PointsDirtyMask &= unchecked((byte)~0b00011000);
    //}

    //public void Clear(UCPlayer player)
    //{
    //    L.LogDebug("Clearing creds ui for " + player);
    //    player.PointsDirtyMask |= 0b01010000;
    //    Parent.SetVisibility(player.Connection, false);
    //}

    //private string GetCreditsString(UCPlayer player)
    //{
    //    return "<color=#" + CreditsColor + ">C</color>  " + player.CachedCredits.ToString(player.Locale.CultureInfo);
    //}
}