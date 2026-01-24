using MediatR;

namespace SAMGestor.Application.Features.Reports.ExportReport;

public sealed record ExportReportCommand(
    Guid RetreatId,
    string TemplateKey,
    string Format,
    int Page = 1,
    int PageSize = 10000
) : IRequest<ExportReportResponse>;