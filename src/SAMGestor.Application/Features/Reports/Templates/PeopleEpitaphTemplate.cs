using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de Lápides para atividade do retiro.
/// Exibe foto, nome, data de nascimento, cidade e família dos confirmados.
/// Filtro: PaymentConfirmed e Confirmed apenas.
/// </summary>
public sealed class PeopleEpitaphTemplate : IReportTemplate
{
    public string Key => "people-epitaph";
    public string DefaultTitle => "Lápides dos Participantes";

    private readonly IReportingReadDb _readDb;

    public PeopleEpitaphTemplate(IReportingReadDb readDb)
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
                            (r.Status == RegistrationStatus.Confirmed ||
                             r.Status == RegistrationStatus.PaymentConfirmed))
                .Select(r => new {
                    r.Id,
                    Name = r.Name.Value,
                    r.BirthDate,
                    r.City,
                    PhotoUrl = r.PhotoUrl != null ? r.PhotoUrl.Value : null,
                    PhotoStorageKey = r.PhotoStorageKey
                }),
            ct);

        if (!registrations.Any())
        {
            return CreateEmptyPayload(ctx);
        }

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
                .Select(f => new { f.Id, Name = f.Name.Value, Color = f.Color.HexCode }),
            ct);

        var familyDict = families.ToDictionary(f => f.Id);
        var memberFamilyDict = familyMembers.ToDictionary(fm => fm.RegistrationId, fm => fm.FamilyId);

        var allRows = registrations
            .Select(r =>
            {
                var hasFamilyId = memberFamilyDict.TryGetValue(r.Id, out var familyId);
                var family = hasFamilyId && familyDict.TryGetValue(familyId, out var fam) 
                    ? fam 
                    : null;

                return new EpitaphRow
                {
                    Name = r.Name,
                    BirthDate = r.BirthDate,
                    City = r.City ?? "-",
                    PhotoUrl = r.PhotoUrl,
                    PhotoStorageKey = r.PhotoStorageKey,
                    FamilyName = family?.Name,
                    FamilyColor = family?.Color,
                    FamilyOrder = ExtractFamilyOrderKey(family?.Name ?? "ZZZ_SEM_FAMILIA")
                };
            })
            .OrderBy(r => r.FamilyOrder)
            .ThenBy(r => r.Name)
            .ToList();
        
        
        var totalRecords = allRows.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;
        var pagedRows = take > 0 
            ? allRows.Skip(skip).Take(take).ToList() 
            : allRows;
        
        var columns = new[]
        {
            new ColumnDef("photoUrl", "Foto URL"),
            new ColumnDef("photoStorageKey", "Foto Key"),
            new ColumnDef("name", "Nome"),
            new ColumnDef("birthDate", "Data de Nascimento"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("familyName", "Família"),
            new ColumnDef("familyColor", "Cor Família")
        };

        var data = pagedRows
            .Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["photoUrl"] = r.PhotoUrl,
                ["photoStorageKey"] = r.PhotoStorageKey,
                ["name"] = r.Name,
                ["birthDate"] = r.BirthDate.ToString("dd/MM/yyyy"),
                ["city"] = r.City,
                ["familyName"] = r.FamilyName ?? "Sem Família",
                ["familyColor"] = r.FamilyColor ?? "#CCCCCC"
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
            ["totalParticipants"] = totalRecords,
            ["withFamily"] = allRows.Count(r => r.FamilyName != null),
            ["withoutFamily"] = allRows.Count(r => r.FamilyName == null),
            ["withPhoto"] = allRows.Count(r => !string.IsNullOrWhiteSpace(r.PhotoUrl))
        };

        return new ReportPayload(header, columns, data, summary, totalRecords, page, take);
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
            new Dictionary<string, object?> { ["message"] = "Nenhum participante confirmado encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }
    
    private class EpitaphRow
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly BirthDate { get; set; }
        public string City { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? PhotoStorageKey { get; set; }
        public string? FamilyName { get; set; }
        public string? FamilyColor { get; set; }
        public string FamilyOrder { get; set; } = string.Empty;
    }
    private static string ExtractFamilyOrderKey(string familyName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(familyName, @"(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
        {
            return $"A{number:D3}"; 
        }
        return $"B{familyName}";
    }
}
