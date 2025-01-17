using System;

namespace Uncreated.Warfare.Util;

public static class CancellationTokenExtensions
{
    [MustUseReturnValue("CombinedTokenSources must be disposed.")]
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other)
    {
        if (token.CanBeCanceled)
        {
            if (!other.CanBeCanceled)
                return new CombinedTokenSources(token, null);

            if (token == other)
                return new CombinedTokenSources(token, null);

            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(token, other);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }

        if (!other.CanBeCanceled)
            return default;

        token = other;
        return new CombinedTokenSources(other, null);
    }

    [MustUseReturnValue("CombinedTokenSources must be disposed.")]
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other1, CancellationToken other2)
    {
        if (other1 == other2 || token == other2)
            return token.CombineTokensIfNeeded(other1);

        if (other1 == token)
            return token.CombineTokensIfNeeded(other2);

        CancellationTokenSource src;
        if (token.CanBeCanceled)
        {
            if (other1.CanBeCanceled)
            {
                if (other2.CanBeCanceled)
                {
                    src = CancellationTokenSource.CreateLinkedTokenSource(token, other1, other2);
                    token = src.Token;
                    return new CombinedTokenSources(token, src);
                }

                src = CancellationTokenSource.CreateLinkedTokenSource(token, other1);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            if (!other2.CanBeCanceled)
                return new CombinedTokenSources(token, null);

            src = CancellationTokenSource.CreateLinkedTokenSource(token, other2);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }

        if (other1.CanBeCanceled)
        {
            if (other2.CanBeCanceled)
            {
                src = CancellationTokenSource.CreateLinkedTokenSource(other1, other2);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            token = other1;
            return new CombinedTokenSources(other1, null);
        }

        if (!other2.CanBeCanceled)
            return default;

        token = other2;
        return new CombinedTokenSources(token, null);

    }
}

public readonly struct CombinedTokenSources : IDisposable
{
    private readonly CancellationTokenSource? _tknSrc;
    public readonly CancellationToken Token;
    internal CombinedTokenSources(CancellationToken token, CancellationTokenSource? tknSrc)
    {
        Token = token;
        _tknSrc = tknSrc;
    }
    public void Dispose()
    {
        _tknSrc?.Dispose();
    }
}