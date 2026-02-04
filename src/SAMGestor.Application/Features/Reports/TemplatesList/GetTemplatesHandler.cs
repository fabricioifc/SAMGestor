using MediatR;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.TemplatesList;

public sealed class GetTemplatesSchemasHandler 
    : IRequestHandler<GetTemplatesSchemasQuery, IReadOnlyList<ReportTemplateInfoDto>>
{
    private readonly IReportTemplateRegistry _registry;

    public GetTemplatesSchemasHandler(IReportTemplateRegistry registry)
    {
        _registry = registry;
    }

    public Task<IReadOnlyList<ReportTemplateInfoDto>> Handle(
        GetTemplatesSchemasQuery request, 
        CancellationToken ct)
    {
        var templates = _registry.GetAllTemplates();

        var templateInfos = templates
            .Select(t => new ReportTemplateInfoDto(
                Key: t.Key,
                Title: t.DefaultTitle,
                Description: GetDescription(t.Key),
                Category: GetCategory(t.Key),
                HasData: true,
                EstimatedRecords: null
            ))
            .ToList();

        return Task.FromResult<IReadOnlyList<ReportTemplateInfoDto>>(templateInfos);
    }

    private static string GetCategory(string key)
    {
        return key switch
        {
            "people-epitaph" => "Participantes",
            "contemplated-participants" => "Participantes",
            "tape-names" => "Participantes",
            
            "tents-allocation" => "Alocações",
            
            "check-in-bota-fora" => "Check-in",
            
            "wellness-per-family" => "Bem-Estar",
            
            "participant-individual-card" => "Fichas",
            
            "bags-distribution" => "Distribuição",
            
            "rahamistas-per-familia" => "Famílias",
            
            "shirts-by-size" => "Camisetas",
            
            "service-teams" => "Serviço",
            "service-coordinators" => "Serviço",
            
            _ => "Outros"
        };
    }

    private static string GetDescription(string key)
    {
        return key switch
        {
            "people-epitaph" => "Relatório com foto, data de nascimento e família dos participantes para atividade de lápides",
            "contemplated-participants" => "Lista completa de participantes contemplados com contato e informações detalhadas",
            "tape-names" => "Lista simples de nomes dos participantes confirmados e pagos",
            "tents-allocation" => "Alocação de participantes por barraca, organizado por número da barraca",
            "check-in-bota-fora" => "Checklist de entrada com campos para marcar itens recebidos (4 faixas alfabéticas)",
            "wellness-per-family" => "Relatório por família para anotar medicamentos e observações de bem-estar",
            "participant-individual-card" => "Ficha individual com uma página por participante para anotações durante o retiro",
            "bags-distribution" => "Distribuição aleatória de participantes em duas colunas",
            "rahamistas-per-familia" => "Lista de rahamistas agrupados por família com cores",
            "shirts-by-size" => "Relatório de camisetas agrupado por tamanho (P, M, G, GG, etc.)",
            
            "service-teams" => "Equipes de serviço com coordenadores, vice-coordenadores e membros",
            "service-coordinators" => "Lista detalhada de coordenadores e vice-coordenadores das equipes de serviço",
            
            _ => "Relatório customizado"
        };
    }
}
