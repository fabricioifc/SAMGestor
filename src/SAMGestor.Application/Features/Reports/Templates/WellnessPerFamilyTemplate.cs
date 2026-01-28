using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de Bem-Estar organizado por família.
/// Mostra cada família com espaço para anotar medicamentos/observações.
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

        var familiesFromDb = await _readDb.ToListAsync(
            _readDb.Families
                .Where(f => f.RetreatId == retreatId),
            ct);

        var families = familiesFromDb
            .Select(f => new FamilyRow(
                f.Id,
                f.Name.Value,
                f.Color.HexCode
            ))
            .OrderBy(f => 
            {
                var parts = f.Name.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out var num))
                    return num;
                return int.MaxValue;
            })
            .ThenBy(f => f.Name)
            .ToList();

        if (families.Count == 0)
            return CreateEmptyPayload(ctx);

        var familyIds = families.Select(f => f.Id).ToList();

        var familyMembers = await _readDb.ToListAsync(
            _readDb.FamilyMembers
                .Where(fm => familyIds.Contains(fm.FamilyId))
                .Select(fm => new FamilyMemberRow(fm.FamilyId, fm.RegistrationId)),
            ct);

        var registrationIds = familyMembers.Select(fm => fm.RegistrationId).Distinct().ToList();

        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => registrationIds.Contains(r.Id)),
            ct);

        var confirmedIds = registrationsFromDb.Select(r => r.Id).ToHashSet();
        
        var wellnessData = new List<WellnessGroup>();

        foreach (var family in families)
        {
            var memberCount = familyMembers
                .Count(fm => fm.FamilyId == family.Id);

            if (memberCount > 0)
            {
                wellnessData.Add(new WellnessGroup
                {
                    FamilyId = family.Id,
                    FamilyName = family.Name,
                    FamilyColor = family.Color,
                    MemberCount = memberCount
                });
            }
        }

        if (wellnessData.Count == 0)
            return CreateEmptyPayload(ctx);

        var totalFamilies = wellnessData.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;

        var pagedFamilies = take > 0
            ? wellnessData.Skip(skip).Take(take).ToList()
            : wellnessData;

        var columns = new[]
        {
            new ColumnDef("familyName", "Família"),
            new ColumnDef("familyColor", "Cor"),
            new ColumnDef("memberCount", "Total Membros")
        };

        var data = pagedFamilies.Select(BuildFamilyRow).ToList();

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Bem-Estar",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var totalParticipants = wellnessData.Sum(w => w.MemberCount);

        var summary = new Dictionary<string, object?>
        {
            ["totalFamilies"] = totalFamilies,
            ["totalParticipants"] = totalParticipants
        };

        return new ReportPayload(header, columns, data, summary, totalFamilies, page, take);
    }

    private IDictionary<string, object?> BuildFamilyRow(WellnessGroup group)
    {
        return new Dictionary<string, object?>
        {
            ["familyName"] = group.FamilyName,
            ["familyColor"] = group.FamilyColor,
            ["memberCount"] = group.MemberCount
        };
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
    
    
    private sealed record FamilyRow(Guid Id, string Name, string Color);

    private sealed record FamilyMemberRow(Guid FamilyId, Guid RegistrationId);

    private sealed class WellnessGroup
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string FamilyColor { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }
}
