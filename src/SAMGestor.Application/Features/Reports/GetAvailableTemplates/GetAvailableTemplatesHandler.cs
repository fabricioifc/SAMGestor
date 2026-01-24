// Application/Reports/GetAvailableTemplates/GetAvailableTemplatesHandler.cs

using MediatR;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Reports.GetAvailableTemplates;

public sealed class GetAvailableTemplatesHandler 
    : IRequestHandler<GetAvailableTemplatesQuery, List<ReportTemplateInfoDto>>
{
    private readonly IReportTemplateRegistry _registry;
    private readonly IReportingReadDb _readDb;
    private readonly IRetreatRepository _retreatRepository;

    public GetAvailableTemplatesHandler(
        IReportTemplateRegistry registry,
        IReportingReadDb readDb,
        IRetreatRepository retreatRepository)
    {
        _registry = registry;
        _readDb = readDb;
        _retreatRepository = retreatRepository;
    }

    public async Task<List<ReportTemplateInfoDto>> Handle(
        GetAvailableTemplatesQuery query,
        CancellationToken ct)
    {
        var retreat = await _retreatRepository.GetByIdAsync(query.RetreatId);
        if (retreat == null)
            throw new KeyNotFoundException($"Retiro {query.RetreatId} não encontrado.");

        var templates = _registry.GetAllTemplates();
        var result = new List<ReportTemplateInfoDto>();

        foreach (var template in templates)
        {
            var info = await BuildTemplateInfoAsync(template, query.RetreatId, ct);
            result.Add(info);
        }

        return result;
    }

    private async Task<ReportTemplateInfoDto> BuildTemplateInfoAsync(
        IReportTemplate template,
        Guid retreatId,
        CancellationToken ct)
    {
        var hasData = await CheckHasDataAsync(template.Key, retreatId, ct);
        var estimatedCount = hasData 
            ? await GetEstimatedCountAsync(template.Key, retreatId, ct) 
            : 0;

        return new ReportTemplateInfoDto(
            Key: template.Key,
            Title: template.DefaultTitle,
            Description: GetTemplateDescription(template.Key),
            Category: GetTemplateCategory(template.Key),
            HasData: hasData,
            EstimatedRecords: estimatedCount
        );
    }

    private async Task<bool> CheckHasDataAsync(string templateKey, Guid retreatId, CancellationToken ct)
    {
        return templateKey switch
        {
            "contemplated-participants" => 
                await _readDb.AnyAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            "contemplated-by-family" => 
                await _readDb.AnyAsync(
                    _readDb.Families.Where(f => f.RetreatId == retreatId),
                    ct),

            "shirts-by-size" => 
                await _readDb.AnyAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            "tents-allocation" => 
                await _readDb.AnyAsync(
                    _readDb.Tents.Where(t => t.RetreatId == retreatId),
                    ct),

            "people-epitaph" => 
                await _readDb.AnyAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            _ => false
        };
    }

    private async Task<int> GetEstimatedCountAsync(string templateKey, Guid retreatId, CancellationToken ct)
    {
        return templateKey switch
        {
            "contemplated-participants" => 
                await _readDb.CountAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            "contemplated-by-family" => 
                await _readDb.CountAsync(
                    _readDb.Families.Where(f => f.RetreatId == retreatId),
                    ct),

            "shirts-by-size" => 
                await _readDb.CountAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            "tents-allocation" => 
                await _readDb.CountAsync(
                    _readDb.Tents.Where(t => t.RetreatId == retreatId),
                    ct),

            "people-epitaph" => 
                await _readDb.CountAsync(
                    _readDb.Registrations.Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected),
                    ct),

            _ => 0
        };
    }

    private static string GetTemplateDescription(string key) => key switch
    {
        "contemplated-participants" => "Lista completa de participantes contemplados no sorteio",
        "contemplated-by-family" => "Participantes organizados por família espiritual",
        "shirts-by-size" => "Distribuição de camisetas por tamanho",
        "tents-allocation" => "Mapa de alocação de participantes em barracas",
        "people-epitaph" => "Informações de epitáfio dos participantes",
        _ => "Relatório disponível"
    };

    private static string GetTemplateCategory(string key) => key switch
    {
        "contemplated-participants" => "Participantes",
        "contemplated-by-family" => "Participantes",
        "shirts-by-size" => "Logística",
        "tents-allocation" => "Logística",
        "people-epitaph" => "Participantes",
        _ => "Geral"
    };
}
