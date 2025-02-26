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
    private readonly IAudioConverter _audioConverter;
    private readonly ModerationTranslations _moderationTranslations;

    private static AudioRecordManager? _instance;

    public int VoiceBufferSize { get; }

    public AudioRecordManager(
        IConfiguration systemConfiguration,
        ILogger<AudioRecordManager> logger,
        HarmonyPatchService patchService,
        IPlayerService playerService,
        IAudioConverter audioConverter,
        TranslationInjection<ModerationTranslations> moderationTranslations)
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
        if (_audioConverter.Enabled)
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
        if (!_audioConverter.Enabled)
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
    }

    private void AcceptPressed(WarfarePlayer player, int button, in ToastMessage message, ref bool consume, ref bool closewindow)
    {
        player.Component<AudioRecordPlayerComponent>().HasPressedDeny = false;
        player.Save.HasSeenVoiceChatNotice = true;
        player.Save.Save();

        _logger.LogInformation($"Player {player} accepted the voice chat recording agreement.");
    }

    private static void OnVoiceActivity(PlayerVoice voice, ArraySegment<byte> data)
    {
        if (_instance == null || !_instance._audioConverter.Enabled)
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
            WarfareModule.Singleton.GlobalLogger.LogWarning("{0} - Failed to find NetPakReader.ReadBytesPtr(int, out byte[], out int).", method);
            return instructions;
        }

        ConstructorInfo? arrSegmentCtor = typeof(ArraySegment<byte>)
                                         .GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [ typeof(byte[]), typeof(int), typeof(int) ], null);
        if (arrSegmentCtor == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("{0} - Failed to find ArraySegment<byte>(byte[], int, int).", method);
            return instructions;
        }

        FieldInfo? rpcField = typeof(PlayerVoice).GetField("SendPlayVoiceChat", BindingFlags.NonPublic | BindingFlags.Static);
        if (readbytesPtr == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("{0} - Failed to find PlayerVoice.SendPlayVoiceChat.", method);
            return instructions;
        }

        MethodInfo invokeTarget = new Action<PlayerVoice, ArraySegment<byte>>(OnVoiceActivity).Method;

        List<CodeInstruction> ins = [ ..instructions ];

        int readCallPos = -1;
        bool success = false;
        for (int i = 0; i < ins.Count; ++i)
        {
            CodeInstruction opcode = ins[i];
            // find ReadBytesPtr call
            if (readCallPos == -1 && opcode.Calls(readbytesPtr))
            {
                readCallPos = i;
                if (i == ins.Count - 1)
                    break;
            }
            // find target of branch statement
            else if (readCallPos != -1 && opcode.LoadsField(rpcField))
            {
                // locals are stored in display class since they're used in a lambda function.

                // this can either be int or LocalBuilder
                object? displayClassLocal = null;

                FieldInfo? byteArrLocal = null,
                           offsetLocal = null,
                           lengthLocal = null;

                // find display class local and fields in the display class
                for (int j = readCallPos - 6; j < readCallPos; ++j)
                {
                    CodeInstruction backtrackOpcode = ins[j];
                    if (backtrackOpcode.IsLdloc())
                    {
                        if (backtrackOpcode.opcode == OpCodes.Ldloc_0)
                            displayClassLocal = 0;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_1)
                            displayClassLocal = 1;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_2)
                            displayClassLocal = 2;
                        else if (backtrackOpcode.opcode == OpCodes.Ldloc_3)
                            displayClassLocal = 3;
                        else
                            displayClassLocal = (LocalBuilder)backtrackOpcode.operand;
                    }
                    else if (backtrackOpcode.opcode == OpCodes.Ldfld || backtrackOpcode.opcode == OpCodes.Ldflda)
                    {
                        FieldInfo loadedField = (FieldInfo)backtrackOpcode.operand;
                        if (lengthLocal == null)
                            lengthLocal = loadedField;
                        else if (byteArrLocal == null)
                            byteArrLocal = loadedField;
                        else if (offsetLocal == null)
                            offsetLocal = loadedField;
                    }
                }

                if (displayClassLocal == null || byteArrLocal == null || offsetLocal == null || lengthLocal == null)
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning("{0} - Failed to discover local fields or display class local. disp class: {1}, byte arr: {2}, offset: {3}, length: {4}.",
                        method, displayClassLocal != null, byteArrLocal != null, offsetLocal != null, lengthLocal != null);
                    break;
                }

                CodeInstruction startInstruction = new CodeInstruction(
                    displayClassLocal switch
                    {
                        int index => index switch
                        {
                            0 => OpCodes.Ldloc_0,
                            1 => OpCodes.Ldloc_1,
                            2 => OpCodes.Ldloc_2,
                            _ => OpCodes.Ldloc_3
                        },
                        _ => OpCodes.Ldloc
                    }, displayClassLocal is LocalBuilder ? displayClassLocal : null);
                ins.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                ins.Insert(i + 1, startInstruction);
                ins.Insert(i + 2, new CodeInstruction(OpCodes.Ldfld, byteArrLocal));
                ins.Insert(i + 3, new CodeInstruction(startInstruction.opcode, startInstruction.operand));
                ins.Insert(i + 4, new CodeInstruction(OpCodes.Ldfld, offsetLocal));
                ins.Insert(i + 5, new CodeInstruction(startInstruction.opcode, startInstruction.operand));
                ins.Insert(i + 6, new CodeInstruction(OpCodes.Ldfld, lengthLocal));

                ins.Insert(i + 7, new CodeInstruction(OpCodes.Newobj, arrSegmentCtor));
                ins.Insert(i + 8, new CodeInstruction(OpCodes.Call, invokeTarget));
                ins[i + 9].MoveLabelsTo(ins[i]);
                success = true;
                break;
            }
        }

        WarfareModule.Singleton.GlobalLogger.LogInformation("{0} - Patched incoming voice data.", method);

        if (success)
            return ins;

        WarfareModule.Singleton.GlobalLogger.LogWarning("{0} - Failed to patch voice to copy voice data for recording.", method);
        return ins;
    }
}