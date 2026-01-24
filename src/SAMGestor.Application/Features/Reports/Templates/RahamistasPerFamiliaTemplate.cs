using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de participantes contemplados agrupados por família espiritual.
/// Exibe padrinhos/madrinhas e membros separados por gênero, com dados completos.
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

        var families = await _readDb.ToListAsync(
            _readDb.Families
                .Where(f => f.RetreatId == retreatId)
                .OrderBy(f => f.Name)
                .Select(f => new {
                    f.Id,
                    Name = f.Name.Value,
                    Color = f.Color.HexCode
                }),
            ct);

        if (!families.Any())
        {
            return CreateEmptyPayload(ctx);
        }

        var familyIds = families.Select(f => f.Id).ToList();
        var familyMembers = await _readDb.ToListAsync(
            _readDb.FamilyMembers
                .Where(fm => familyIds.Contains(fm.FamilyId))
                .OrderBy(fm => fm.Position)
                .Select(fm => new {
                    fm.FamilyId,
                    fm.RegistrationId,
                    fm.Position,
                    fm.IsPadrinho,
                    fm.IsMadrinha
                }),
            ct);

        var registrationIds = familyMembers.Select(fm => fm.RegistrationId).ToList();
        var registrations = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => registrationIds.Contains(r.Id) && 
                           r.Status == RegistrationStatus.Selected)
                .Select(r => new {
                    r.Id,
                    Name = r.Name.Value,
                    r.Gender,
                    r.BirthDate,
                    r.WeightKg,
                    r.HeightCm,
                    r.ShirtSize,
                    r.City
                }),
            ct);

        var regDict = registrations.ToDictionary(r => r.Id);

        var payments = await _readDb.ToListAsync(
            _readDb.Payments
                .Where(p => registrationIds.Contains(p.RegistrationId))
                .Select(p => new {
                    p.RegistrationId,
                    p.Status,
                    p.Method
                }),
            ct);

        var paymentDict = payments
            .GroupBy(p => p.RegistrationId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.Status == PaymentStatus.Paid)
                      .First()
            );

        var familyData = new List<FamilyGroupData>();

        foreach (var family in families)
        {
            var members = familyMembers
                .Where(fm => fm.FamilyId == family.Id)
                .Select(fm => new MemberData
                {
                    RegistrationId = fm.RegistrationId,
                    Position = fm.Position,
                    IsPadrinho = fm.IsPadrinho,
                    IsMadrinha = fm.IsMadrinha,
                    Registration = regDict.TryGetValue(fm.RegistrationId, out var reg) ? reg : null,
                    Payment = paymentDict.TryGetValue(fm.RegistrationId, out var pay) ? pay : null
                })
                .Where(m => m.Registration != null)
                .OrderBy(m => m.Position)
                .ToList();

            if (members.Any())
            {
                familyData.Add(new FamilyGroupData
                {
                    FamilyId = family.Id,
                    FamilyName = family.Name,
                    FamilyColor = family.Color,
                    Members = members
                });
            }
        }

        var totalFamilies = familyData.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;
        var pagedFamilies = take > 0
            ? familyData.Skip(skip).Take(take).ToList()
            : familyData;

        var columns = new[]
        {
            new ColumnDef("familyName", "Família"),
            new ColumnDef("familyColor", "Cor"),
            new ColumnDef("padrinhos", "Padrinhos/Madrinhas"),
            new ColumnDef("mulheres", "Mulheres"),
            new ColumnDef("homens", "Homens"),
            new ColumnDef("totalMembers", "Total Membros"),
            new ColumnDef("shirtSummary", "Resumo Camisetas")
        };

        var data = pagedFamilies.Select(family => BuildFamilyRow(family)).ToList();

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

    private IDictionary<string, object?> BuildFamilyRow(FamilyGroupData family)
    {
        var padrinhos = family.Members
            .Where(m => m.IsPadrinho || m.IsMadrinha)
            .Select(m => FormatPadrinhoMadrinha(m))
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

    private Dictionary<string, object?> FormatPadrinhoMadrinha(MemberData member)
    {
        var reg = member.Registration!;
        var paymentStatus = GetPaymentStatus(member.Payment);

        return new Dictionary<string, object?>
        {
            ["name"] = reg.Name,
            ["role"] = member.IsPadrinho ? "PP" : (member.IsMadrinha ? "M" : "?"),
            ["shirtSize"] = reg.ShirtSize?.ToString() ?? "-",
            ["paymentStatus"] = paymentStatus
        };
    }

    private Dictionary<string, object?> FormatMembro(MemberData member, int index)
    {
        var reg = member.Registration!;
        var idade = CalculateAge(reg.BirthDate);
        var altura = reg.HeightCm.HasValue ? $"{reg.HeightCm.Value:0.00}" : "-";
        var peso = reg.WeightKg.HasValue ? $"{reg.WeightKg.Value:0}" : "-";
        var paymentStatus = GetPaymentStatus(member.Payment);

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

    private string GetShirtSummary(List<MemberData> members)
    {
        var sizes = members
            .Where(m => m.Registration?.ShirtSize != null)
            .GroupBy(m => m.Registration!.ShirtSize!.Value)
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        return string.Join(", ", sizes);
    }

    private int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private string GetPaymentStatus(dynamic? payment)
    {
        if (payment == null) return "Pendente";
        
        return payment.Status switch
        {
            PaymentStatus.Paid => GetPaymentMethodLabel(payment.Method),
            PaymentStatus.Pending => "Pendente",
            PaymentStatus.Canceled => "Falhou",
            _ => "Pendente"
        };
    }

    private string GetPaymentMethodLabel(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Pix => "Ok pix",
            PaymentMethod.Card => "Ok cartão",
            PaymentMethod.BankSlip => "Ok boleto",
            PaymentMethod.Cash => "Ok dinheiro",
            PaymentMethod.BankTransfer => "Ok transf",
            PaymentMethod.Check => "Ok cheque",
            _ => "Ok"
        };
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
            new Dictionary<string, object?> { ["message"] = "Nenhuma família encontrada" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }
    
    private class FamilyGroupData
    {
        public Guid FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string FamilyColor { get; set; } = string.Empty;
        public List<MemberData> Members { get; set; } = new();
    }

    private class MemberData
    {
        public Guid RegistrationId { get; set; }
        public int Position { get; set; }
        public bool IsPadrinho { get; set; }
        public bool IsMadrinha { get; set; }
        public dynamic? Registration { get; set; }
        public dynamic? Payment { get; set; }
    }
}
