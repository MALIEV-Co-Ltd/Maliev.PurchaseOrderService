namespace Maliev.PurchaseOrderService.Api.DTOs;

public class ErrorResponse
{
    public ErrorInfo Error { get; set; } = new();
}

public class ErrorInfo
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorDetail> Details { get; set; } = new();
    public string? TraceId { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}