namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Simple PDF DTO for basic operations
/// </summary>
public class PdfDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF Generation Result
/// </summary>
public class PdfGenerationResultDto : IDisposable
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream? PdfContent { get; set; }
    public long FileSize { get; set; }
    public int PageCount { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public TimeSpan GenerationTime { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public void Dispose()
    {
        PdfContent?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for PDF Generation Options
/// </summary>
public class PdfGenerationOptionsDto
{
    public string PageSize { get; set; } = "A4";
    public string Orientation { get; set; } = "Portrait";
    public PdfMarginDto Margins { get; set; } = new();
    public bool PrintBackground { get; set; } = true;
    public bool PreferCSSPageSize { get; set; } = false;
    public double Scale { get; set; } = 1.0;
    public string Format { get; set; } = "PDF";
    public bool DisplayHeaderFooter { get; set; } = false;
    public string HeaderTemplate { get; set; } = string.Empty;
    public string FooterTemplate { get; set; } = string.Empty;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public int Timeout { get; set; } = 30000; // milliseconds
}

/// <summary>
/// Data Transfer Object for PDF Margins
/// </summary>
public class PdfMarginDto
{
    public string Top { get; set; } = "1cm";
    public string Right { get; set; } = "1cm";
    public string Bottom { get; set; } = "1cm";
    public string Left { get; set; } = "1cm";
}

/// <summary>
/// Data Transfer Object for PDF Merge Result
/// </summary>
public class PdfMergeResultDto : IDisposable
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream? MergedPdfContent { get; set; }
    public long FileSize { get; set; }
    public int TotalPages { get; set; }
    public int InputFileCount { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; }
    public TimeSpan MergeTime { get; set; }

    public void Dispose()
    {
        MergedPdfContent?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for PDF Merge Options
/// </summary>
public class PdfMergeOptionsDto
{
    public bool AddBookmarks { get; set; } = true;
    public bool OptimizeSize { get; set; } = true;
    public string OutputFileName { get; set; } = "merged.pdf";
    public List<PdfPageRange> PageRanges { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for PDF Page Range
/// </summary>
public class PdfPageRange
{
    public int FileIndex { get; set; }
    public int StartPage { get; set; } = 1;
    public int? EndPage { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF File Input
/// </summary>
public class PdfFileInput : IDisposable
{
    public string FileName { get; set; } = string.Empty;
    public Stream Content { get; set; } = Stream.Null;
    public string Password { get; set; } = string.Empty;
    public List<int> PageNumbers { get; set; } = new();

    public void Dispose()
    {
        Content?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for PDF to Image Result
/// </summary>
public class PdfToImageResultDto : IDisposable
{
    public string ConversionId { get; set; } = string.Empty;
    public List<PdfPageImageDto> PageImages { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ConvertedAt { get; set; }
    public TimeSpan ConversionTime { get; set; }

    public void Dispose()
    {
        foreach (var image in PageImages)
        {
            image?.Dispose();
        }
    }
}

/// <summary>
/// Data Transfer Object for PDF Page Image
/// </summary>
public class PdfPageImageDto : IDisposable
{
    public int PageNumber { get; set; }
    public string ImageFormat { get; set; } = "PNG";
    public Stream? ImageContent { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public void Dispose()
    {
        ImageContent?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for PDF to Image Options
/// </summary>
public class PdfToImageOptionsDto
{
    public string ImageFormat { get; set; } = "PNG";
    public int DPI { get; set; } = 300;
    public int Quality { get; set; } = 100;
    public List<int> PageNumbers { get; set; } = new();
    public int MaxWidth { get; set; } = 0;
    public int MaxHeight { get; set; } = 0;
    public bool TransparentBackground { get; set; } = false;
}

/// <summary>
/// Data Transfer Object for PDF Text Extraction Result
/// </summary>
public class PdfTextExtractionResultDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public List<PdfPageTextDto> PageTexts { get; set; } = new();
    public int TotalPages { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ExtractedAt { get; set; }
    public TimeSpan ExtractionTime { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF Page Text
/// </summary>
public class PdfPageTextDto
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<PdfTextBlockDto> TextBlocks { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for PDF Text Block
/// </summary>
public class PdfTextBlockDto
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string FontName { get; set; } = string.Empty;
    public double FontSize { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF Text Extraction Options
/// </summary>
public class PdfTextExtractionOptionsDto
{
    public List<int> PageNumbers { get; set; } = new();
    public bool PreserveLayout { get; set; } = true;
    public bool ExtractImages { get; set; } = false;
    public bool ExtractMetadata { get; set; } = true;
    public string Language { get; set; } = "en";
}

/// <summary>
/// Data Transfer Object for PDF Watermark Result
/// </summary>
public class PdfWatermarkResultDto : IDisposable
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream? WatermarkedPdfContent { get; set; }
    public long FileSize { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }

    public void Dispose()
    {
        WatermarkedPdfContent?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for PDF Watermark Options
/// </summary>
public class PdfWatermarkOptionsDto
{
    public string Text { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public Stream? ImageStream { get; set; }
    public double Opacity { get; set; } = 0.5;
    public string Position { get; set; } = "Center";
    public double Rotation { get; set; } = 0;
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 48;
    public string Color { get; set; } = "#808080";
    public List<int> PageNumbers { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for PDF Information
/// </summary>
public class PdfInfoDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Producer { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime ModificationDate { get; set; }
    public int PageCount { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public bool IsPasswordProtected { get; set; }
    public long FileSize { get; set; }
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
    public List<PdfPageInfoDto> Pages { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for PDF Page Information
/// </summary>
public class PdfPageInfoDto
{
    public int PageNumber { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Orientation { get; set; } = string.Empty;
    public int Rotation { get; set; }
    public bool HasImages { get; set; }
    public bool HasForms { get; set; }
    public int TextLength { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF Validation Result
/// </summary>
public class PdfValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string PdfVersion { get; set; } = string.Empty;
    public bool IsCorrupted { get; set; }
    public bool IsPasswordProtected { get; set; }
    public bool HasForms { get; set; }
    public bool HasDigitalSignatures { get; set; }
    public int PageCount { get; set; }
    public long FileSize { get; set; }
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Data Transfer Object for PDF Validation Options
/// </summary>
public class PdfValidationOptionsDto
{
    public bool CheckStructure { get; set; } = true;
    public bool CheckContent { get; set; } = true;
    public bool CheckMetadata { get; set; } = true;
    public bool CheckSecurity { get; set; } = true;
    public bool CheckCompliance { get; set; } = false;
    public string ComplianceStandard { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for PDF Template
/// </summary>
public class PdfTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<string> RequiredFields { get; set; } = new();
    public List<string> OptionalFields { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for PDF Template Download
/// </summary>
public class PdfTemplateDownloadDto : IDisposable
{
    public string TemplateId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream? TemplateContent { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime DownloadedAt { get; set; }

    public void Dispose()
    {
        TemplateContent?.Dispose();
    }
}