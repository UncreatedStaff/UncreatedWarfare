#if NETSTANDARD || NETFRAMEWORK
#define TELEMETRY
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.Networking;
using Uncreated.Warfare.Logging;

#if TELEMETRY
using System.Net.Http;
#endif

namespace Uncreated.Warfare.Networking.Purchasing;

// this is just a reinterpretation of the HttpClient implementation that can be found here, just with UnityWebRequests and UniTask instead:
//  https://github.com/stripe/stripe-dotnet/blob/master/src/Stripe.net/Infrastructure/Public/SystemNetHttpClient.cs
internal class UnityWebRequestsHttpClient : IHttpClient
{
    private const int TimeoutSeconds = 80;
    private const int MaxRetryDelayMs = 5000;
    private const int MinRetryDelayMs = 500;
    private const int MaxRetries = 2;
#if TELEMETRY
    private readonly RequestTelemetry _requestTelemetry = new RequestTelemetry();
    private readonly Stopwatch _stopwatch = new Stopwatch();
#endif
    private static string? _clientUserAgent;
    private static string? _userAgent;
    private async UniTask<StripeResponse> MakeRequestAsyncImpl(StripeRequest request, CancellationToken token)
    {
        IntlStripeResponse response = await SendHttpRequest(request, token);
        HttpResponseHeaders headerList = ConvertHeaders(in response);
        
        StripeResponse resp = new StripeResponse((HttpStatusCode)response.Request.responseCode, headerList, response.Request.downloadHandler.text);
        response.Request.Dispose();
        return resp;
    }
    private async UniTask<StripeStreamedResponse> MakeStreamingRequestAsyncImpl(StripeRequest request, CancellationToken token)
    {
        IntlStripeResponse response = await SendHttpRequest(request, token);
        HttpResponseHeaders headerList = ConvertHeaders(in response);

        MemoryStream stream = new MemoryStream(response.Request.downloadHandler.data);
        StripeStreamedResponse resp = new StripeStreamedResponse((HttpStatusCode)response.Request.responseCode, headerList, stream);
        response.Request.Dispose();
        return resp;
    }
    private static HttpResponseHeaders ConvertHeaders(in IntlStripeResponse response)
    {
        HttpResponseHeaders headerList = (HttpResponseHeaders)Activator.CreateInstance(typeof(HttpResponseHeaders),
            BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            Array.Empty<object>(), CultureInfo.InvariantCulture, null);

        Dictionary<string, string> headers = response.CachedHeaders;
        foreach (KeyValuePair<string, string> pair in headers)
        {
            headerList.TryAddWithoutValidation(pair.Key, pair.Value);
        }

        return headerList;
    }
    private async UniTask<IntlStripeResponse> SendHttpRequest(StripeRequest request, CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);
        
        int numRetries = 0;
#if TELEMETRY
        _requestTelemetry.MaybeAddTelemetryHeader(request.StripeHeaders);
#endif

        while (true)
        {
            Exception? requestException = null;

            if (request.Content != null)
            {
                Task t = request.Content.LoadIntoBufferAsync();
                if (!t.IsCompleted)
                {
                    await t.ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);
                }
            }

            UnityWebRequest req = BuildRequest(request);
            
#if TELEMETRY
            _stopwatch.Restart();
#endif
            bool isActualError = false;
            try
            {
                // UniTask will throw a UnityWebRequestException if it fails

                await req.SendWebRequest().WithCancellation(token);
            }
            catch (UnityWebRequestException ex)
            {
                requestException = TryParseError(req, ex, out isActualError);

                if (isActualError)
                {
                    L.LogError($"Stripe API call failed: {ex.Message}");
                    string? text = req.downloadHandler?.text;
                    if (text != null)
                    {
                        L.LogError($"Response: {Environment.NewLine}" + text);
                    }
                }
            }

            ThreadUtil.assertIsGameThread();

#if TELEMETRY
            _stopwatch.Stop();
            TimeSpan elapsed = _stopwatch.Elapsed;
#endif

            Dictionary<string, string>? headers = req.GetResponseHeaders();
            if (isActualError || !ShouldRetry(numRetries, requestException != null, req, headers))
            {
                if (requestException != null)
                {
                    req.Dispose();
                    throw requestException;
                }
#if TELEMETRY
                if (headers.TryGetValue("Request-Id", out string reqId))
                {
                    HttpResponseMessage tempMessage = new HttpResponseMessage((HttpStatusCode)req.responseCode);
                    tempMessage.Headers.TryAddWithoutValidation("Request-Id", reqId);

                    _requestTelemetry.MaybeEnqueueMetrics(tempMessage, elapsed);
                }
#endif
                return new IntlStripeResponse(req, headers, numRetries);
            }

            req.Dispose();

            ++numRetries;

            int msSleepTime = MinRetryDelayMs * (int)Math.Pow(2, numRetries - 1);
            if (msSleepTime > MaxRetryDelayMs)
                msSleepTime = MaxRetryDelayMs;
            msSleepTime = (int)Math.Round(msSleepTime * ((3d + UnityEngine.Random.value) / 4d));
            if (msSleepTime < MinRetryDelayMs)
                msSleepTime = MinRetryDelayMs;

            await UniTask.Delay(msSleepTime, true, cancellationToken: token);
        }
    }
    private static bool ShouldRetry(int numRetries, bool error, UnityWebRequest req, IReadOnlyDictionary<string, string> headers)
    {
        if (numRetries >= MaxRetries)
        {
            return false;
        }

        if (error)
            return true;

        if (headers != null && headers.TryGetValue("Stripe-Should-Retry", out string shouldRetry))
        {
            if ("true".Equals(shouldRetry, StringComparison.OrdinalIgnoreCase))
                return true;
            if ("false".Equals(shouldRetry, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return req.responseCode is 409 or >= 500;
    }
    private static string BuildUserAgentString()
    {
        string userAgent = $"Stripe/v1 .NetBindings/{StripeConfiguration.StripeNetVersion} Uncreated Warfare/{UCWarfare.Version}";

        if (UCWarfare.Config.WebsiteUri != null)
            userAgent += " (" + UCWarfare.Config.WebsiteUri.OriginalString + ")";

        return userAgent;
    }
    private static string BuildStripeClientUserAgentString()
    {
        StringBuilder sb = new StringBuilder("{");
        sb.Append("\"bindings_version\":\"")
            .Append(StripeConfiguration.StripeNetVersion)
            .Append("\",\"lang\":\".net")
            .Append("\",\"publisher\":\"stripe\"");
        
        Type type = typeof(RuntimeInformation);
        try
        {
            sb.Append($"\",\"lang_version\":\"{type.GetMethod("GetRuntimeVersion", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, Array.Empty<object>()) ?? "Unturned (Unity)"}\"");
        }
        catch (Exception)
        {
            sb.Append("\",\"lang_version\":\"(unknown)\"");
        }

        try
        {
            sb.Append($"\",\"os_version\":\"{RuntimeInformation.OSDescription}\"");
        }
        catch (Exception)
        {
            sb.Append("\",\"os_version\":\"(unknown)\"");
        }

        try
        {
            Assembly? assembly = Assembly.GetAssembly(typeof(JsonConvert));
            FileVersionInfo? fileVersion = assembly == null ? null : FileVersionInfo.GetVersionInfo(assembly.Location);
            sb.Append($"\",\"newtonsoft_json_version\":\"{fileVersion?.FileVersion ?? "(unknown)"}\"");
        }
        catch (Exception)
        {
            sb.Append("\",\"newtonsoft_json_version\":\"(unknown)\"");
        }

        string str = $"Uncreated Warfare/{UCWarfare.Version}";
        if (UCWarfare.Config.WebsiteUri != null)
            str += " (" + UCWarfare.Config.WebsiteUri.OriginalString + ")";
        sb.Append($"\",\"application\":\"{str}\"");

        sb.Append('}');
        return sb.ToString();
    }
    private static Exception TryParseError(UnityWebRequest req, UnityWebRequestException ex, out bool isActualError)
    {
        isActualError = false;
        string? text = req.downloadHandler?.text;
        if (text is not { Length: > 0 })
            return ex;

        try
        {
            StripeErrorResponse error = JsonConvert.DeserializeObject<StripeErrorResponse>(text);

            if (error.Error != null)
            {
                string? message = error.Error.Message;
                if (string.IsNullOrWhiteSpace(message))
                    message = !string.IsNullOrWhiteSpace(error.Error.Type) ? ("Type: " + error.Error.Type) : (req.error ?? req.result.ToString());
                else if (!string.IsNullOrWhiteSpace(error.Error.Type))
                    message = "Type: " + error.Error.Type + " | Message: " + message;

                return new StripeException(message, ex)
                {
                    StripeError = error.Error,
                    HttpStatusCode = (HttpStatusCode)req.responseCode,
                    HelpLink = error.Error.DocUrl ?? error.Error.RequestLogUrl
                };
            }
        }
        catch
        {
            // ignored
        }

        isActualError = false;
        return ex;
    }
    private static UnityWebRequest BuildRequest(StripeRequest request)
    {
        _userAgent ??= BuildUserAgentString();
        _clientUserAgent ??= BuildStripeClientUserAgentString();

        UnityWebRequest requestMessage;
        if (request.Content != null)
        {
            Task<byte[]> task = request.Content.ReadAsByteArrayAsync();
            task.Wait(); // this isn't a mortal sin because it's buffered before this function is called.
            byte[] bytes = task.Result;
            requestMessage = new UnityWebRequest(request.Uri, request.Method.ToString(), new DownloadHandlerBuffer(), new UploadHandlerRaw(bytes));
            requestMessage.uploadHandler.contentType = request.Content.Headers.ContentType.MediaType;
            requestMessage.SetRequestHeader("Content-Type", request.Content.Headers.ContentType.MediaType);
        }
        else
        {
            requestMessage = new UnityWebRequest(request.Uri, request.Method.ToString(), new DownloadHandlerBuffer(), null);
        }
        
        requestMessage.timeout = TimeoutSeconds;

        requestMessage.SetRequestHeader("Accept", "application/json");
        requestMessage.SetRequestHeader("User-Agent", _userAgent);
        requestMessage.SetRequestHeader("Authorization", request.AuthorizationHeader.ToString());
        requestMessage.SetRequestHeader("X-Stripe-Client-User-Agent", _clientUserAgent);

        foreach (KeyValuePair<string, string> header in request.StripeHeaders)
        {
            requestMessage.SetRequestHeader(header.Key, header.Value);
        }
        
        return requestMessage;
    }

    Task<StripeResponse> IHttpClient.MakeRequestAsync(StripeRequest request, CancellationToken token) => MakeRequestAsyncImpl(request, token).AsTask();
    Task<StripeStreamedResponse> IHttpClient.MakeStreamingRequestAsync(StripeRequest request, CancellationToken token) => MakeStreamingRequestAsyncImpl(request, token).AsTask();

    private struct IntlStripeResponse
    {
        public readonly UnityWebRequest Request;
        public readonly Dictionary<string, string> CachedHeaders;
        public readonly int Retries;
        public IntlStripeResponse(UnityWebRequest request, Dictionary<string, string> cachedHeaders, int retries)
        {
            Request = request;
            CachedHeaders = cachedHeaders;
            Retries = retries;
        }
    }
    private readonly struct StripeErrorResponse
    {
        [JsonProperty("error")]
        public StripeError Error { get; }

        [JsonConstructor]
        public StripeErrorResponse(StripeError error)
        {
            Error = error;
        }
    }
}
#endif