using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de contagem de camisetas por tamanho dos participantes confirmados.
/// </summary>

public sealed class ShirtsBySizeTemplate : IReportTemplate
{
    public string Key => "shirts-by-size";
    public string DefaultTitle => "Camisetas por Tamanho";

    private readonly IReportingReadDb _readDb;

    public ShirtsBySizeTemplate(IReportingReadDb readDb) 
        => _readDb = readDb;

    public async Task<ReportPayload> GetDataAsync(
        ReportContext ctx,
        int skip,
        int take,
        CancellationToken ct)
    {
        if (ctx.RetreatId == Guid.Empty)
            throw new ArgumentException("RetreatId é obrigatório para este relatório");

        var retreatId = ctx.RetreatId;
        
        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => r.RetreatId == retreatId && 
                            (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.PaymentConfirmed) &&
                            r.ShirtSize.HasValue),
            ct);
        
        var registrations = registrationsFromDb
            .Select(r => new RegistrationRow(r.Id, r.ShirtSize!.Value))
            .ToList();
        
        if (registrations.Count == 0)
            return CreateEmptyPayload(ctx);
      
        var grouped = registrations
            .GroupBy(r => r.ShirtSize)
            .Select(g => new ShirtSizeGroup(g.Key, g.Count()))
            .OrderBy(x => x.Size)
            .ToList();
       
        var columns = new[]
        {
            new ColumnDef("size", "Tamanho"),
            new ColumnDef("sizeLabel", "Descrição"),
            new ColumnDef("count", "Quantidade")
        };

        var data = grouped
            .Select(x => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["size"] = (int)x.Size,
                ["sizeLabel"] = MapSizeLabel(x.Size),
                ["count"] = x.Count
            })
            .ToList();

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Logística",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var totalShirts = grouped.Sum(g => g.Count);

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = registrations.Count,
            ["totalShirts"] = totalShirts,
            ["uniqueSizes"] = grouped.Count
        };

        return new ReportPayload(header, columns, data, summary, data.Count, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Logística",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhuma camiseta encontrada" }
        };

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = 0,
            ["totalShirts"] = 0,
            ["uniqueSizes"] = 0
        };

        return new ReportPayload(header, columns, data, summary, 0, 1, 0);
    }

    private static string MapSizeLabel(ShirtSize size) => size switch
    {
        ShirtSize.P => "P - Pequeno",
        ShirtSize.M => "M - Médio",
        ShirtSize.G => "G - Grande",
        ShirtSize.GG => "GG - Extra Grande",
        ShirtSize.GG1 => "GG1 - Extra Grande 1",
        ShirtSize.GG2 => "GG2 - Extra Grande 2",
        ShirtSize.GG3 => "GG3 - Extra Grande 3",
        ShirtSize.GG4 => "GG4 - Extra Grande 4",
        _ => "Não definido"
    };
    
    private sealed record RegistrationRow(Guid Id, ShirtSize ShirtSize);

    private sealed record ShirtSizeGroup(ShirtSize Size, int Count);
}
