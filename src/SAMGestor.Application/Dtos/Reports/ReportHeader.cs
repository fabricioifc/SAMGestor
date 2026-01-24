namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportHeader(
    string TemplateKey,      
    string Title,           
    string Category,         
    DateTime GeneratedAt,   
    Guid? RetreatId,       
    string? RetreatName        
);