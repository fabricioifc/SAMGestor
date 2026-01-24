using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório completo de participantes contemplados com foto, dados pessoais e status.
/// </summary>

public sealed class ContemplatedParticipantsTemplate : IReportTemplate
{
    public string Key => "contemplated-participants";
    public string DefaultTitle => "Participantes Contemplados";

    private readonly IReportingReadDb _readDb;

    public ContemplatedParticipantsTemplate(IReportingReadDb readDb)
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
                           r.Status == RegistrationStatus.Selected)
                .Select(r => new {
                    r.Id,
                    Name = r.Name.Value,
                    Email = r.Email.Value,
                    r.Phone,
                    r.BirthDate,
                    r.City,
                    r.Status,
                    PhotoUrl = r.PhotoUrl != null ? r.PhotoUrl.Value : null,
                    PhotoStorageKey = r.PhotoStorageKey
                })
                .OrderBy(r => r.Name),
            ct);

        if (!registrations.Any())
        {
            return CreateEmptyPayload(ctx);
        }

        var allRows = registrations.Select(r => new ParticipantRow
        {
            Name = r.Name,
            Email = r.Email,
            Phone = FormatPhone(r.Phone),
            Age = CalculateAge(r.BirthDate),
            City = r.City ?? "-",
            Status = r.Status,
            StatusLabel = GetStatusLabel(r.Status),
            PhotoUrl = r.PhotoUrl,
            PhotoStorageKey = r.PhotoStorageKey
        }).ToList();

        var statusCounts = allRows
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalRecords = allRows.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;
        var pagedRows = take > 0 
            ? allRows.Skip(skip).Take(take).ToList() 
            : allRows;

        var columns = new[]
        {
            new ColumnDef("photoUrl", "Foto"),
            new ColumnDef("photoStorageKey", "Foto Key"),
            new ColumnDef("name", "Nome"),
            new ColumnDef("age", "Idade"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("phone", "Telefone"),
            new ColumnDef("email", "E-mail"),
            new ColumnDef("status", "Status")
        };

        var data = pagedRows
            .Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["photoUrl"] = r.PhotoUrl,
                ["photoStorageKey"] = r.PhotoStorageKey,
                ["name"] = r.Name,
                ["age"] = r.Age,
                ["city"] = r.City,
                ["phone"] = r.Phone,
                ["email"] = r.Email,
                ["status"] = r.StatusLabel
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
            ["selected"] = statusCounts.GetValueOrDefault(RegistrationStatus.Selected, 0),
            ["pendingPayment"] = statusCounts.GetValueOrDefault(RegistrationStatus.PendingPayment, 0),
            ["paymentConfirmed"] = statusCounts.GetValueOrDefault(RegistrationStatus.PaymentConfirmed, 0),
            ["confirmed"] = statusCounts.GetValueOrDefault(RegistrationStatus.Confirmed, 0),
            ["canceled"] = statusCounts.GetValueOrDefault(RegistrationStatus.Canceled, 0)
        };

        return new ReportPayload(header, columns, data, summary, totalRecords, page, take);
    }

    private int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private string GetStatusLabel(RegistrationStatus status) => status switch
    {
        RegistrationStatus.NotSelected => "Não Contemplado",
        RegistrationStatus.Selected => "Contemplado",
        RegistrationStatus.PendingPayment => "Aguardando Pagamento",
        RegistrationStatus.PaymentConfirmed => "Pagamento Confirmado",
        RegistrationStatus.Confirmed => "Confirmado",
        RegistrationStatus.Canceled => "Cancelado",
        _ => "Desconhecido"
    };

    private string FormatPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "-";

        var digits = new string(phone.Where(char.IsDigit).ToArray());
       
        if (digits.Length == 11)
            return $"({digits[..2]}) {digits[2..7]}-{digits[7..]}";
        if (digits.Length == 10)
            return $"({digits[..2]}) {digits[2..6]}-{digits[6..]}";
        
        return phone;
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
            new Dictionary<string, object?> { ["message"] = "Nenhum participante contemplado encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }

    private class ParticipantRow
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string City { get; set; } = string.Empty;
        public RegistrationStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? PhotoStorageKey { get; set; }
    }
}
