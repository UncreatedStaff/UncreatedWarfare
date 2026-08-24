using DanielWillett.ReflectionTools;
using System;
using System.Text;

namespace Uncreated.Warfare.Util;

internal static class WorkshopUtility
{
    private static readonly StaticGetter<List<Provider.ServerRequiredWorkshopFile>>? TryGetRequiredWorkshopFile
        = Accessor.GenerateStaticGetter<Provider, List<Provider.ServerRequiredWorkshopFile>>("serverRequiredWorkshopFiles");

    /// <summary>
    /// Adds the given workshop file ID to the public mod list.
    /// </summary>
    /// <param name="file">ID of the file to add.</param>
    /// <param name="advertise">Whether or not to update the server listing to include the new mod.</param>
    /// <exception cref="GameThreadException"/>
    /// <returns>Whether or not the mod was able to be added (meaning it wasn't already there).</returns>
    public static bool AddModIdToServerMenu(PublishedFileId_t file, bool advertise = true)
    {
        GameThread.AssertCurrent();

        ulong workshopId = file.m_PublishedFileId;

        if (Provider.getServerWorkshopFileIDs().Contains(workshopId))
            return false;

        Provider.registerServerUsingWorkshopFileId(workshopId);

        if (advertise)
            UpdateGameServerAdvertisement();

        return true;
    }

    /// <summary>
    /// Adds the given workshop file ID from the public mod list.
    /// </summary>
    /// <param name="file">ID of the file to remove.</param>
    /// <param name="advertise">Whether or not to update the server listing to reflect the missing mod.</param>
    /// <exception cref="GameThreadException"/>
    /// <returns>Whether or not the mod was there to remove.</returns>
    public static bool RemoveModIdFromServerMenu(PublishedFileId_t file, bool advertise = true)
    {
        ulong workshopId = file.m_PublishedFileId;

        List<ulong> list = Provider.getServerWorkshopFileIDs();
        if (!list.Remove(workshopId))
            return false;

        TryGetRequiredWorkshopFile?.Invoke().RemoveAll(x => x.fileId == workshopId);

        if (advertise)
            UpdateGameServerAdvertisement();

        return true;
    }

    /// <summary>
    /// Update the server listing to include any new mod changes that weren't implicitly advertised already.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static void UpdateGameServerAdvertisement()
    {
        GameThread.AssertCurrent();

        List<ulong> ids = Provider.getServerWorkshopFileIDs();

        if (ids.Count <= 0)
            return;

        StringBuilder modList = new StringBuilder(ids.Count * 17 + (ids.Count - 1));
        for (int index = 0; index < ids.Count; ++index)
        {
            if (index != 0)
                modList.Append(',');

            modList.Append(ids[index]);
        }

        int ttlLen = modList.Length;

        // split the mod list into 127 character segments of the whole string.
        // See Provder.onDedicatedUGCInstalled and MenuPlayServerInfoUI.onRulesQueryRefreshed
        int segmentCount = (ttlLen - 1) / 127 + 1;
        int segmentIndex = 0;
        SteamGameServer.SetKeyValue("Mod_Count", segmentCount.ToString());
        for (int segmentStartIndex = 0; segmentStartIndex < ttlLen; segmentStartIndex += 127)
        {
            int length = Math.Min(ttlLen - segmentStartIndex, 127);
            string segmentContents = modList.ToString(segmentStartIndex, length);
            SteamGameServer.SetKeyValue($"Mod_{segmentIndex}", segmentContents);
            ++segmentIndex;
        }
    }
}