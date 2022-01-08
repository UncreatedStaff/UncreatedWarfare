using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Players;

namespace Uncreated.Warfare
{
    public static class OffenseManager
    {
        private const int HWIDS_COLUMN_SIZE = 161;
        public static async Task BanPlayer(ulong offender, ulong banner, int duration, string reason)
        {
            if (duration == 0)
            {
                UCPlayer admin2 = UCPlayer.FromID(banner);
                if (admin2 == null)
                    L.Log(Translation.Translate("ban_invalid_number_console", 0, "0"));
                else
                    admin2.SendChat("ban_invalid_number", "0");
                return;
            }
            UCPlayer admin = UCPlayer.FromID(banner);

            if (!IsValidSteam64ID(offender))
            {
                if (admin == null)
                    L.Log(Translation.Translate("ban_no_player_found_console", 0, offender.ToString(Data.Locale)));
                else
                    admin.SendChat("ban_no_player_found", offender.ToString(Data.Locale));
                return;
            }

            UCPlayer bannedPlayer = UCPlayer.FromID(offender);
            
            if (bannedPlayer == null)
            {
                uint ip = 0;
                List<byte[][]> hwidses = new List<byte[][]>(8);
                byte[] buffer = new byte[161];
                await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `ban_offenses` WHERE `Violator` = @0;",
                    new object[1] { offender },
                    (R) =>
                    {
                        ip = R.GetUInt32(0);
                        long i = R.GetBytes(1, 0L, buffer, 0, HWIDS_COLUMN_SIZE);
                        if (i != 0)
                        {
                            i = buffer[0];
                            byte[][] hwids = new byte[i][];
                            for (; i >= 0L; i--)
                            {
                                hwids[i] = new byte[20];
                                Buffer.BlockCopy(buffer, 1 + (int)i * 20, hwids[i], 0, 20);
                            }
                            hwidses.Add(hwids);
                        }
                    });
                if (ip == 0)
                {
                    await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `logindata` WHERE `Steam64` = @0 LIMIT 1;",
                        new object[1] { offender },
                        (R) =>
                        {
                            ip = R.GetUInt32(0);
                            long i = R.GetBytes(1, 0L, buffer, 0, HWIDS_COLUMN_SIZE);
                            if (i != 0)
                            {
                                i = buffer[0];
                                byte[][] hwids = new byte[i][];
                                for (; i >= 0L; i--)
                                {
                                    hwids[i] = new byte[20];
                                    Buffer.BlockCopy(buffer, 1 + (int)i * 20, hwids[i], 0, 20);
                                }
                                hwidses.Add(hwids);
                            }
                        });
                }
                if (hwidses.Count == 0) hwidses.Add(new byte[0][]);
                await UCWarfare.ToUpdate();
                
                Provider.requestBanPlayer(new CSteamID(banner), new CSteamID(offender), ip, reason, duration < 0 ? uint.MaxValue : (uint)duration * 60u);

                await UCWarfare.ToPool();
                await Data.DatabaseManager.NonQueryAsync(
                    "INSERT INTO `ban_offenses` (`Violator`, `Admin`, `Reason`, `Duration`, `HWIDs`, `IP`, `Timestamp`) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                    new object[7]
                    {
                        offender, banner, reason, duration, hwidses[0], ip, DateTime.Now
                    });
                return; // on main thread
            }
            else
            {
                List<uint> ips = new List<uint>(2);
                uint ip = bannedPlayer.Player.channel.owner.getIPv4AddressOrZero();
                if (ip == 0)
                {
                    await Data.DatabaseManager.QueryAsync("SELECT `IP` FROM `ban_offenses` WHERE `Violator` = @0 LIMIT 1;",
                        new object[1] { offender },
                        (R) =>
                        {
                            ip = R.GetUInt32(0);
                        });
                    if (ip == 0)
                    {
                        await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `logindata` WHERE `Steam64` = @0 LIMIT 1;",
                            new object[1] { offender },
                            (R) =>
                            {
                                ip = R.GetUInt32(0);
                            });
                    }
                }
                await UCWarfare.ToUpdate();

                Provider.requestBanPlayer(new CSteamID(banner), new CSteamID(offender), ip, reason, duration < 0 ? uint.MaxValue : (uint)duration * 60u);

                await UCWarfare.ToPool();
                byte[][] hwids = (byte[][])bannedPlayer.Player.channel.owner.playerID.GetHwids();
                byte[] inhwids = new byte[HWIDS_COLUMN_SIZE];
                inhwids[0] = (byte)hwids.Length;
                for (int i = 0; i < hwids.Length; i++)
                    Buffer.BlockCopy(hwids[i], 0, inhwids, 1 + i * 20, 20);
                await Data.DatabaseManager.NonQueryAsync(
                    "INSERT INTO `ban_offenses` (`Violator`, `Admin`, `Reason`, `Duration`, `HWIDs`, `IP`, `Timestamp`) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                    new object[7]
                    {
                        offender, banner, reason, duration, inhwids, ip, DateTime.Now
                    });
            }

            await UCWarfare.ToPool();
            string timeLocalized = duration < 0 ? "a long time" : ((uint)duration).GetTimeFromMinutes(0);
            FPlayerName adminNames = await F.GetPlayerOriginalNamesAsync(banner);
            FPlayerName violatorNames = banner == 0 ? FPlayerName.Nil : await F.GetPlayerOriginalNamesAsync(offender);
            await UCWarfare.ToUpdate();
            string translation = "ban_console";
            if (banner == 0) translation += "_operator";
            L.Log(Translation.Translate(translation, 0, out _, violatorNames.PlayerName, offender.ToString(Data.Locale),
                adminNames.PlayerName, banner.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
            if (admin != null)
                admin.SendChat("ban_feedback", violatorNames.CharacterName, timeLocalized);
            translation = "ban_";
            if (duration < 0) translation += "permanent_";
            translation += "broadcast";
            if (banner == 0) translation += "_operator";
            foreach (LanguageSet set in Translation.EnumerateLanguageSets(x => x.Steam64 != banner))
            {
                if (duration < 0)
                {
                    while (set.MoveNext())
                    {
                        set.Next.SendChat(translation, violatorNames.CharacterName, adminNames.CharacterName);
                    }
                }
                else
                {
                    timeLocalized = duration < 0 ? "a long time" : ((uint)duration).GetTimeFromMinutes(set.Language);
                    while (set.MoveNext())
                    {
                        set.Next.SendChat(translation, violatorNames.CharacterName, adminNames.CharacterName, timeLocalized);
                    }
                }
            }
        }

        public static async Task KickPlayer(ulong offender, ulong kicker, string reason)
        {
            UCPlayer admin = UCPlayer.FromID(kicker);

            if (!IsValidSteam64ID(offender))
                goto NoPlayer;
            UCPlayer bannedPlayer = UCPlayer.FromID(offender);
            if (bannedPlayer == null)
                goto NoPlayer;


            await Data.DatabaseManager.NonQueryAsync(
                "INSERT INTO `kicks` (`Kicked`, `Kicker`, `Reason`, `Tiemstamp`) VALUES (@0, @1, @2, @3);",
                new object[4]
                {
                    offender, kicker, reason, DateTime.Now
                });
            FPlayerName adminNames = kicker == 0 ? FPlayerName.Nil : await F.GetPlayerOriginalNamesAsync(kicker);
            FPlayerName violatorNames = await F.GetPlayerOriginalNamesAsync(offender);

            await UCWarfare.ToUpdate();

            Provider.kick(bannedPlayer.Player.channel.owner.playerID.steamID, reason);
            string translation = "kick_kicked_console";
            if (kicker == 0) translation += "_operator";
            L.Log(Translation.Translate(translation, 0, out _, violatorNames.PlayerName, offender.ToString(Data.Locale),
                adminNames.PlayerName, kicker.ToString(Data.Locale), reason), ConsoleColor.Cyan);
            if (admin != null)
                admin.SendChat("kick_kicked_feedback", violatorNames.CharacterName);
            translation = "kick_kicked_broadcast";
            if (kicker == 0)
            {
                translation += "_operator";
                Chat.BroadcastToAllExcept(new ulong[1] { kicker }, translation, violatorNames.CharacterName, adminNames.CharacterName);
            }
            else
                Chat.Broadcast(translation, violatorNames.CharacterName, adminNames.CharacterName);
            return;

            NoPlayer:
            if (admin == null)
                L.Log(Translation.Translate("kick_no_player_found_console", 0, offender.ToString(Data.Locale)));
            else
                admin.SendChat("kick_no_player_found", offender.ToString(Data.Locale));
        }

        public static bool IsValidSteam64ID(ulong id)
        {
            return (int)Math.Floor(id / 100000000000000m) == 765;
        }
    }
}