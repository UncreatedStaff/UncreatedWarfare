using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Uncreated.Warfare.Networking.Purchasing;
public interface IStripeService
{
    IStripeClient StripeClient { get; }
    ProductService ProductService { get; }
    SemaphoreSlim Semaphore { get; }
    CultureInfo PriceFormatting { get; }
    bool Available { get; }
}
public class StripeService : IStripeService, IDisposable
{
    public const string TaxCode = "txcd_10000000";
    internal static readonly List<string> ProductExpandOptions = new List<string>
    {
        "data.default_price", "data.tax_code"
    };
    public IStripeClient StripeClient { get; }
    public ProductService ProductService { get; }
    public SemaphoreSlim Semaphore { get; }
    public CultureInfo PriceFormatting { get; }
    public bool Available => StripeClient != null;
    public StripeService() : this(StripeConfiguration.StripeClient) { }
    public StripeService(IStripeClient client)
    {
        StripeClient = client;
        ProductService = new ProductService(StripeClient);
        Semaphore = new SemaphoreSlim(1, 1);
        PriceFormatting = new CultureInfo("en-US");
    }
    public void Dispose()
    {
        if (StripeClient is IDisposable disp)
            disp.Dispose();
        Semaphore.Dispose();
    }
}
#if NETSTANDARD || NETFRAMEWORK
public class WarfareStripeService : IStripeService
{
    public IStripeClient StripeClient { get; }
    public ProductService ProductService { get; }
    public SemaphoreSlim Semaphore { get; }
    public CultureInfo PriceFormatting { get; }
    public bool Available => StripeClient != null;

    public WarfareStripeService()
    {
        if (UCWarfare.Config.StripeAPIKey == null)
            return;

        StripeClient = new StripeClient(UCWarfare.Config.StripeAPIKey, httpClient: new UnityWebRequestsHttpClient(), clientId: $"Uncreated Warfare/{UCWarfare.Version}");
        ProductService = new ProductService(StripeClient);
        Semaphore = new SemaphoreSlim(1, 1);
        PriceFormatting = new CultureInfo("en-US");
    }
    public void Dispose()
    {
        if (StripeClient is IDisposable disp)
            disp.Dispose();
        Semaphore.Dispose();
    }
}
#endif