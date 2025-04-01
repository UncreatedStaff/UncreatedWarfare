using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.UI;
public static class ToastPlayerExtensions
{
    /// <summary>
    /// Send a toast to a set of players.
    /// </summary>
    public static void SendToasts(this LanguageSet set, ToastMessage message)
    {
        while (set.MoveNext())
        {
            set.Next.Component<ToastManager>().Queue(in message);
        }
    }

    /// <summary>
    /// Send a toast to a player.
    /// </summary>
    public static void SendToast(this WarfarePlayer player, ToastMessage message)
    {
        player.Component<ToastManager>().Queue(in message);
    }
}