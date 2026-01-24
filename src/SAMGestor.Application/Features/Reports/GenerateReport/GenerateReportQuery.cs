using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.GenerateReport;

public sealed record GenerateReportQuery(
    Guid RetreatId,
    string TemplateKey,
    int Page = 1,
    int PageSize = 50
) : IRequest<ReportPayload>;