using System;
using System.IO;
using Uncreated.Warfare.Logging;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Networking;

/// <summary>
/// Utility to download data directly to a stream using <see cref="UnityWebRequest"/>.
/// </summary>
public class UnityStreamDownloadHandler(Stream stream, bool leaveOpen = false, int bufferSize = 2048) : DownloadHandlerScript(new byte[bufferSize])
{
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        try
        {
            stream.Write(data, 0, dataLength);
            return true;
        }
        catch (Exception ex)
        {
            L.LogError($"Error in UnityStreamDownloadHandler when writing to {stream.GetType().Name}. Aborting download.");
            L.LogError(ex);
            return false;
        }
    }

    protected override void CompleteContent()
    {
        DisposeIntl();
        base.CompleteContent();
    }

    public override void Dispose()
    {
        DisposeIntl();
        base.Dispose();
    }

    private void DisposeIntl()
    {
        try
        {
            stream.Flush();
        }
        catch
        {
            // ignored
        }

        if (leaveOpen)
            return;

        stream.Dispose();
        leaveOpen = true;
    }
}