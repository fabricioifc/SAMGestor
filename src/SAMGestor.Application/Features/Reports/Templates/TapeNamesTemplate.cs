using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório "Fitas" - Lista simples de nomes dos participantes confirmados e pagos.
/// Usado para gerar fitas/listas com nomes dos participantes.
/// </summary>

public sealed class TapeNamesTemplate : IReportTemplate
{
    public string Key => "tape-names";
    public string DefaultTitle => "Fitas";

    private readonly IReportingReadDb _readDb;

    public TapeNamesTemplate(IReportingReadDb readDb)
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
            .OrderBy(r => r.Name.Value)
            .Select(r => new
            {
                r.Id,
                Name = r.Name.Value
            });

        var registrations = await _readDb.ToListAsync(registrationsQuery, ct);

        if (!registrations.Any())
            return CreateEmptyPayload(ctx);
        
        var columns = new[]
        {
            new ColumnDef("number", "#"),
            new ColumnDef("name", "Nome")
        };

        var data = registrations
            .Select((reg, index) => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["number"] = (index + 1).ToString(),
                ["name"] = reg.Name
            })
            .ToList();

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Participantes",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = registrations.Count
        };

        return new ReportPayload(header, columns, data, summary, registrations.Count, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Participantes",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhum participante confirmado e pago encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }
}
