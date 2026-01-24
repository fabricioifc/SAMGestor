using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório "Bolsas" - Distribuição aleatória de participantes em 2 colunas.
/// Simples lista de nomes dispostos em colunas para formar grupos.
/// </summary>

public sealed class BagsDistributionTemplate : IReportTemplate
{
    public string Key => "bags-distribution";
    public string DefaultTitle => "Bolsas";

    private readonly IReportingReadDb _readDb;

    public BagsDistributionTemplate(IReportingReadDb readDb)
        => _readDb = readDb;

    public async Task<ReportPayload> GetDataAsync(
        ReportContext ctx,
        int skip,
        int take,
        CancellationToken ct)
    {
        if (ctx.RetreatId == Guid.Empty)
            throw new ArgumentException("RetreatId é obrigatório");

        var retreatId = ctx.RetreatId;

        var paidRegistrationIds = await _readDb.ToListAsync(
            _readDb.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Select(p => p.RegistrationId),
            ct);

        var paidSet = paidRegistrationIds.ToHashSet();

        var registrationsQuery = _readDb.Registrations
            .Where(r => r.RetreatId == retreatId &&
                       r.Status == RegistrationStatus.Confirmed &&
                       paidSet.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                Name = r.Name.Value
            });

        var registrations = await _readDb.ToListAsync(registrationsQuery, ct);

        if (!registrations.Any())
            return CreateEmptyPayload(ctx);

        var random = new Random(DateTime.Now.Millisecond);
        var shuffled = registrations.OrderBy(_ => random.Next()).ToList();
        
        var columnA = new List<string>();
        var columnB = new List<string>();

        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i % 2 == 0)
                columnA.Add(shuffled[i].Name);
            else
                columnB.Add(shuffled[i].Name);
        }

        var maxRows = Math.Max(columnA.Count, columnB.Count);

        var columns = new[]
        {
            new ColumnDef("columnA", "A"),
            new ColumnDef("columnB", "B")
        };

        var data = new List<IDictionary<string, object?>>();

        for (int i = 0; i < maxRows; i++)
        {
            data.Add(new Dictionary<string, object?>
            {
                ["columnA"] = i < columnA.Count ? columnA[i] : "",
                ["columnB"] = i < columnB.Count ? columnB[i] : ""
            });
        }

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Distribuição",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = registrations.Count,
            ["columnACount"] = columnA.Count,
            ["columnBCount"] = columnB.Count
        };

        return new ReportPayload(header, columns, data, summary, registrations.Count, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Distribuição",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhum participante encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }
}
