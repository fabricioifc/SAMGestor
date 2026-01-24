using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de contagem de camisetas por tamanho dos contemplados pagos.
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
        
        var registrations = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => r.RetreatId == retreatId && 
                           r.Status == RegistrationStatus.Selected &&
                           r.ShirtSize.HasValue)
                .Select(r => new { r.Id, r.ShirtSize }),
            ct);
        
        var paidRegistrationIds = await _readDb.ToListAsync(
            _readDb.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Select(p => p.RegistrationId),
            ct);

        var paidSet = paidRegistrationIds.ToHashSet();
        
        var sizes = registrations
            .Where(r => paidSet.Contains(r.Id) && r.ShirtSize.HasValue)
            .Select(r => r.ShirtSize!.Value)
            .ToList();
        
        var grouped = sizes
            .GroupBy(s => s)
            .Select(g => new { Size = g.Key, Count = g.Count() })
            .OrderBy(x => x.Size)
            .ToList();
        
        var columns = new[]
        {
            new ColumnDef("size", "Tamanho de Camiseta"),
            new ColumnDef("count", "Quantidade")
        };

        var data = grouped
            .Select(x => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["size"] = MapSizeLabel(x.Size),
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

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = sizes.Count,
            ["totalShirts"] = sizes.Count
        };

        return new ReportPayload(header, columns, data, summary, data.Count, 1, 0);
    }

    private static string MapSizeLabel(ShirtSize size) => size switch
    {
        ShirtSize.P => "P",
        ShirtSize.M => "M",
        ShirtSize.G => "G",
        ShirtSize.GG => "GG",
        ShirtSize.GG1 => "GG1",
        ShirtSize.GG2 => "GG2",
        ShirtSize.GG3 => "GG3",
        ShirtSize.GG4 => "GG4",
        _ => "Não definido"
    };
}
