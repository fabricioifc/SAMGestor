namespace SAMGestor.Application.Features.Reports.ExportReport;

public sealed record ExportReportResponse(
    string ContentType,
    string FileName,
    byte[] Bytes
);