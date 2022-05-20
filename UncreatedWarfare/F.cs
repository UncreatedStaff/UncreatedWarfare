using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare
{
    public static class F
    {
        public const float SPAWN_HEIGHT_ABOVE_GROUND = 0.5f;
        public static readonly char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u' };
        /// <summary>Convert an HTMLColor string to a actual color.</summary>
        /// <param name="htmlColorCode">A hexadecimal/HTML color key.</param>
        public static Color Hex(this string htmlColorCode)
        {
            string code = "#";
            if (htmlColorCode.Length > 0 && htmlColorCode[0] != '#')
                code += htmlColorCode;
            else
                code = htmlColorCode;
            if (ColorUtility.TryParseHtmlString(code, out Color color))
                return color;
            else if (ColorUtility.TryParseHtmlString(htmlColorCode, out color))
                return color;
            else return Color.white;
        }
        public static byte[] CloneBytes(byte[] src)
        {
            int length = src.Length;
            byte[] output = new byte[length];
            Buffer.BlockCopy(src, 0, output, 0, length);
            return output;
        }
        public static string FilterRarityToHex(string color)
        {
            if (color == null)
                return UCWarfare.GetColorHex("default");
            string f1 = "color=" + color;
            string f2 = ItemTool.filterRarityRichText(f1);
            string rtn;
            if (f2.Equals(f1) || f2.Length <= 7)
                rtn = color;
            else
                rtn = f2.Substring(7); // 7 is "color=#" length
            if (!int.TryParse(rtn, System.Globalization.NumberStyles.HexNumber, Data.Locale, out _))
                return UCWarfare.GetColorHex("default");
            else return rtn;
        }
        public static string MakeRemainder(this string[] array, int startIndex = 0, int length = -1, string deliminator = " ")
        {
            StringBuilder builder = new StringBuilder();
            for (int i = startIndex; i < (length == -1 ? array.Length : length); i++)
            {
                if (i > startIndex) builder.Append(deliminator);
                builder.Append(array[i]);
            }
            return builder.ToString();
        }
        public static int DivideRemainder(float divisor, float dividend, out int remainder)
        {
            float answer = divisor / dividend;
            remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * dividend);
            return (int)Mathf.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, uint dividend, out uint remainder)
        {
            decimal answer = (decimal)divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, decimal dividend, out uint remainder)
        {
            decimal answer = divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static bool PermissionCheck(this IRocketPlayer player, EAdminType type)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(player, false);
            for (int i = 0; i < groups.Count; i++)
            {
                RocketPermissionsGroup grp = groups[i];
                if (grp.Id.Equals("default", StringComparison.Ordinal)) continue;
                if (grp.Id.Equals(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, StringComparison.Ordinal))
                {
                    if ((type & EAdminType.ADMIN_OFF_DUTY) == EAdminType.ADMIN_OFF_DUTY) return true;
                    continue;
                }
                if (grp.Id.Equals(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, StringComparison.Ordinal))
                {
                    if ((type & EAdminType.ADMIN_ON_DUTY) == EAdminType.ADMIN_ON_DUTY) return true;
                    continue;
                }
                if (grp.Id.Equals(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, StringComparison.Ordinal))
                {
                    if ((type & EAdminType.TRIAL_ADMIN_OFF_DUTY) == EAdminType.TRIAL_ADMIN_OFF_DUTY) return true;
                    continue;
                }
                if (grp.Id.Equals(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, StringComparison.Ordinal))
                {
                    if ((type & EAdminType.TRIAL_ADMIN_ON_DUTY) == EAdminType.TRIAL_ADMIN_ON_DUTY) return true;
                    continue;
                }
                if (grp.Id.Equals(UCWarfare.Config.AdminLoggerSettings.HelperGroup, StringComparison.Ordinal))
                {
                    if ((type & EAdminType.HELPER) == EAdminType.HELPER) return true;
                    continue;
                }
            }
            return false;
        }
        public static EAdminType GetPermissions(this IRocketPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(player, false);
            EAdminType perms = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                RocketPermissionsGroup grp = groups[i];
                if (grp.Id == "default") continue;
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup)
                {
                    perms |= EAdminType.ADMIN;
                }
                else if (grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup)
                {
                    perms |= EAdminType.TRIAL_ADMIN;
                }
                else if (grp.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup)
                {
                    perms |= EAdminType.HELPER;
                }
            }
            return perms;
        }
        public static unsafe string ToProperCase(this string input)
        {
            char[] output = new char[input.Length];
            fixed (char* p = input)
            {
                char last = ' ';
                for (int i = 0; i < input.Length; ++i)
                {
                    char current = *(p + i);
                    if (current == '_') output[i] = ' ';
                    else if (last == ' ' || last == '_')
                    {
                        output[i] = char.ToUpperInvariant(current);
                    }
                    else
                    {
                        output[i] = char.ToLowerInvariant(current);
                    }
                    last = current;
                }
            }
            return new string(output);
        }
        public static bool OnDutyOrAdmin(this IRocketPlayer player) => (player is UnturnedPlayer pl && pl.Player.channel.owner.isAdmin) || (player is UCPlayer upl && upl.Player.channel.owner.isAdmin) || player.PermissionCheck(EAdminType.MODERATE_PERMS_ON_DUTY);
        public static bool OnDuty(this IRocketPlayer player) => player.PermissionCheck(EAdminType.MODERATE_PERMS_ON_DUTY);
        public static bool OffDuty(this IRocketPlayer player) => !OnDuty(player);
        public static bool IsIntern(this IRocketPlayer player) => player.PermissionCheck(EAdminType.TRIAL_ADMIN);
        public static bool IsAdmin(this IRocketPlayer player) => player.PermissionCheck(EAdminType.ADMIN);
        public static bool IsHelper(this IRocketPlayer player) => player.PermissionCheck(EAdminType.HELPER);
        /// <summary>Ban someone for <paramref name="duration"/> seconds.</summary>
        /// <param name="duration">Duration of ban IN SECONDS</param>
        public static void OfflineBan(ulong offender, uint ipAddress, CSteamID banner, string reason, uint duration)
        {
            CSteamID banned = new CSteamID(offender);
            Provider.ban(banned, reason, duration);
            for (int index = 0; index < SteamBlacklist.list.Count; ++index)
            {
                if (SteamBlacklist.list[index].playerID.m_SteamID == offender)
                {
                    SteamBlacklist.list[index].judgeID = banner;
                    SteamBlacklist.list[index].reason = reason;
                    SteamBlacklist.list[index].duration = duration;
                    SteamBlacklist.list[index].banned = Provider.time;
                    return;
                }
            }
            SteamBlacklist.list.Add(new SteamBlacklistID(banned, ipAddress, banner, reason, duration, Provider.time));
        }
        public static string An(this string word)
        {
            if (word.Length > 0)
            {
                char first = char.ToLower(word[0]);
                for (int i = 0; i < vowels.Length; i++)
                    if (vowels[i] == first)
                        return "n";
            }
            return string.Empty;
        }
        public static string An(this char letter)
        {
            char let = char.ToLower(letter);
            for (int i = 0; i < vowels.Length; i++)
                if (vowels[i] == let)
                    return "n";
            return string.Empty;
        }
        public static string S(this int number) => number == 1 ? string.Empty : "s";
        public static string S(this float number) => number == 1 ? string.Empty : "s";
        public static string S(this uint number) => number == 1 ? string.Empty : "s";
        public static ulong GetTeamFromPlayerSteam64ID(this ulong s64)
        {
            if (!Data.Is<ITeams>(out _))
            {
                SteamPlayer pl2 = PlayerTool.getSteamPlayer(s64);
                if (pl2 == null) return 0;
                else return pl2.player.quests.groupID.m_SteamID;
            }
            SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
            if (pl == default)
            {
                if (PlayerManager.HasSave(s64, out PlayerSave save))
                    return save.Team;
                else return 0;
            }
            else return pl.GetTeam();
        }
        public static ulong GetTeam(this UCPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this UnturnedPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this ulong groupID)
        {
            if (!Data.Is<ITeams>(out _)) return groupID;
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this UnturnedPlayer player) => GetTeamByte(player.Player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this ulong groupID)
        {
            if (!Data.Is<ITeams>(out _)) return groupID > byte.MaxValue ? byte.MaxValue : (byte)groupID;
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static Vector3 GetBaseSpawn(this Player player)
        {
            if (!Data.Is<ITeams>(out _)) return TeamManager.LobbySpawn;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this Player player, out ulong team)
        {
            if (!Data.Is<ITeams>(out _))
            {
                team = player.quests.groupID.m_SteamID;
                return TeamManager.LobbySpawn;
            }
            team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this ulong playerID, out ulong team)
        {
            team = playerID.GetTeamFromPlayerSteam64ID();
            if (!Data.Is<ITeams>(out _))
            {
                return TeamManager.LobbySpawn;
            }
            return team.GetBaseSpawnFromTeam();
        }
        public static Vector3 GetBaseSpawnFromTeam(this ulong team)
        {
            if (!Data.Is<ITeams>(out _))
            {
                return TeamManager.LobbySpawn;
            }
            if (team == 1) return TeamManager.Team1Main.Center3D;
            else if (team == 2) return TeamManager.Team2Main.Center3D;
            else return TeamManager.LobbySpawn;
        }
        public static float GetBaseAngle(this ulong team)
        {
            if (!Data.Is<ITeams>(out _))
            {
                return TeamManager.LobbySpawnAngle;
            }
            if (team == 1) return TeamManager.Team1SpawnAngle;
            else if (team == 2) return TeamManager.Team2SpawnAngle;
            else return TeamManager.LobbySpawnAngle;
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(client);
                if (pl != null)
                    newtext = Translation.TranslateSign(text, pl, false);
            }
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        /// <summary>Runs one player at a time instead of one language at a time. Used for kit signs.</summary>
        public static void InvokeSignUpdateForAll(InteractableSign sign, byte x, byte y, string text)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (text == null) return;
            if (text.StartsWith("sign_"))
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    SteamPlayer pl = Provider.clients[i];
                    if (Regions.checkArea(x, y, pl.player.movement.region_x, pl.player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    {
                        UCPlayer? pl2 = UCPlayer.FromSteamPlayer(pl);
                        if (pl2 != null)
                            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, pl.transportConnection, Translation.TranslateSign(text, pl2, false));
                    }
                }
            }
        }
        public static IEnumerable<SteamPlayer> EnumerateClients_Remote(byte x, byte y, byte distance)
        {
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer client = Provider.clients[i];
                if (client.player != null && Regions.checkArea(x, y, client.player.movement.region_x, client.player.movement.region_y, distance))
                    yield return client;
            }
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, bool changeText = false, string text = "")
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (text == default || client == default) return;
            string newtext;
            if (!changeText)
                newtext = sign.text;
            else newtext = text;
            if (newtext.StartsWith("sign_"))
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(client);
                if (pl != null)
                    newtext = Translation.TranslateSign(newtext, pl, false);
            }
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        public static float GetTerrainHeightAt2DPoint(float x, float z, float above = 0)
        {
            return LevelGround.getHeight(new Vector3(x, 0, z)) + above;
        }
        internal static float GetHeight(Vector2 point, float minHeight) => GetHeight(new Vector3(point.x, 0f, point.y), minHeight);
        internal static float GetHeight(Vector3 point, float minHeight)
        {
            float height;
            if (Physics.Raycast(new Ray(new Vector3(point.x, Level.HEIGHT, point.z), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
            {
                height = hit.point.y;
                if (!float.IsNaN(minHeight))
                    return Mathf.Max(height, minHeight);
                return height;
            }
            else
            {
                height = LevelGround.getHeight(point);
                if (!float.IsNaN(minHeight))
                    return Mathf.Max(height, minHeight);
                else return height;
            }
        }
        public static float GetHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
                return h.point.y + above;
            else return defaultY;
        }
        public static string ReplaceCaseInsensitive(this string source, string replaceIf, string replaceWith = "")
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (replaceIf == null || replaceWith == null || source.Length == 0 || replaceIf.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = source.ToLower().ToCharArray();
            char[] replaceIfChars = replaceIf.ToLower().ToCharArray();
            StringBuilder buffer = new StringBuilder();
            int replaceIfLength = replaceIfChars.Length;
            StringBuilder newString = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (buffer.Length < replaceIfLength)
                {
                    if (lowerchars[i] == replaceIfChars[buffer.Length]) buffer.Append(chars[i]);
                    else
                    {
                        if (buffer.Length != 0)
                            newString.Append(buffer.ToString());
                        buffer.Clear();
                        newString.Append(chars[i]);
                    }
                }
                else
                {
                    if (replaceWith.Length != 0) newString.Append(replaceWith);
                    newString.Append(chars[i]);
                }
            }
            return newString.ToString();
        }
        public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (replacables.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
            char[] lowerrepls;
            if (!caseSensitive)
            {
                lowerrepls = new char[replacables.Length];
                for (int i = 0; i < replacables.Length; i++)
                {
                    lowerrepls[i] = char.ToLower(replacables[i]);
                }
            }
            else lowerrepls = replacables;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                bool found = false;
                for (int c = 0; c < lowerrepls.Length; c++)
                {
                    if (lowerrepls[c] == lowerchars[i])
                    {
                        found = true;
                    }
                }
                if (!found) sb.Append(chars[i]);
            }
            return sb.ToString();
        }
        public static void TriggerEffectReliable(ushort ID, CSteamID player, Vector3 position)
        {
            TriggerEffectParameters p = new TriggerEffectParameters(ID)
            {
                position = position,
                reliable = true,
                relevantPlayerID = player
            };
            EffectManager.triggerEffect(p);
        }
        public static void TriggerEffectReliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
        {
            EffectManager.sendEffectReliable(asset.id, connection, position);
        }
        public static bool SavePhotoToDisk(string path, Texture2D texture)
        {
            byte[] data = texture.EncodeToPNG();
            try
            {
                FileStream stream = File.Create(path);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch { return false; }
        }
        public static bool TryGetPlaytimeComponent(this Player player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success)!;
            return success;
        }
        public static bool TryGetPlaytimeComponent(this CSteamID player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success)!;
            return success;
        }
        public static bool TryGetPlaytimeComponent(this ulong player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success)!;
            return success;
        }
        public static PlaytimeComponent? GetPlaytimeComponent(this Player player, out bool success)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.PlaytimeComponents.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out PlaytimeComponent pt))
            {
                success = pt != null;
                return pt;
            }
            else if (player == null || player.transform == null)
            {
                success = false;
                return null;
            }
            else if (player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
        public static PlaytimeComponent? GetPlaytimeComponent(this CSteamID player, out bool success)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.PlaytimeComponents.TryGetValue(player.m_SteamID, out PlaytimeComponent pt))
            {
                success = pt != null;
                return pt;
            }
            else if (player == default || player == CSteamID.Nil)
            {
                success = false;
                return null;
            }
            else
            {
                Player p = PlayerTool.getPlayer(player);
                if (p == null)
                {
                    success = false;
                    return null;
                }
                if (p.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static PlaytimeComponent? GetPlaytimeComponent(this ulong player, out bool success)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player == 0)
            {
                success = false;
                return default;
            }
            if (Data.PlaytimeComponents.TryGetValue(player, out PlaytimeComponent pt))
            {
                success = pt != null;
                return pt;
            }
            else
            {
                SteamPlayer p = PlayerTool.getSteamPlayer(player);
                if (p == default || p.player == default)
                {
                    success = false;
                    return null;
                }
                if (p.player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static FPlayerName GetPlayerOriginalNames(UCPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(SteamPlayer player) => GetPlayerOriginalNames(player.player);
        public static FPlayerName GetPlayerOriginalNames(UnturnedPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.OriginalNames.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out FPlayerName names))
                return names;
            else return new FPlayerName(player);
        }
        public static FPlayerName GetPlayerOriginalNames(ulong player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
                return names;
            else
            {
                SteamPlayer? pl = PlayerTool.getSteamPlayer(player);
                if (pl == default)
                    return Data.DatabaseManager.GetUsernames(player);
                else return new FPlayerName()
                {
                    CharacterName = pl.playerID.characterName,
                    NickName = pl.playerID.nickName,
                    PlayerName = pl.playerID.playerName,
                    Steam64 = player
                };
            }
        }
        public static Task<FPlayerName> GetPlayerOriginalNamesAsync(ulong player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
                return Task.FromResult(names);
            else if (OffenseManager.IsValidSteam64ID(player))
            {
                SteamPlayer? pl = PlayerTool.getSteamPlayer(player);
                if (pl == default)
                    return Data.DatabaseManager.GetUsernamesAsync(player);
                else return Task.FromResult(new FPlayerName()
                {
                    CharacterName = pl.playerID.characterName,
                    NickName = pl.playerID.nickName,
                    PlayerName = pl.playerID.playerName,
                    Steam64 = player
                });
            }
            return Task.FromResult(FPlayerName.Nil);
        }
        public static bool IsInMain(this Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.Is<ITeams>(out _)) return false;
            ulong team = player.GetTeam();
            if (team == 1) return TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsInMain(Vector3 point)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.Is<ITeams>(out _)) return false;
            return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
        }
        public static bool IsOnFlag(this Player player) => player != null && Data.Is(out IFlagRotation fg) && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
        public static bool IsOnFlag(this Player player, out Flag flag)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (player != null && Data.Is(out IFlagRotation fg))
            {
                if (fg.OnFlag == null || fg.Rotation == null)
                {
                    flag = null!;
                    return false;
                }
                if (fg.OnFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int id))
                {
                    flag = fg.Rotation.Find(x => x.ID == id);
                    return flag != null;
                }
            }
            flag = null!;
            return false;
        }
        public static string Colorize(this string inner, string colorhex) => $"<color=#{colorhex}>{inner}</color>";
        public static string ColorizeName(string innerText, ulong team)
        {
            if (!Data.Is<ITeams>(out _)) return innerText;
            if (team == TeamManager.ZOMBIE_TEAM_ID) return $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>";
            else if (team == TeamManager.Team1ID) return $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>";
            else if (team == TeamManager.Team2ID) return $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>";
            else if (team == TeamManager.AdminID) return $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>";
            else return $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>";
        }
        public static void CheckDir(string path, out bool success, bool unloadIfFail = false)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    success = true;
                    L.Log("Created directory: \"" + path + "\".", ConsoleColor.Magenta);
                }
                catch (Exception ex)
                {
                    L.LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                    success = false;
                    if (unloadIfFail)
                        UCWarfare.I?.UnloadPlugin();
                }
            }
            else success = true;
        }
        public static void SaveProfilingData()
        {
            CheckDir(Data.DATA_DIRECTORY + "Profiling\\", out _);
            string fi = Data.DATA_DIRECTORY + "Profiling\\" + DateTime.Now.ToString("yyyy-mm-dd_HH-mm-ss") + "_profile.csv";
            L.Log("Flushing profiling information to \"" + fi + "\"", ConsoleColor.Cyan);
            ProfilingUtils.WriteAllDataToCSV(fi);
            ProfilingUtils.Clear();
        }
        public static void SendSteamURL(this SteamPlayer player, string message, ulong SteamID) => player.SendURL(message, $"https://steamcommunity.com/profiles/{SteamID}/");
        public static void SendURL(this SteamPlayer player, string message, string url)
        {
            if (player == default || url == default) return;
            player.player.sendBrowserRequest(message, url);
        }
        public static string GetLayer(Vector3 direction, Vector3 origin, int Raymask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 8192f, Raymask))
            {
                if (hit.transform != null)
                    return hit.transform.gameObject.layer.ToString();
                else return "nullHitNoTransform";
            }
            else return "nullNoHit";
        }
        public static bool CanStandAtLocation(Vector3 source)
        {
            return Physics.OverlapCapsuleNonAlloc(source + new Vector3(0.0f, PlayerStance.RADIUS + 0.01f, 0.0f), source +
                new Vector3(0.0f, PlayerMovement.HEIGHT_STAND + 0.5f - PlayerStance.RADIUS, 0.0f), PlayerStance.RADIUS, PlayerStance.checkColliders,
                RayMasks.BLOCK_STANCE, QueryTriggerInteraction.Ignore) == 0;
        }

        private static string emp = string.Empty;
        public static string GetClosestLocation(Vector3 point)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ref string closest = ref emp;
            float smallest = -1f;
            for (int i = 0; i < LevelNodes.nodes.Count; i++)
            {
                if (LevelNodes.nodes[i] is LocationNode node)
                {
                    float amt = (point - node.point).sqrMagnitude;
                    if (smallest == -1 || amt < smallest)
                    {
                        closest = ref node.name;
                        smallest = amt;
                    }
                }
            }
            int index = GetClosestLocationIndex(point);
            return index == -1 ? string.Empty : ((LocationNode)LevelNodes.nodes[index]).name;
        }
        public static int GetClosestLocationIndex(Vector3 point)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            int index = -1;
            float smallest = -1f;
            for (int i = 0; i < LevelNodes.nodes.Count; i++)
            {
                if (LevelNodes.nodes[i] is LocationNode node)
                {
                    float amt = (point - node.point).sqrMagnitude;
                    if (smallest == -1 || amt < smallest)
                    {
                        index = i;
                        smallest = amt;
                    }
                }
            }
            return index;
        }
        public static void NetInvoke(this NetCall call) =>
            call.Invoke(Data.NetClient);
        public static void NetInvoke<T>(this NetCallRaw<T> call, T arg) =>
            call.Invoke(Data.NetClient, arg);
        public static void NetInvoke<T1, T2>(this NetCallRaw<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCallRaw<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCallRaw<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1>(this NetCall<T1> call, T1 arg1) =>
            call.Invoke(Data.NetClient, arg1);
        public static void NetInvoke<T1, T2>(this NetCall<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCall<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCall<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1, T2, T3, T4, T5>(this NetCall<T1, T2, T3, T4, T5> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6>(this NetCall<T1, T2, T3, T4, T5, T6> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5, arg6);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7>(this NetCall<T1, T2, T3, T4, T5, T6, T7> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) =>
            call.Invoke(Data.NetClient, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        public static bool FilterName(string original, out string final)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (UCWarfare.Config.DisableNameFilter || UCWarfare.Config.MinAlphanumericStringLength <= 0)
            {
                final = original;
                return false;
            }
            IEnumerator<char> charenum = original.GetEnumerator();
            int counter = 0;
            int alphanumcount = 0;
            while (charenum.MoveNext())
            {
                counter++;
                char ch = charenum.Current;
                int c = ch;
                if (c > 31 && c < 127)
                {
                    if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                    {
                        final = original;
                        charenum.Dispose();
                        return false;
                    }
                    else
                    {
                        alphanumcount++;
                    }
                }
                else
                {
                    alphanumcount = 0;
                }
            }
            charenum.Dispose();
            final = original;
            return alphanumcount != original.Length;
        }
        public static DateTime FromUnityTime(this float realtimeSinceStartup) => 
            DateTime.Now - TimeSpan.FromSeconds(Time.realtimeSinceStartup) + TimeSpan.FromSeconds(realtimeSinceStartup);

        /// <summary>
        /// Finds the 2D distance between two Vector3's x and z components.
        /// </summary>
        public static float SqrDistance2D(Vector3 a, Vector3 b) => Mathf.Pow(b.x - a.x, 2) + Mathf.Pow(b.z - a.z, 2);

        private static readonly char[] ABET = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L' };
        private static bool _setGridConstants = false;
        private static float _toMapCoordsMultiplier;
        private static float _gridSize;
        private const int _sqrsTotal = 36;
        private static float _sqrSize;
        private static void SetGridPositionConstants()
        {
            _toMapCoordsMultiplier = Level.size / (Level.size - Level.border * 2f);
            _gridSize = Level.size - Level.border * 2;
            _sqrSize = Mathf.Floor(_gridSize / 36f);
            _setGridConstants = true;
        }
        public static string ToGridPosition(Vector3 pos)
        {
            if (!_setGridConstants) SetGridPositionConstants();
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float x = Level.size / 2 + _toMapCoordsMultiplier * pos.x;
            float y = Level.size / 2 - _toMapCoordsMultiplier * pos.z;

            int xSqr;
            bool isOut = false;
            if (x < Level.border)
            {
                isOut = true;
                xSqr = 0;
            }
            else if (x > Level.border + _gridSize)
            {
                isOut = true;
                xSqr = _sqrsTotal - 1;
            }
            else
                xSqr = Mathf.FloorToInt((x - Level.border) / _sqrSize);
            int ySqr;
            if (y < Level.border)
            {
                isOut = true;
                ySqr = 0;
            }
            else if (y > Level.border + _gridSize)
            {
                isOut = true;
                ySqr = _sqrsTotal - 1;
            }
            else
                ySqr = Mathf.FloorToInt((y - Level.border) / _sqrSize);
            int bigsqrx = Mathf.FloorToInt(xSqr / 3f);
            int smlSqrDstX = xSqr % 3;
            int bigsqry = Mathf.FloorToInt(ySqr / 3f);
            int smlSqrDstY = ySqr % 3;
            string rtn = ABET[bigsqrx] + (bigsqry + 1).ToString();
            if (!isOut) rtn += "-" + (smlSqrDstX + (2 - smlSqrDstY) * 3 + 1).ToString();
            return rtn;
        }
    }
}