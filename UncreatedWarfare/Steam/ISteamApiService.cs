using System;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Steam;
public interface ISteamApiService
{
    /// <exception cref="SteamApiRequestException"/>
    Task<TResponse> ExecuteQueryAsync<TResponse>(SteamApiQuery query, CancellationToken token) where TResponse : notnull;
}

public class SteamApiRequestException : Exception
{
    /// <summary>
    /// Is this an error from the API, as oppose to an error caused by failing to access the API.
    /// </summary>
    /// <remarks>This includes errors like unauthorized access to a player's private profile.</remarks>
    public bool IsApiResponseError { get; internal set; }

    public SteamApiRequestException(string message) : base(message) { }
    public SteamApiRequestException(string message, Exception innerException) : base(message, innerException) { }
}

public readonly struct SteamApiQuery
{
    public readonly string Interface;
    public readonly string Method;
    public readonly int Version;
    public readonly string? QueryString;
    public readonly float StartTimeout = 1f;

    public SteamApiQuery(string @interface, string method, int version, string? queryString)
    {
        Interface = @interface;
        Method = method;
        Version = version;
        QueryString = queryString;
    }

    public string CreateUrl(string apiKey)
    {
        int vLen = MathUtility.CountDigits(Version);
        int stringLen = SteamApiServiceExtensions.BaseSteamApiUrl.Length + Interface.Length + 1 + Method.Length + 2 + vLen + 5 + apiKey.Length;
        if (QueryString != null)
            stringLen += 1 + QueryString.Length;

        CreateUrlState state = default;
        state.APIKey = apiKey;
        state.Version = Version;
        state.Method = Method;
        state.Interface = Interface;
        state.VersionLength = vLen;
        state.Query = QueryString;

        return string.Create(stringLen, state, (span, state) =>
        {
            int index = 0;
            SteamApiServiceExtensions.BaseSteamApiUrl.AsSpan().CopyTo(span);
            index += SteamApiServiceExtensions.BaseSteamApiUrl.Length;

            state.Interface.AsSpan().CopyTo(span[index..]);
            index += state.Interface.Length;

            span[index++] = '/';

            state.Method.AsSpan().CopyTo(span[index..]);
            index += state.Method.Length;

            span[index++] = '/';
            span[index++] = 'v';

            state.Version.TryFormat(span[index..], out _, "D", CultureInfo.InvariantCulture);
            index += state.VersionLength;

            span[index++] = '?';
            span[index++] = 'k';
            span[index++] = 'e';
            span[index++] = 'y';
            span[index++] = '=';

            state.APIKey.AsSpan().CopyTo(span[index..]);
            index += state.APIKey.Length;

            if (state.Query == null)
                return;

            span[index++] = '&';
            state.Query.AsSpan().CopyTo(span[index..]);
        });
    }
    private struct CreateUrlState
    {
        public string Interface;
        public string Method;
        public string? Query;
        public string APIKey;
        public int Version;
        public int VersionLength;
    }

    public override string ToString()
    {
        return $"steam://{Interface}/{Method}/v{Version.ToString(CultureInfo.InvariantCulture)}";
    }
}