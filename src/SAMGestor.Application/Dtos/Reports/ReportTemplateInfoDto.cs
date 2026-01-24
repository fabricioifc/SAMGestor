public sealed record ReportTemplateInfoDto(
    string Key,
    string Title,
    string Description,
    string Category,         
    bool HasData,             
    int? EstimatedRecords    
);