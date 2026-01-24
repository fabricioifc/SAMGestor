using MediatR;

namespace SAMGestor.Application.Features.Reports.GetAvailableTemplates;

public sealed record GetAvailableTemplatesQuery(Guid RetreatId) 
    : IRequest<List<ReportTemplateInfoDto>>;