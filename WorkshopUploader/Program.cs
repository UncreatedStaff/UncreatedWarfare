using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Responsible for uploading mods as a separate program.
/// <para>
/// This is necessary because of an issue with Pty.NET and Mono, so this program needs to run in .NET:
/// <see href="https://github.com/microsoft/vs-pty.net/issues/33"/>.
/// </para>
/// </summary>
internal static class Program
{
    private static CancellationTokenSource? _cancelToken;
    
    private static async Task<int> Main(string[] args)
    {
        WorkshopUploadParameters parameters;
        
        using (FileStream fs = new FileStream(string.Join(' ', args), FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan))
        {
            parameters = (WorkshopUploadParameters)JsonSerializer.Deserialize(fs, typeof(WorkshopUploadParameters), WorkshopUploadParametersContext.Default)!;
        }

        _cancelToken = new CancellationTokenSource();

        Console.CancelKeyPress += Cancel;

        ulong? id = null;
        try
        {
            id = await WorkshopUploader.UploadMod(parameters, _cancelToken.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Console.CancelKeyPress -= Cancel;
            _cancelToken.Dispose();
            _cancelToken = null;
        }

        if (!id.HasValue)
        {
            return 2;
        }

        Console.WriteLine($"Complete. Mod ID: {id}");
        return 0;
    }

    private static void Cancel(object? sender, ConsoleCancelEventArgs args)
    {
        _cancelToken?.Cancel();
        args.Cancel = true;
    }
}

[JsonSerializable(typeof(WorkshopUploadParameters), GenerationMode = JsonSourceGenerationMode.Metadata)]
internal partial class WorkshopUploadParametersContext : JsonSerializerContext;