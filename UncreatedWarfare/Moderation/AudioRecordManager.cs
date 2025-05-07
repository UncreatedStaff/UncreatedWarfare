using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using SDG.NetPak;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Moderation;

[Priority(1)] // before VoiceChatRestrictionsTweak
public class AudioRecordManager : IHostedService
{
    private readonly ILogger<AudioRecordManager> _logger;
    private readonly IPlayerService _playerService;
    private readonly IConfiguration _sysConfig;
    private readonly IAudioConverter? _audioConverter;
    private readonly ModerationTranslations _moderationTranslations;

    private static AudioRecordManager? _instance;

    public int VoiceBufferSize { get; }

    public AudioRecordManager(
        IConfiguration systemConfiguration,
        ILogger<AudioRecordManager> logger,
        HarmonyPatchService patchService,
        IPlayerService playerService,
        TranslationInjection<ModerationTranslations> moderationTranslations,
        IAudioConverter? audioConverter = null)
    {
        _logger = logger;
        _playerService = playerService;
        _audioConverter = audioConverter;
        _moderationTranslations = moderationTranslations.Value;
        _sysConfig = systemConfiguration;

        _instance = this;

        VoiceBufferSize = systemConfiguration.GetValue<int>("audio_recording:buffer_size");

        try
        {
            MethodInfo? method = typeof(PlayerVoice).GetMethod(nameof(PlayerVoice.ReceiveVoiceChatRelay),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                patchService.Patcher.Patch(method, transpiler: Accessor.GetMethod(TranspileReceiveVoiceChatRelay));
            }
            else
            {
                _logger.LogError("Method not found: {0}.", new MethodDefinition(nameof(PlayerVoice.ReceiveVoiceChatRelay))
                    .DeclaredIn<PlayerVoice>(isStatic: false)
                    .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                    .ReturningVoid());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to patch audio listener.");
        }

    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice += OnRelayVoice;
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice -= OnRelayVoice;
        return UniTask.CompletedTask;
    }
    
    public Task<AudioConvertResult> ConvertVoiceAsync([InstantHandle] IEnumerable<ArraySegment<byte>> data, Stream stream, bool leaveOpen, CancellationToken token = default)
    {
        if (_audioConverter is { Enabled: true })
        {
            float volume = _sysConfig.GetValue("audio_recording:volume", 1.5f);
            return _audioConverter.ConvertAsync(stream, leaveOpen, data, volume, token);
        }

        if (!leaveOpen)
            stream.Dispose();
            
        return Task.FromResult(AudioConvertResult.Disabled);
    }

    private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        if (_audioConverter is not { Enabled: true })
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(speaker);
        if (player.Save.HasSeenVoiceChatNotice)
            return;
        
        shouldAllow = false;
        ToastManager toastManager = player.Component<ToastManager>();
        if (player.Component<AudioRecordPlayerComponent>().HasPressedDeny
            || toastManager.TryFindCurrentToastInfo(ToastMessageStyle.Popup, out _))
        {
            return;
        }

        ToastMessage message = ToastMessage.Popup(
            _moderationTranslations.VoiceRecordNoticeTitle.Translate(player),
            _moderationTranslations.VoiceRecordNoticeDescription.Translate(player),
            _moderationTranslations.VoiceRecordNoticeAcceptButton.Translate(player),
            _moderationTranslations.VoiceRecordNoticeDenyButton.Translate(player),
            callbacks: new PopupCallbacks(AcceptPressed, DenyPressed)
        );

        player.SendToast(message);
    }

    private void DenyPressed(WarfarePlayer player, int button, in ToastMessage message, ref bool consume, ref bool closewindow)
    {
        player.Component<AudioRecordPlayerComponent>().HasPressedDeny = true;
        _logger.LogInformation($"Player {player} denied the voice chat recording agreement.");
        if (player.UnturnedPlayer.voice.GetCustomAllowTalking())
            player.UnturnedPlayer.voice.ServerSetPermissions(player.UnturnedPlayer.voice.GetAllowTalkingWhileDead(), false);
    }

    private void AcceptPressed(WarfarePlayer player, int button, in ToastMessage message, ref bool consume, ref bool closewindow)
    {
        player.Component<AudioRecordPlayerComponent>().HasPressedDeny = false;
        player.Save.HasSeenVoiceChatNotice = true;
        player.Save.Save();

        if (!player.Component<PlayerModerationCacheComponent>().IsMuted() && !player.UnturnedPlayer.voice.GetCustomAllowTalking())
        {
            player.UnturnedPlayer.voice.ServerSetPermissions(player.UnturnedPlayer.voice.GetAllowTalkingWhileDead(), true);
        }

        _logger.LogInformation($"Player {player} accepted the voice chat recording agreement.");
    }

    private static void OnVoiceActivity(PlayerVoice voice, ArraySegment<byte> data)
    {
        if (_instance?._audioConverter == null || !_instance._audioConverter.Enabled)
            return;

        IPlayerService playerService = _instance._playerService;
        WarfarePlayer player = playerService.GetOnlinePlayer(voice.player);
        player.Component<AudioRecordPlayerComponent>().AppendPacket(data);
    }

    private static IEnumerable<CodeInstruction> TranspileReceiveVoiceChatRelay(IEnumerable<CodeInstruction> instructions, MethodBase method)
    {
        MethodInfo? readbytesPtr = typeof(NetPakReader)
                                   .GetMethod(nameof(NetPakReader.ReadBytesPtr), BindingFlags.Instance | BindingFlags.Public);
        if (readbytesPtr == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find NetPakReader.ReadBytesPtr(int, out byte[], out int).");
            return instructions;
        }

        ConstructorInfo? arrSegmentCtor = typeof(ArraySegment<byte>)
                                         .GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [ typeof(byte[]), typeof(int), typeof(int) ], null);
        if (arrSegmentCtor == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find ArraySegment<byte>(byte[], int, int).");
            return instructions;
        }

        FieldInfo? rpcField = typeof(PlayerVoice).GetField("SendPlayVoiceChat", BindingFlags.NonPublic | BindingFlags.Static);
        if (readbytesPtr == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find PlayerVoice.SendPlayVoiceChat.");
            return instructions;
        }

        Type? parameterType = Type.GetType("SDG.Unturned.PlayerVoice+SendPlayVoiceChatWriteParameters, Assembly-CSharp");
        if (parameterType == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find type SDG.Unturned.PlayerVoice.SendPlayVoiceChatWriteParameters.");
            return instructions;
        }

        FieldInfo? byteArrayField = parameterType.GetField("source", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo? compressedSizeField = parameterType.GetField("compressedSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo? sourceOffsetField = parameterType.GetField("sourceOffset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (byteArrayField == null)
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find SendPlayVoiceChatWriteParameters.source.");
        if (compressedSizeField == null)
            WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to find SendPlayVoiceChatWriteParameters.compressedSize.");
        if (sourceOffsetField == null)
            WarfareModule.Singleton.GlobalLogger.LogWarning( $"{method} - Failed to find SendPlayVoiceChatWriteParameters.sourceOffset.");
        if (byteArrayField == null || compressedSizeField == null || sourceOffsetField == null)
        {
            return instructions;
        }


        MethodInfo invokeTarget = new Action<PlayerVoice, ArraySegment<byte>>(OnVoiceActivity).Method;

        List<CodeInstruction> ins = [ ..instructions ];

        LocalBuilder? parametersLocal = null;

        bool success = false;
        for (int i = 0; i < ins.Count; ++i)
        {
            CodeInstruction opcode = ins[i];
            // find local of parameters struct
            if (i > 0 && opcode.opcode == OpCodes.Initobj && opcode.operand is Type t && t == parameterType)
            {
                // last instruction should be a ldloca[.s] <LocalBuilder>
                // there are no ldloca immidiate instructions
                parametersLocal = ins[i - 1].operand as LocalBuilder;
            }
            // find RPC call
            else if (parametersLocal != null && opcode.LoadsField(rpcField))
            {
                ins.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                ins.Insert(i + 1, new CodeInstruction(OpCodes.Ldloca, parametersLocal));
                ins.Insert(i + 2, new CodeInstruction(OpCodes.Ldfld, byteArrayField));
                ins.Insert(i + 3, new CodeInstruction(OpCodes.Ldloca, parametersLocal));
                ins.Insert(i + 4, new CodeInstruction(OpCodes.Ldfld, sourceOffsetField));
                ins.Insert(i + 5, new CodeInstruction(OpCodes.Ldloca, parametersLocal));
                ins.Insert(i + 6, new CodeInstruction(OpCodes.Ldfld, compressedSizeField));

                ins.Insert(i + 7, new CodeInstruction(OpCodes.Newobj, arrSegmentCtor));
                ins.Insert(i + 8, new CodeInstruction(OpCodes.Call, invokeTarget));
                ins[i + 9].MoveLabelsTo(ins[i]);
                success = true;
                break;
            }
        }

        WarfareModule.Singleton.GlobalLogger.LogInformation($"{method} - Patched incoming voice data.");

        if (success)
            return ins;

        WarfareModule.Singleton.GlobalLogger.LogWarning($"{method} - Failed to patch voice to copy voice data for recording.");
        return ins;
    }
}