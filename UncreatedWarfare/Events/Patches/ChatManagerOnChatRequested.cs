using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using StackCleaner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class ChatManagerOnChatRequested : IHarmonyPatch
{
    private static MethodInfo? _target;
    private static readonly string[] ChatLevelsRaw = [ "GLO", "A/S", "GRP" ];
    private static readonly string[] ChatLevelsANSI = [ "\e[42mGLO\e[49m", "\e[41mA/S\e[49m", "\e[43mGRP\e[49m" ];
    private static readonly string[] ChatLevelsExtendedANSI = ChatLevelsANSI;

    public static readonly PermissionLeaf AdminChatPermissions = new PermissionLeaf("features.admin_chat", false, true);

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = Accessor.GetMethod(ChatManager.ReceiveChatRequest);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for receive chat message method.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(ChatManager.ReceiveChatRequest))
                .DeclaredIn<ChatManager>(isStatic: true)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .WithParameter<byte>("flags")
                .WithParameter<string>("text")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for receive chat message method.", _target);
        _target = null;
    }

    private static ILogger? _chatLogger;


    // SDG.Unturned.ChatManager
    /// <summary>
    /// Postfix of <see cref="ChatManager.ReceiveChatRequest"/> to redo how chat messages are processed.
    /// </summary>
    [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.ReceiveChatRequest))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool Prefix(in ServerInvocationContext context, byte flags, string text)
    {
        return SimulateChatRequest(context.GetCallingPlayer(), text, (EChatMode)(flags & 0b01111111), (flags & (1 << 7)) != 0);
    }

    public static bool SimulateChatRequest(SteamPlayer steamPlayer, string text, EChatMode mode, bool fromUnityEvent)
    {
        ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;

        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(steamPlayer);

        if (!player.IsOnline || Time.realtimeSinceStartup - steamPlayer.lastChat < ChatManager.chatrate)
        {
            return false;
        }

        steamPlayer.lastChat = Time.realtimeSinceStartup;

        if (mode is not EChatMode.GLOBAL and not EChatMode.LOCAL and not EChatMode.GROUP)
        {
            return false;
        }


        if (text.Length < 2 || fromUnityEvent && !Provider.configData.UnityEvents.Allow_Client_Messages)
        {
            return false;
        }

        text = text.Trim();

        if (text.Length > ChatManager.MAX_MESSAGE_LENGTH)
        {
            text = text[..ChatManager.MAX_MESSAGE_LENGTH];
        }

        text = text.Replace("noparse", string.Empty);

        CancellationToken token = player.DisconnectToken;
        UniTask.Create(async () =>
        {
            UserPermissionStore permission = serviceProvider.Resolve<UserPermissionStore>();
            EventDispatcher eventDispatcher = serviceProvider.Resolve<EventDispatcher>();

            bool onDuty = await permission.HasPermissionAsync(player, AdminChatPermissions, token);

            await UniTask.SwitchToMainThread(token);

            Color color = onDuty ? Palette.ADMIN : Palette.AMBIENT;
            bool isRich = true;
            bool isVisible = true;

            ChatManager.onChatted?.Invoke(steamPlayer, mode, ref color, ref isRich, text, ref isVisible);
            if (!ChatManager.process(steamPlayer, text, fromUnityEvent) || !isVisible)
            {
                return;
            }

            string pfx = GetDefaultPrefix(mode, onDuty, player.Team);

            PlayerChatRequested argsRequested = new PlayerChatRequested
            {
                Player = player,
                PlayerName = player.Names,
                HasAdminChatPermissions = onDuty,
                Text = text,
                OriginalText = text,
                IconUrlOverride = null,
                AllowRichText = isRich,
                MessageColor = color,
                ChatMode = mode,
                Prefix = pfx,
                IsUnityMessage = fromUnityEvent,
                ShouldReplicate = true,
                TargetPlayers = args =>
                {
                    const float localSqrRadius = 64 * 64;
                    Vector3 pos = args.Player.Position;
                    return args.ChatMode switch
                    {
                        EChatMode.LOCAL => playerService.OnlinePlayers.Where(x => (pos - x.Position).sqrMagnitude <= localSqrRadius || x.IsInSquadWith(args.Player)),
                        EChatMode.GROUP => playerService.OnlinePlayers.Where(x => x.Team == args.Player.Team),
                        _ => playerService.OnlinePlayers
                    };
                }
            };

            if (!await eventDispatcher.DispatchEventAsync(argsRequested, token))
            {
                return;
            }

            await UniTask.SwitchToMainThread(token);

            if (player.IsDisconnected)
            {
                return;
            }

            _chatLogger ??= serviceProvider.Resolve<ILoggerFactory>().CreateLogger("Unturned.Chat");

            if (CommandWindow.shouldLogChat)
            {
                LogChatMessage(argsRequested, serviceProvider.Resolve<ITranslationValueFormatter>());
            }

            if (ReferenceEquals(argsRequested.Prefix, pfx) && mode != argsRequested.ChatMode)
            {
                argsRequested.Prefix = GetDefaultPrefix(argsRequested.ChatMode, onDuty, player.Team);
            }

            string tmproText = argsRequested.Prefix + (!onDuty ? "<noparse>" + text : text);
            string? imguiText = null;

            ChatService chatService = serviceProvider.Resolve<ChatService>();

            string? FormatFunc(WarfarePlayer destPlayer, bool imgui)
            {
                if (destPlayer.IsDisconnected) return null;

                string name;
                if (onDuty)
                {
                    // on duty admins should always display using their display name
                    name = argsRequested.PlayerName.GetDisplayNameOrPlayerName();
                }
                else if (destPlayer.Team == argsRequested.Player.Team)
                {
                    name = argsRequested.PlayerName.NickName;
                }
                else
                {
                    name = argsRequested.PlayerName.CharacterName;
                }

                string t;
                if (imgui)
                {
                    t = imguiText ??= argsRequested.Prefix + (!onDuty ? text.Replace('<', '{').Replace('>', '}') : text);
                }
                else
                {
                    t = tmproText;
                }

                string targetText = t.Replace("%SPEAKER%", name);

                return targetText;
            }

            Func<PlayerChatRequested, IEnumerable<WarfarePlayer>> targetPlayers = argsRequested.TargetPlayers;
            List<WarfarePlayer>? list = null;
            if (argsRequested.ShouldReplicate)
            {
                IEnumerable<WarfarePlayer> enumerable = argsRequested.TargetPlayers(argsRequested);

                list = ListPool<WarfarePlayer>.claim();
                foreach (WarfarePlayer destPlayer in enumerable)
                {
                    string? msg = FormatFunc(destPlayer, destPlayer.Save.IMGUI);
                    if (msg == null)
                        continue;

                    list.Add(destPlayer);
                    chatService.Send(destPlayer, msg, argsRequested.MessageColor, argsRequested.ChatMode, argsRequested.IconUrlOverride, argsRequested.AllowRichText, argsRequested.Player);
                }

                targetPlayers = _ => list;
            }

            PlayerChatSent argsSent = new PlayerChatSent
            {
                Player = player,
                Text = argsRequested.Text,
                OriginalText = argsRequested.OriginalText,
                PlayerName = argsRequested.PlayerName,
                ChatMode = argsRequested.ChatMode,
                AllowRichText = argsRequested.AllowRichText,
                MessageColor = argsRequested.MessageColor,
                WasReplicated = argsRequested.ShouldReplicate,
                IconUrlOverride = string.IsNullOrWhiteSpace(argsRequested.IconUrlOverride) ? null : argsRequested.IconUrlOverride,
                Prefix = argsRequested.Prefix,
                Request = argsRequested,
                FormatHandler = FormatFunc,
                TargetPlayers = targetPlayers
            };

            if (list == null)
            {
                _ = eventDispatcher.DispatchEventAsync(argsSent, CancellationToken.None);
            }
            else
            {
                await eventDispatcher.DispatchEventAsync(argsSent, CancellationToken.None);
                await UniTask.SwitchToMainThread(CancellationToken.None);
                ListPool<WarfarePlayer>.release(list);
            }
        });

        return false;
    }

    private static readonly (int, ConsoleColor) ChatColor = (TerminalColorHelper.ToArgb(new Color32(180, 180, 100, 255)), ConsoleColor.DarkYellow);

    private static void LogChatMessage(PlayerChatRequested args, ITranslationValueFormatter formatter)
    {
        if (!_chatLogger!.IsEnabled(LogLevel.Information))
            return;

        StackColorFormatType coloring = formatter.TranslationService.TerminalColoring;

        string[] chatLevels = coloring switch
        {
            StackColorFormatType.ExtendedANSIColor => ChatLevelsExtendedANSI,
            StackColorFormatType.ANSIColor => ChatLevelsANSI,
            _ => ChatLevelsRaw
        };

        string modePrefix = chatLevels[(int)args.ChatMode];

        string fileMessage = $"[{args.Steam64.m_SteamID:D17}] {args.PlayerName.PlayerName.Truncate(20),-20} \"{args.OriginalText}\"";
        string logMessage = fileMessage;
        if (coloring is StackColorFormatType.ExtendedANSIColor or StackColorFormatType.ANSIColor)
        {
            bool extended = coloring == StackColorFormatType.ExtendedANSIColor;

            int strLen = TerminalColorHelper.GetTerminalColorSequenceLength(WarfareFormattedLogValues.GetArgb(extended, in WarfareFormattedLogValues.StructDefault))
                         + TerminalColorHelper.GetTerminalColorSequenceLength(WarfareFormattedLogValues.GetArgb(extended, WarfareFormattedLogValues.Colors[typeof(string)]))
                         + TerminalColorHelper.GetTerminalColorSequenceLength(WarfareFormattedLogValues.GetArgb(extended, ChatColor))
                         + args.OriginalText.Length
                         + 41;

            MakeLogMessageState state = default;
            state.Args = args;
            state.Extended = extended;

            logMessage = string.Create(strLen, state, static (span, state) =>
            {
                int index = TerminalColorHelper.WriteTerminalColorSequence(span, WarfareFormattedLogValues.GetArgb(state.Extended, in WarfareFormattedLogValues.StructDefault));
                state.Args.Steam64.m_SteamID.TryFormat(span[index..], out _, "D17", CultureInfo.InvariantCulture);
                index += 17;

                span[index] = ' ';
                ++index;

                index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], WarfareFormattedLogValues.GetArgb(state.Extended, WarfareFormattedLogValues.Colors[typeof(string)]));
                ReadOnlySpan<char> nameSpan = state.Args.PlayerName.PlayerName;
                if (nameSpan.Length <= 20)
                {
                    nameSpan.CopyTo(span[index..]);
                    index += nameSpan.Length;
                    for (int i = nameSpan.Length; i < 20; ++i)
                    {
                        span[index] = ' ';
                        ++index;
                    }
                }
                else
                {
                    nameSpan[..20].CopyTo(span[index..]);
                    index += 20;
                }

                index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], WarfareFormattedLogValues.GetArgb(state.Extended, in ChatColor));

                span[index] = ' ';
                ++index;
                span[index] = '"';
                ++index;

                state.Args.OriginalText.AsSpan().CopyTo(span[index..]);
                index += state.Args.OriginalText.Length;

                span[index] = '"';
            });
        }

        string terminalLog = WarfareLogger.CreateString(formatter, "Unturned.Chat", null, null, logMessage,
                                                        DateTime.UtcNow, LogLevel.Information, false, modePrefix, false, true);

        string fileLog = WarfareLogger.CreateString(formatter, "Unturned.Chat", null, null, fileMessage,
                                                    DateTime.UtcNow, LogLevel.Information, true, ChatLevelsRaw[(int)args.ChatMode], false, true);

        WarfareLoggerProvider.WriteToLogRaw(LogLevel.Information, terminalLog, fileLog);

        if (args.IsUnityMessage)
        {
            UnturnedLog.info($"UnityEventMsg {args.Steam64.m_SteamID}: \"{args.OriginalText}\"", ConsoleColor.Gray);
        }
    }

    private struct MakeLogMessageState
    {
        public PlayerChatRequested Args;
        public bool Extended;
    }

    private static string GetDefaultPrefix(EChatMode mode, bool onDuty, Team team)
    {
        Color32 color = onDuty ? Palette.ADMIN : team.Faction.Color;
        return mode switch
        {
            EChatMode.GROUP => $"[G] <color=#{HexStringHelper.FormatHexColor(color)}>%SPEAKER%</color>: ",
            EChatMode.LOCAL => $"[A/S] <color=#{HexStringHelper.FormatHexColor(color)}>%SPEAKER%</color>: ",
            _ => $"<color=#{HexStringHelper.FormatHexColor(color)}>%SPEAKER%</color>: "
        };
    }

}
