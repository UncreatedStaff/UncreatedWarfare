using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Chat
    {
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, Color textColor, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, Color textColor, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, Color textColor, params string[] formatting) =>
            SendChat(player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, params string[] formatting) =>
            SendChat(player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, Color textColor, params string[] formatting)
        {
            string localizedString = Translation.Translate(text, player.playerID.steamID.m_SteamID, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
                SendSingleMessage(localizedString, textColor, EChatMode.SAY, null, localizedString.Contains("</"), player);
            else
            {
                L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
                string defaultMessage = text;
                string newMessage;
                if (JSONMethods.DefaultTranslations.ContainsKey(text))
                    defaultMessage = JSONMethods.DefaultTranslations[text];
                try
                {
                    newMessage = string.Format(defaultMessage, formatting);
                }
                catch (FormatException)
                {
                    newMessage = defaultMessage + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
                if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                    SendSingleMessage(newMessage, textColor, EChatMode.SAY, null, newMessage.Contains("</"), player);
                else
                    L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MAX_CHAT_MESSAGE_SIZE.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, params string[] formatting)
        {
            string localizedString = Translation.Translate(text, player.playerID.steamID.m_SteamID, out Color textColor, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
                SendSingleMessage(localizedString, textColor, EChatMode.SAY, null, localizedString.Contains("</"), player);
            else
            {
                L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
                string defaultMessage = text;
                string newMessage;
                if (JSONMethods.DefaultTranslations.ContainsKey(text))
                    defaultMessage = JSONMethods.DefaultTranslations[text];
                try
                {
                    newMessage = string.Format(defaultMessage, formatting);
                }
                catch (FormatException)
                {
                    newMessage = defaultMessage + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
                if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                    SendSingleMessage(newMessage, textColor, EChatMode.SAY, null, newMessage.Contains("</"), player);
                else
                    L.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MAX_CHAT_MESSAGE_SIZE.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Max amount of bytes that can be sent in an Unturned Chat Message.
        /// </summary>
        const int MAX_CHAT_MESSAGE_SIZE = 2047;
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, Color textColor, params string[] formatting)
        {
            SteamPlayer sp = PlayerTool.getSteamPlayer(player);
            if (sp != null)
                sp.SendChat(text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat using the translation file, automatically extrapolates the color.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, params string[] formatting)
        {
            SteamPlayer sp = PlayerTool.getSteamPlayer(player);
            if (sp != null)
                sp.SendChat(text, formatting);
        }
        /// <summary>
        /// Send a white message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this UnturnedPlayer player, string message, params string[] formatting) =>
            SendChat(player.Player.channel.owner, message, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this Player player, string message, params string[] formatting) =>
            SendChat(player.channel.owner, message, formatting);
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, Color textColor, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients)
                SendChat(player, text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para>
        /// <para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients)
                SendChat(player, text, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, Color textColor, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                SendChat(player, text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                SendChat(player, text, formatting);
        }

        public static void SendSingleMessage(string text, Color color, EChatMode mode, string iconURL, bool richText, SteamPlayer recipient)
        {
            try
            {
                ThreadUtil.assertIsGameThread();
            }
            catch
            {
                L.LogWarning("Tried to send a chat message on non-game thread.");
                return;
            }
            if (Data.SendChatIndividual == null)
            {
                ChatManager.serverSendMessage(text, color, null, recipient, mode, iconURL, richText);
                return;
            }
            if (!Provider.isServer)
            {
                L.LogWarning("Tried to send a chat message from client.");
                return;
            }
            if (recipient == null)
            {
                L.LogWarning("Tried to send a chat message to null recipient.");
                return;
            }
            try
            {
                try
                {
                    ChatManager.onServerSendingMessage?.Invoke(ref text, ref color, null, recipient, mode, ref iconURL, ref richText);
                }
                catch (Exception ex)
                {
                    L.LogError("Error invoking ChatManager.onServerSendingMessage event: ");
                    L.LogError(ex);
                }
                if (iconURL == null) iconURL = string.Empty;
                Data.SendChatIndividual.Invoke(ENetReliability.Reliable, recipient.transportConnection, CSteamID.Nil, iconURL, mode, color, richText, text);
            }
            catch
            {
                ChatManager.serverSendMessage(text, color, null, recipient, mode, iconURL, richText);
            }
        }/// <param name="backupcause">Used in case the key can not be found.</param>
        public static void BroadcastDeath(string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, bool translateKillerName, ulong killerTeam, ELimb limb, string itemName, float distance, out string message, bool broadcast = true)
        {
            if (broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string killer = translateKillerName ? Translation.Translate(killerName.CharacterName, player) : killerName.CharacterName;
                    string localizedString = Translation.TranslateDeath(player.playerID.steamID.m_SteamID, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, false, translateKillerName);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString,
                            UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 ? "death_background_teamkill" : "death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string newMessage;
                        if (!JSONMethods.DefaultDeathTranslations.TryGetValue(key, out string defaultMessage) && !JSONMethods.DefaultDeathTranslations.TryGetValue(backupcause.ToString(), out defaultMessage))
                            defaultMessage = key;
                        try
                        {
                            newMessage = string.Format(defaultMessage, F.ColorizeName(dead.CharacterName, deadTeam), F.ColorizeName(killer, killerTeam),
                                Translation.TranslateLimb(player.playerID.steamID.m_SteamID, limb), itemName, Math.Round(distance).ToString(Data.Locale));
                        }
                        catch (FormatException)
                        {
                            newMessage = key + $" ({F.ColorizeName(dead.CharacterName, deadTeam)}, {F.ColorizeName(killer, killerTeam)}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + key + "\"");
                        }
                        if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                            ChatManager.say(player.playerID.steamID, newMessage,
                                UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 && dead.Steam64 != killerName.Steam64 ? "death_background_teamkill" : "death_background"), newMessage.Contains("</"));
                        else
                            L.LogError("There's been an error sending a chat message. Default message for \"" + key + "\" is longer than "
                                + MAX_CHAT_MESSAGE_SIZE.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
                    }
                }
            }
            message = Translation.TranslateDeath(0, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, true, translateKillerName, false);
        }
        public static void BroadcastLandmineDeath(string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, out string message, bool broadcast = true)
        {
            if (broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string localizedString = Translation.TranslateLandmineDeath(player.playerID.steamID.m_SteamID, key, dead, deadTeam, killerName, killerTeam, triggererName, triggererTeam, limb, landmineName, false);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MAX_CHAT_MESSAGE_SIZE)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString,
                            UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 ? "death_background_teamkill" : "death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        L.LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string newMessage;
                        if (!JSONMethods.DefaultDeathTranslations.TryGetValue(key, out string defaultMessage))
                            defaultMessage = key;
                        try
                        {
                            newMessage = string.Format(defaultMessage, F.ColorizeName(dead.CharacterName, deadTeam), F.ColorizeName(killerName.CharacterName, killerTeam),
                                Translation.TranslateLimb(player.playerID.steamID.m_SteamID, limb), landmineName, "0", F.ColorizeName(triggererName.CharacterName, triggererTeam));
                        }
                        catch (FormatException)
                        {
                            newMessage = key + $" ({F.ColorizeName(dead.CharacterName, deadTeam)}, {F.ColorizeName(killerName.CharacterName, killerTeam)}, {limb}, {landmineName}, {triggererName.CharacterName}";
                            L.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + key + "\"");
                        }
                        if (Encoding.UTF8.GetByteCount(newMessage) <= MAX_CHAT_MESSAGE_SIZE)
                            ChatManager.say(player.playerID.steamID, newMessage,
                                UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 && dead.Steam64 != killerName.Steam64 ? "death_background_teamkill" : "death_background"), newMessage.Contains("</"));
                        else
                            L.LogError("There's been an error sending a chat message. Default message for \"" + key + "\" is longer than "
                                + MAX_CHAT_MESSAGE_SIZE.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
                    }
                }
            }
            message = Translation.TranslateLandmineDeath(0, key, dead, deadTeam, killerName, killerTeam, triggererName, triggererTeam, limb, landmineName, true, false);
        }
    }
}
