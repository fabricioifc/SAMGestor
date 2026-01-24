using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Reports.ExportReport;

public sealed class ExportReportHandler 
    : IRequestHandler<ExportReportCommand, ExportReportResponse>
{
    private readonly IReportTemplateRegistry _registry;
    private readonly IReportExporter _exporter;
    private readonly IRetreatRepository _retreatRepository;

    public ExportReportHandler(
        IReportTemplateRegistry registry,
        IReportExporter exporter,
        IRetreatRepository retreatRepository)
    {
        _registry = registry;
        _exporter = exporter;
        _retreatRepository = retreatRepository;
    }

    public async Task<ExportReportResponse> Handle(
        ExportReportCommand command, 
        CancellationToken ct)
    {
        var retreat = await _retreatRepository.GetByIdAsync(command.RetreatId);
        if (retreat == null)
            throw new KeyNotFoundException($"Retiro {command.RetreatId} não encontrado.");
        
        var validFormats = new[] { "csv", "pdf", "xlsx" };
        if (!validFormats.Contains(command.Format.ToLowerInvariant()))
            throw new ArgumentException($"Formato inválido. Use: {string.Join(", ", validFormats)}");
        
        var template = _registry.GetTemplate(command.TemplateKey);
        if (template == null)
            throw new KeyNotFoundException($"Template '{command.TemplateKey}' não encontrado.");
        
        var context = new ReportContext(
            TemplateKey: command.TemplateKey,
            RetreatId: command.RetreatId,
            RetreatName: retreat.Name,
            Page: command.Page,
            PageSize: command.PageSize
        );

        var payload = await template.GetDataAsync(context, command.Page, command.PageSize, ct);
        
        var (contentType, fileName, bytes) = await _exporter.ExportAsync(
            payload,
            command.Format,
            fileNameBase: $"{retreat.Name}_{template.DefaultTitle}",
            ct
        );

        return new ExportReportResponse(contentType, fileName, bytes);
    }
}
