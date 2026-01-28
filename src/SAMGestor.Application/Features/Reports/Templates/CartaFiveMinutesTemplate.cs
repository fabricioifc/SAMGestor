using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Carta 5 Minutos: Uma página por participante (Confirmados + PaymentConfirmed).
/// Topo com nome e cor da família, resto em branco para anotações.
/// </summary>
public sealed class CartaFiveMinutesTemplate : IReportTemplate
{
    public string Key => "carta-five-minutes";
    public string DefaultTitle => "Carta 5 Minutos";

    private readonly IReportingReadDb _readDb;

    public CartaFiveMinutesTemplate(IReportingReadDb readDb)
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

        var registrations = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => r.RetreatId == retreatId &&
                           (r.Status == RegistrationStatus.Confirmed ||
                            r.Status == RegistrationStatus.PaymentConfirmed))
                .OrderBy(r => r.Name.Value)
                .Select(r => new
                {
                    r.Id,
                    Name = r.Name.Value
                }),
            ct);

        if (!registrations.Any())
            return CreateEmptyPayload(ctx);

        var registrationIds = registrations.Select(r => r.Id).ToList();
        
        var familyMembers = await _readDb.ToListAsync(
            _readDb.FamilyMembers
                .Where(fm => registrationIds.Contains(fm.RegistrationId))
                .Select(fm => new { fm.FamilyId, fm.RegistrationId }),
            ct);

        var familyIds = familyMembers.Select(fm => fm.FamilyId).Distinct().ToList();

        var families = await _readDb.ToListAsync(
            _readDb.Families
                .Where(f => familyIds.Contains(f.Id))
                .Select(f => new
                {
                    f.Id,
                    Name = f.Name.Value,
                    Color = f.Color.HexCode
                }),
            ct);

        var familyDict = families.ToDictionary(f => f.Id);
        var memberDict = familyMembers.ToDictionary(fm => fm.RegistrationId, fm => fm.FamilyId);
        
        var columns = new[]
        {
            new ColumnDef("name", "Nome"),
            new ColumnDef("familyColor", "Cor Família")
        };

        var data = registrations
            .Select(reg =>
            {
                var hasFamilyId = memberDict.TryGetValue(reg.Id, out var familyId);
                var family = hasFamilyId && familyDict.TryGetValue(familyId, out var fam)
                    ? fam
                    : null;

                return (IDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["name"] = reg.Name,
                    ["familyColor"] = family?.Color ?? "#CCCCCC"
                };
            })
            .ToList();

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Fichas",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = registrations.Count,
            ["totalPages"] = registrations.Count
        };

        return new ReportPayload(header, columns, data, summary, registrations.Count, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Fichas",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhum participante confirmado encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }
}
