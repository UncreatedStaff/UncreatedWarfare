using System;
using System.IO;
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
            WarfareModule.Singleton.GlobalLogger.LogError(ex, "Error in UnityStreamDownloadHandler when writing to {0}. Aborting download.", stream.GetType());
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