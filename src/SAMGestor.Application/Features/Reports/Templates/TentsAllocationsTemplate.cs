using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de alocação de barracas com participantes confirmados e pagos.
/// Agrupa por barraca e lista rahamistas, madrinhas e padrinhos.
/// Exibe família com cor para melhor visualização.
/// </summary>

public sealed class TentsAllocationTemplate : IReportTemplate
{
    public string Key => "tents-allocation";
    public string DefaultTitle => "Alocação de Barracas";

    private readonly IReportingReadDb _readDb;

    public TentsAllocationTemplate(IReportingReadDb readDb)
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

        var tentsQuery = _readDb.Tents
            .Where(t => t.RetreatId == retreatId)
            .OrderBy(t => t.Number.Value)
            .Select(t => new
            {
                t.Id,
                Number = t.Number.Value,
                t.Capacity,
                Category = t.Category
            });

        var tents = await _readDb.ToListAsync(tentsQuery, ct);

        if (!tents.Any())
            return CreateEmptyPayload(ctx);

        var tentIds = tents.Select(t => t.Id).ToList();

        var registrationsQuery = _readDb.Registrations
            .Where(r => r.RetreatId == retreatId &&
                       (r.Status == RegistrationStatus.Confirmed ||
                        r.Status == RegistrationStatus.PaymentConfirmed))
            .Select(r => new
            {
                r.Id,
                Name = r.Name.Value,
                Gender = r.Gender
            });

        var registrations = await _readDb.ToListAsync(registrationsQuery, ct);
        var regById = registrations.ToDictionary(r => r.Id);

        var tentAssignmentsQuery = _readDb.TentAssignments
            .Where(ta => tentIds.Contains(ta.TentId))
            .Select(ta => new { ta.TentId, ta.RegistrationId });

        var tentAssignments = await _readDb.ToListAsync(tentAssignmentsQuery, ct);
        var assignedRegistrationIds = tentAssignments.Select(ta => ta.RegistrationId).ToList();

        var familyMembersQuery = _readDb.FamilyMembers
            .Where(fm => assignedRegistrationIds.Contains(fm.RegistrationId))
            .Select(fm => new
            {
                fm.FamilyId,
                fm.RegistrationId,
                fm.IsPadrinho,
                fm.IsMadrinha
            });

        var familyMembers = await _readDb.ToListAsync(familyMembersQuery, ct);
        var familyIds = familyMembers.Select(fm => fm.FamilyId).Distinct().ToList();
        
        var familiesQuery = _readDb.Families
            .Where(f => familyIds.Contains(f.Id))
            .Select(f => new
            {
                f.Id,
                Name = f.Name.Value,
                Color = f.Color.HexCode
            });

        var families = await _readDb.ToListAsync(familiesQuery, ct);

        var familyDict = families.ToDictionary(f => f.Id);
        var memberDict = familyMembers.ToDictionary(fm => fm.RegistrationId);

        var allocationsData = new List<TentAllocationRow>();

        foreach (var tent in tents)
        {
            var tentParticipants = tentAssignments
                .Where(ta => ta.TentId == tent.Id)
                .Select(ta =>
                {
                    if (!regById.TryGetValue(ta.RegistrationId, out var reg))
                        return null;

                    var member = memberDict.GetValueOrDefault(ta.RegistrationId);
                    var familyId = member?.FamilyId;
                    var family = familyId.HasValue && familyDict.TryGetValue(familyId.Value, out var fam)
                        ? fam
                        : null;

                    var roleLabel = "Rahamista";
                    var roleOrder = 1;

                    if (member != null)
                    {
                        if (member.IsPadrinho)
                        {
                            roleLabel = "Padrinho";
                            roleOrder = 2;
                        }
                        else if (member.IsMadrinha)
                        {
                            roleLabel = "Madrinha";
                            roleOrder = 3;
                        }
                    }

                    return new TentParticipantRow
                    {
                        RegistrationId = ta.RegistrationId,
                        Name = reg.Name,
                        Gender = reg.Gender.ToString().Substring(0, 1),
                        FamilyName = family?.Name ?? "Sem Família",
                        FamilyColor = family?.Color ?? "#CCCCCC",
                        Role = roleLabel,
                        RoleOrder = roleOrder
                    };
                })
                .Where(p => p != null)
                .OrderBy(p => p!.RoleOrder)
                .ThenBy(p => p!.FamilyName)          
                .ThenBy(p => p!.Name)               
                .ToList();

            if (tentParticipants.Any())
            {
                allocationsData.Add(new TentAllocationRow
                {
                    TentNumber = tent.Number,
                    TentCapacity = tent.Capacity,
                    TentCategory = tent.Category.ToString(),
                    ParticipantCount = tentParticipants.Count,
                    Participants = tentParticipants!
                });
            }
        }

        var columns = new[]
        {
            new ColumnDef("tentNumber", "Barraca"),
            new ColumnDef("tentCategory", "Tipo"),
            new ColumnDef("participantName", "Nome"),
            new ColumnDef("gender", "Sexo"),
            new ColumnDef("role", "Função"),
            new ColumnDef("familyName", "Família"),
            new ColumnDef("familyColor", "Cor")
        };

        var data = new List<IDictionary<string, object?>>();

        foreach (var tent in allocationsData)
        {
           

            foreach (var participant in tent.Participants)
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["tentNumber"] = $"#{tent.TentNumber}",  
                    ["tentCategory"] = tent.TentCategory,   
                    ["participantName"] = participant.Name,
                    ["gender"] = participant.Gender,
                    ["role"] = participant.Role,
                    ["familyName"] = participant.FamilyName,
                    ["familyColor"] = participant.FamilyColor
                });
            }
        }

        var totalTents = allocationsData.Count;
        var totalParticipants = allocationsData.Sum(t => t.ParticipantCount);

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Alocações",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalTents"] = totalTents,
            ["totalParticipants"] = totalParticipants,
            ["averagePerTent"] = totalTents > 0 ? Math.Round((double)totalParticipants / totalTents, 1) : 0
        };

        return new ReportPayload(header, columns, data, summary, totalParticipants, 1, 0);
    }

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Alocações",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhuma alocação de barraca encontrada" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }

    private class TentAllocationRow
    {
        public int TentNumber { get; set; }
        public int TentCapacity { get; set; }
        public string TentCategory { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public List<TentParticipantRow> Participants { get; set; } = new();
    }

    private class TentParticipantRow
    {
        public Guid RegistrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string FamilyColor { get; set; } = "#CCCCCC";
        public string Role { get; set; } = string.Empty;
        public int RoleOrder { get; set; }
    }
}
