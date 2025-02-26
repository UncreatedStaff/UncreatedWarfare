using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Util;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Uses Senior S's audio converter web API for converting Steam Voice to .wav files.
/// </summary>
/// <remarks><see href="https://github.com/Senior-S/SVD-Example-Use/"/></remarks>
public class WebAudioConverter : IAudioConverter
{
    private readonly ILogger<WebAudioConverter> _logger;

    private readonly Uri? _authenticateUri;
    private readonly Uri? _decodeUri;
    private readonly System.Random _boundaryGenerator = new System.Random();

    private DateTime _lastAuthenticate = DateTime.MinValue;
    private string? _tokenStr;

    /// <inheritdoc />
    public bool Enabled => _authenticateUri != null;

    public WebAudioConverter(IConfiguration systemConfiguration, ILogger<WebAudioConverter> logger)
    {
        _logger = logger;
        IConfigurationSection section = systemConfiguration.GetSection("audio_recording");
        string? baseUriStr = section["base_uri"];
        string? username = section["username"];
        string? password = section["password"];

        if (baseUriStr != null && username != null && password != null)
        {
            username = Uri.EscapeDataString(username);
            password = Uri.EscapeDataString(password);

            Uri baseUri = new Uri(baseUriStr);

            _authenticateUri = new Uri(baseUri, $"authentication?username={username}&key={password}");
            _decodeUri = new Uri(baseUri, "decoder/form");
        }
        else
        {
            _logger.LogInformation("Voice recording disabled, missing credentials.");
        }
    }

    /// <inheritdoc />
    public async Task<AudioConvertResult> ConvertAsync(Stream output, bool leaveOpen, IEnumerable<ArraySegment<byte>> packets, float volume, CancellationToken token = default)
    {
        if (!Enabled)
        {
            if (!leaveOpen)
                output.Dispose();
            return AudioConvertResult.Disabled;
        }

        await UniTask.SwitchToMainThread(token);

        try
        {
            int authTries = 0;
            while (true)
            {
                if (_tokenStr == null || (DateTime.UtcNow - _lastAuthenticate).TotalMinutes > 23d)
                {
                    AudioConvertResult result = await TryAuthenticateAsync(token);
                    if (result != AudioConvertResult.Success)
                        return result;
                    ++authTries;
                }

                byte[] boundary = new byte[40];

                byte[] multipartData = CreateMultipartPacket(packets.ToArrayFast());

                Buffer.BlockCopy(multipartData, multipartData.Length - 42, boundary, 0, 40);

                using UnityWebRequest webRequest = new UnityWebRequest(_decodeUri, "POST");

                webRequest.SetRequestHeader("Authorization", _tokenStr);
                webRequest.uploadHandler = new UploadHandlerRaw(multipartData)
                {
                    contentType = "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary)
                };

                webRequest.downloadHandler = new UnityStreamDownloadHandler(output, leaveOpen: true);

                _logger.LogInformation(webRequest.url);
                _logger.LogInformation("Authorization: " + _tokenStr!);

                try
                {
                    await webRequest.SendWebRequest();
                }
                catch (UnityWebRequestException)
                {
                    // ignore
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    return AudioConvertResult.ConnectionError;
                }

                switch (webRequest.responseCode)
                {
                    case 400: // Bad Request
                        return AudioConvertResult.InvalidFormat;

                    case 401: // Unauthorized
                        _tokenStr = null;
                        if (authTries > 1)
                            return AudioConvertResult.Unauthorized;
                        continue;

                    case 200: // Success
                        return AudioConvertResult.Success;

                    default:
                        _logger.LogError("Unrecognized API response: {0}, {1}, \"{2}\".", webRequest.responseCode, webRequest.result, webRequest.error);
                        return AudioConvertResult.UnknownError;
                }
            }
        }
        finally
        {
            if (!leaveOpen)
            {
                try
                {
                    await output.FlushAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                output.Dispose();
            }
        }
    }

    private static string ReadJwt(byte[] utf8)
    {
        if (utf8[0] != (byte)'{')
            return System.Text.Encoding.UTF8.GetString(utf8);

        Utf8JsonReader reader = new Utf8JsonReader(utf8);
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName
                || !reader.GetString()!.Equals("token", StringComparison.InvariantCultureIgnoreCase)
                || !reader.Read())
            {
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Invalid token type for token: {reader.TokenType}.");

            return reader.GetString()!;
        }

        throw new JsonException("Missing property \"token\" from json.");
    }

    public async UniTask<AudioConvertResult> TryAuthenticateAsync(CancellationToken token = default)
    {
        const int tries = 3;

        for (int i = 0; i < tries; ++i)
        {
            using UnityWebRequest authWebReq = UnityWebRequest.Get(_authenticateUri);
            authWebReq.downloadHandler = new DownloadHandlerBuffer();

            _tokenStr = null;

            _logger.LogConditional(authWebReq.url);

            try
            {
                await authWebReq.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                // ignore
            }

            switch (authWebReq.result)
            {
                case UnityWebRequest.Result.Success:
                    string jwt;

                    _logger.LogConditional(authWebReq.downloadHandler.text);

                    try
                    {
                        jwt = ReadJwt(authWebReq.downloadHandler.data);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Invalid JWT json{0}{1}", Environment.NewLine, authWebReq.downloadHandler.text);
                        return AudioConvertResult.Unauthorized;
                    }
                    catch (ArgumentException ex) // includes SecurityTokenMalformedException
                    {
                        _logger.LogError(ex, "Invalid JWT{0}{1}", Environment.NewLine, authWebReq.downloadHandler.text);
                        return AudioConvertResult.Unauthorized;
                    }

                    _tokenStr = "Bearer " + jwt;
                    _lastAuthenticate = DateTime.UtcNow;
                    return AudioConvertResult.Success;

                case UnityWebRequest.Result.ConnectionError:
                    _logger.LogWarning("Failed to get auth token for audio conversion API. Try {0}/{1}.", i + 1, tries);
                    await UniTask.Delay(1000, cancellationToken: token);
                    break;

                default:
                    _logger.LogWarning("Failed to get auth token for audio conversion API. Result: {0}, {1}, \"{2}\".", authWebReq.result, authWebReq.responseCode, authWebReq.error);
                    return AudioConvertResult.Unauthorized;
            }
        }

        return AudioConvertResult.ConnectionError;
    }

    public byte[] CreateMultipartPacket(ArraySegment<byte>[] segments)
    {
        if (segments.Length == 0)
            return Array.Empty<byte>();

        ReadOnlySpan<byte> contentSize = "Content-Length: "u8;
        ReadOnlySpan<byte> newLine = "\r\n"u8;
        ReadOnlySpan<byte> doubleDash = "--"u8;
        ReadOnlySpan<byte> contentDisp = "Content-Disposition: form-data; name=\"packet_"u8;
        ReadOnlySpan<byte> fileName = "\"; filename=\""u8;
        ReadOnlySpan<byte> contentType = ".bin\"\r\nContent-Type: application/octet-stream\r\n"u8;

        Span<byte> boundary = stackalloc byte[40];
        for (int index = 0; index < 40; ++index)
        {
            int num = _boundaryGenerator.Next(48, 110);
            if (num > 57) num += 7;
            if (num > 90) num += 6;
            boundary[index] = (byte)num;
        }

        int size = (boundary.Length + 131) * segments.Length + /* end */ 6 + boundary.Length;
        int maxSize = 0;
        for (int i = 0; i < segments.Length; ++i)
        {
            int packetSize = segments[i].Count;
            size += packetSize + MathUtility.CountDigits(i) * 2 + MathUtility.CountDigits(packetSize);
            if (maxSize < packetSize)
                maxSize = packetSize;
        }

        byte[] resArr = new byte[size];
        Span<byte> result = resArr;
        int pos = 0;
        Span<byte> numUtf8 = stackalloc byte[Math.Max(MathUtility.CountDigits(segments.Length), MathUtility.CountDigits(maxSize))];
        for (int i = 0; i < segments.Length; ++i)
        {
            ReadOnlySpan<char> numStr = i.ToString(CultureInfo.InvariantCulture).AsSpan();

            for (int c = 0; c < numStr.Length; ++c)
                numUtf8[c] = (byte)numStr[c];

            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            doubleDash.CopyTo(result.Slice(pos));
            pos += doubleDash.Length;
            boundary.CopyTo(result.Slice(pos));
            pos += boundary.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            contentDisp.CopyTo(result.Slice(pos));
            pos += contentDisp.Length;
            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            fileName.CopyTo(result.Slice(pos));
            pos += fileName.Length;
            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            contentType.CopyTo(result.Slice(pos));
            pos += contentType.Length;
            contentSize.CopyTo(result.Slice(pos));
            pos += contentSize.Length;
            
            ReadOnlySpan<byte> segment = segments[i];

            int packetSize = segment.Length;

            numStr = packetSize.ToString(CultureInfo.InvariantCulture).AsSpan();

            for (int c = 0; c < numStr.Length; ++c)
                numUtf8[c] = (byte)numStr[c];

            numUtf8.Slice(0, numStr.Length).CopyTo(result.Slice(pos));
            pos += numStr.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;
            newLine.CopyTo(result.Slice(pos));
            pos += newLine.Length;

            segment.CopyTo(result.Slice(pos));
            pos += packetSize;
        }

        newLine.CopyTo(result.Slice(pos));
        pos += newLine.Length;
        doubleDash.CopyTo(result.Slice(pos));
        pos += doubleDash.Length;
        boundary.CopyTo(result.Slice(pos));
        pos += boundary.Length;
        doubleDash.CopyTo(result.Slice(pos));
#if DEBUG
        if (pos + doubleDash.Length != resArr.Length)
            _logger.LogWarning("Multipart data not equal lengths: {0} instead of {1}.", pos, resArr.Length);
#endif
        return resArr;
    }
}
