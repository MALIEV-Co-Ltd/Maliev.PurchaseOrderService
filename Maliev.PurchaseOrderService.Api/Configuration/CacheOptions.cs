namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for caching functionality
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Default cache expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Purchase order specific cache expiration in minutes
    /// </summary>
    public int PurchaseOrderCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Supplier data cache expiration in minutes
    /// </summary>
    public int SupplierCacheExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// Currency data cache expiration in minutes
    /// </summary>
    public int CurrencyCacheExpirationMinutes { get; set; } = 240;

    /// <summary>
    /// Order data cache expiration in minutes
    /// </summary>
    public int OrderCacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Document cache expiration in minutes
    /// </summary>
    public int DocumentCacheExpirationMinutes { get; set; } = 180;

    /// <summary>
    /// PDF generation cache expiration in minutes
    /// </summary>
    public int PdfCacheExpirationMinutes { get; set; } = 1440; // 24 hours

    /// <summary>
    /// Cache refresh interval for background refresh in minutes
    /// </summary>
    public int BackgroundRefreshIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum cache size in MB (0 = unlimited)
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 0;

    /// <summary>
    /// Enable cache compression
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Enable cache statistics collection
    /// </summary>
    public bool EnableStatistics { get; set; } = false;

    /// <summary>
    /// Gets the default expiration as TimeSpan
    /// </summary>
    public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);

    /// <summary>
    /// Gets the purchase order cache expiration as TimeSpan
    /// </summary>
    public TimeSpan PurchaseOrderCacheExpiration => TimeSpan.FromMinutes(PurchaseOrderCacheExpirationMinutes);

    /// <summary>
    /// Gets the supplier cache expiration as TimeSpan
    /// </summary>
    public TimeSpan SupplierCacheExpiration => TimeSpan.FromMinutes(SupplierCacheExpirationMinutes);

    /// <summary>
    /// Gets the currency cache expiration as TimeSpan
    /// </summary>
    public TimeSpan CurrencyCacheExpiration => TimeSpan.FromMinutes(CurrencyCacheExpirationMinutes);

    /// <summary>
    /// Gets the order cache expiration as TimeSpan
    /// </summary>
    public TimeSpan OrderCacheExpiration => TimeSpan.FromMinutes(OrderCacheExpirationMinutes);

    /// <summary>
    /// Gets the document cache expiration as TimeSpan
    /// </summary>
    public TimeSpan DocumentCacheExpiration => TimeSpan.FromMinutes(DocumentCacheExpirationMinutes);

    /// <summary>
    /// Gets the PDF cache expiration as TimeSpan
    /// </summary>
    public TimeSpan PdfCacheExpiration => TimeSpan.FromMinutes(PdfCacheExpirationMinutes);

    /// <summary>
    /// Gets the background refresh interval as TimeSpan
    /// </summary>
    public TimeSpan BackgroundRefreshInterval => TimeSpan.FromMinutes(BackgroundRefreshIntervalMinutes);
}