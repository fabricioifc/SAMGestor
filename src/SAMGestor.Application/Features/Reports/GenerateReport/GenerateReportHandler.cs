using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Reports.GenerateReport;

public sealed class GenerateReportHandler 
    : IRequestHandler<GenerateReportQuery, ReportPayload>
{
    private readonly IReportTemplateRegistry _registry;
    private readonly IRetreatRepository _retreatRepository;

    public GenerateReportHandler(
        IReportTemplateRegistry registry,
        IRetreatRepository retreatRepository)
    {
        _registry = registry;
        _retreatRepository = retreatRepository;
    }

    public async Task<ReportPayload> Handle(
        GenerateReportQuery query,
        CancellationToken ct)
    {

        var retreat = await _retreatRepository.GetByIdAsync(query.RetreatId);
        if (retreat == null)
            throw new KeyNotFoundException($"Retiro {query.RetreatId} não encontrado.");

        var template = _registry.GetTemplate(query.TemplateKey);
        if (template == null)
            throw new KeyNotFoundException($"Template '{query.TemplateKey}' não encontrado.");

        var context = new ReportContext(
            TemplateKey: query.TemplateKey,
            RetreatId: query.RetreatId,
            RetreatName: retreat.Name,
            Page: query.Page,
            PageSize: query.PageSize
        );


        var skip = (query.Page - 1) * query.PageSize;
        var payload = await template.GetDataAsync(context, skip, query.PageSize, ct);

        return payload;
    }

}