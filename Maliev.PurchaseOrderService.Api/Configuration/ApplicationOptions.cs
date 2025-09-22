namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for application settings
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    public string ServiceName { get; set; } = "PurchaseOrderService";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Production";
}



