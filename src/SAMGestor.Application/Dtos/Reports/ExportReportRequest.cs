public sealed record ExportReportRequest(
    string TemplateKey,
    Guid RetreatId,
    string Format,          
    int Page = 1,
    int PageSize = 1000     
);