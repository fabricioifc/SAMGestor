using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de participantes agrupados por família espiritual.
/// Exibe padrinhos/madrinhas e membros separados por gênero.
/// </summary>

public sealed class RahamistasPerFamiliaTemplate : IReportTemplate
{
    public string Key => "rahamistas-per-familia";
    public string DefaultTitle => "Rahamistas por Família";

    private readonly IReportingReadDb _readDb;

    public RahamistasPerFamiliaTemplate(IReportingReadDb readDb)
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
        
        var columns = GetColumns();

        if (families.Count == 0)
            return CreateEmptyPayload(ctx, retreatId, columns);

        var familyIds = families.Select(f => f.Id).ToList();

        var familyMembers = await _readDb.ToListAsync(
            _readDb.FamilyMembers
                .Where(fm => familyIds.Contains(fm.FamilyId))
                .OrderBy(fm => fm.Position)
                .Select(fm => new FamilyMemberRow(
                    fm.FamilyId,
                    fm.RegistrationId,
                    fm.Position,
                    fm.IsPadrinho,
                    fm.IsMadrinha
                )),
            ct);

        var registrationIds = familyMembers.Select(fm => fm.RegistrationId).Distinct().ToList();

        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => registrationIds.Contains(r.Id)),
            ct);

        var registrations = registrationsFromDb
            .Select(r => new RegistrationRow(
                r.Id,
                r.Name.Value,
                r.Gender,
                r.BirthDate,
                r.WeightKg,
                r.HeightCm,
                r.ShirtSize,
                r.City,
                r.Status 
            ))
            .ToList();

        var regDict = registrations.ToDictionary(r => r.Id);

       
        var familyData = new List<FamilyGroupData>(families.Count);

        foreach (var family in families)
        {
            var members = familyMembers
                .Where(fm => fm.FamilyId == family.Id)
                .Select(fm =>
                {
                    regDict.TryGetValue(fm.RegistrationId, out var reg);
                    
                    return new MemberData
                    {
                        RegistrationId = fm.RegistrationId,
                        Position = fm.Position,
                        IsPadrinho = fm.IsPadrinho,
                        IsMadrinha = fm.IsMadrinha,
                        Registration = reg,
                    };
                })
                .Where(m => m.Registration != null)
                .OrderBy(m => m.Position)
                .ToList();

            familyData.Add(new FamilyGroupData
            {
                FamilyId = family.Id,
                FamilyName = family.Name,
                FamilyColor = family.Color,
                Members = members!
            });
        }

        if (familyData.Count == 0)
            return CreateEmptyPayload(ctx, retreatId, columns);

        var totalFamilies = familyData.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;

        var pagedFamilies = take > 0
            ? familyData.Skip(skip).Take(take).ToList()
            : familyData;

        var data = pagedFamilies.Select(BuildFamilyRow).ToList();

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
            ["totalFamilies"] = totalFamilies,
            ["totalParticipants"] = familyData.Sum(f => f.Members.Count),
            ["totalPadrinhos"] = familyData.Sum(f => f.Members.Count(m => m.IsPadrinho || m.IsMadrinha))
        };

        return new ReportPayload(header, columns, data, summary, totalFamilies, page, take);
    }

    private static ColumnDef[] GetColumns() => new[]
    {
        new ColumnDef("familyName", "Família"),
        new ColumnDef("familyColor", "Cor"),
        new ColumnDef("padrinhos", "Padrinhos/Madrinhas"),
        new ColumnDef("mulheres", "Rahamistas Mulheres"),
        new ColumnDef("homens", "Rahamistas Homens"),
        new ColumnDef("totalMembers", "Total Membros"),
        new ColumnDef("shirtSummary", "Resumo Camisetas")
    };

    private IDictionary<string, object?> BuildFamilyRow(FamilyGroupData family)
    {
        
        var padrinhos = family.Members
            .Where(m => m.IsPadrinho || m.IsMadrinha)
            .Select((m, idx) => FormatMembro(m, idx + 1))
            .ToList();

        var mulheres = family.Members
            .Where(m => !m.IsPadrinho && !m.IsMadrinha && m.Registration!.Gender == Gender.Female)
            .Select((m, idx) => FormatMembro(m, idx + 1))
            .ToList();
        
        var homens = family.Members
            .Where(m => !m.IsPadrinho && !m.IsMadrinha && m.Registration!.Gender == Gender.Male)
            .Select((m, idx) => FormatMembro(m, idx + 1))
            .ToList();

        var shirtSummary = GetShirtSummary(family.Members);

        return new Dictionary<string, object?>
        {
            ["familyName"] = family.FamilyName,
            ["familyColor"] = family.FamilyColor,
            ["padrinhos"] = padrinhos,
            ["mulheres"] = mulheres,
            ["homens"] = homens,
            ["totalMembers"] = family.Members.Count,
            ["shirtSummary"] = shirtSummary
        };
    }

    private Dictionary<string, object?> FormatMembro(MemberData member, int index)
    {
        var reg = member.Registration!;
        var idade = CalculateAge(reg.BirthDate);
        var altura = reg.HeightCm.HasValue ? $"{reg.HeightCm.Value:F0}" : "-";
        var peso = reg.WeightKg.HasValue ? $"{reg.WeightKg.Value:0}" : "-";
        var paymentStatus = GetPaymentStatusFromRegistration(reg.Status);

        return new Dictionary<string, object?>
        {
            ["index"] = index,
            ["name"] = reg.Name,
            ["idade"] = idade,
            ["peso"] = peso,
            ["altura"] = altura,
            ["shirtSize"] = reg.ShirtSize?.ToString() ?? "-",
            ["city"] = reg.City ?? "-",
            ["paymentStatus"] = paymentStatus
        };
    }

    private static string GetShirtSummary(List<MemberData> members)
    {
        var sizes = members
            .Select(m => m.Registration?.ShirtSize)
            .Where(s => s.HasValue)
            .GroupBy(s => s!.Value)
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        return string.Join(", ", sizes);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private static string GetPaymentStatusFromRegistration(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.Confirmed => "Confirmado",
            RegistrationStatus.PaymentConfirmed => "Confirmado",
            RegistrationStatus.PendingPayment => "Pendente",
            RegistrationStatus.Selected => "Selecionado",
            RegistrationStatus.NotSelected => "Não selecionado",
            RegistrationStatus.Canceled => "Cancelado",
            _ => "Pendente"
        };
    }
    
    
    private static ReportPayload CreateEmptyPayload(ReportContext ctx, Guid retreatId, ColumnDef[] columns)
    {
        var header = new ReportHeader(
            TemplateKey: "rahamistas-per-familia",
            Title: "Rahamistas por Família",
            Category: "Participantes",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalFamilies"] = 0,
            ["totalParticipants"] = 0,
            ["totalPadrinhos"] = 0
        };

        return new ReportPayload(header, columns, new List<IDictionary<string, object?>>(), summary, 0, 1, 0);
    }
    
    private sealed record FamilyRow(Guid Id, string Name, string Color);

    private sealed record FamilyMemberRow(
        Guid FamilyId,
        Guid RegistrationId,
        int Position,
        bool IsPadrinho,
        bool IsMadrinha);

    private sealed record RegistrationRow(
        Guid Id,
        string Name,
        Gender Gender,
        DateOnly BirthDate,
        decimal? WeightKg,
        decimal? HeightCm,
        ShirtSize? ShirtSize,
        string? City,
        RegistrationStatus Status );


    private sealed class FamilyGroupData
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string FamilyColor { get; set; } = string.Empty;
        public List<MemberData> Members { get; set; } = new();
    }

    private sealed class MemberData
    {
        public Guid RegistrationId { get; set; }
        public int Position { get; set; }
        public bool IsPadrinho { get; set; }
        public bool IsMadrinha { get; set; }
        public RegistrationRow? Registration { get; set; }
    }
}
