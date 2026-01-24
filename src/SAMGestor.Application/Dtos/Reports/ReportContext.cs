namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportContext(
    string TemplateKey,       
    Guid RetreatId,           
    string RetreatName,
    int Page,
    int PageSize,
    Dictionary<string, object>? Filters = null 
);