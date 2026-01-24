using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de Bem-Estar organizado por família.
/// Mostra rahamistas de cada família com campo para anotar medicamentos/observações.
/// </summary>

public sealed class WellnessPerFamilyTemplate : IReportTemplate
{
    public string Key => "wellness-per-family";
    public string DefaultTitle => "Bem-Estar por Família";

    private readonly IReportingReadDb _readDb;

    public WellnessPerFamilyTemplate(IReportingReadDb readDb)
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

        var registrationIds = registrations.Select(r => r.Id).ToList();

        var familyMembersQuery = _readDb.FamilyMembers
            .Where(fm => registrationIds.Contains(fm.RegistrationId))
            .Select(fm => new { fm.FamilyId, fm.RegistrationId });

        var familyMembers = await _readDb.ToListAsync(familyMembersQuery, ct);

        var familyIds = familyMembers.Select(fm => fm.FamilyId).Distinct().ToList();

        var familiesQuery = _readDb.Families
            .Where(f => familyIds.Contains(f.Id))
            .OrderBy(f => f.Name.Value)
            .Select(f => new
            {
                f.Id,
                Name = f.Name.Value,
                Color = f.Color.HexCode
            });

        var families = await _readDb.ToListAsync(familiesQuery, ct);
        
        var familyDict = families.ToDictionary(f => f.Id);
        var memberDict = familyMembers.ToDictionary(fm => fm.RegistrationId, fm => fm.FamilyId);

        var wellnessData = new List<WellnessRow>();

        foreach (var family in families)
        {
            var familyParticipants = familyMembers
                .Where(fm => fm.FamilyId == family.Id)
                .Select(fm => fm.RegistrationId)
                .ToList();

            var participants = registrations
                .Where(r => familyParticipants.Contains(r.Id))
                .OrderBy(r => r.Name)
                .ToList();

            wellnessData.Add(new WellnessRow
            {
                FamilyId = family.Id,
                FamilyName = family.Name,
                FamilyColor = family.Color,
                Participants = participants.Select(p => new WellnessParticipant
                {
                    RegistrationId = p.Id,
                    Name = p.Name
                }).ToList()
            });
        }
        
        var columns = new[]
        {
            new ColumnDef("family", "Família"),
            new ColumnDef("name", "Rahamista"),
            new ColumnDef("medicacao", "Medicamento/Observações"),
            new ColumnDef("familyColor", "Cor")
        };

        var data = new List<IDictionary<string, object?>>();

        foreach (var family in wellnessData)
        {
            var isFirstInFamily = true;

            foreach (var participant in family.Participants)
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["family"] = isFirstInFamily ? family.FamilyName : "",
                    ["name"] = participant.Name,
                    ["medicacao"] = "",  
                    ["familyColor"] = family.FamilyColor
                });

                isFirstInFamily = false;
            }
            
            if (wellnessData.IndexOf(family) < wellnessData.Count - 1)
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["family"] = "",
                    ["name"] = "",
                    ["medicacao"] = "",
                    ["familyColor"] = "#FFFFFF"
                });
            }
        }

        var totalParticipants = registrations.Count;

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Bem-Estar",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalFamilies"] = families.Count,
            ["totalParticipants"] = totalParticipants
        };

        return new ReportPayload(header, columns, data, summary, totalParticipants, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Bem-Estar",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhuma família encontrada" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }

    private class WellnessRow
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string FamilyColor { get; set; } = string.Empty;
        public List<WellnessParticipant> Participants { get; set; } = new();
    }

    private class WellnessParticipant
    {
        public Guid RegistrationId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
