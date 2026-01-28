using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de Check-in "Bota Fora x Rahamivida"
/// Usado na chegada do retiro para marcar itens recebidos/entregues.
/// Participantes confirmados, divididos em 4 tabelas por faixa alfabética (A-D, E-H, I-L, M-Z).
/// </summary>

public sealed class CheckInBotaForaTemplate : IReportTemplate
{
    public string Key => "check-in-bota-fora";
    public string DefaultTitle => "Bota Fora x Rahamivida";

    private readonly IReportingReadDb _readDb;

    public CheckInBotaForaTemplate(IReportingReadDb readDb)
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

        var validStatuses = new[] { RegistrationStatus.Confirmed, RegistrationStatus.PaymentConfirmed };
        
        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.Registrations
                .Where(r => r.RetreatId == retreatId && validStatuses.Contains(r.Status))
                .OrderBy(r => r.Name.Value),
            ct);

        if (registrationsFromDb.Count == 0)
            return CreateEmptyPayload(ctx);

        var registrations = registrationsFromDb
            .Select(r => new ParticipantRow(r.Id, r.Name.Value))
            .ToList();

        var faixas = new Dictionary<string, List<ParticipantRow>>
        {
            ["A-D"] = new(),
            ["E-H"] = new(),
            ["I-L"] = new(),
            ["M-Z"] = new()
        };

        foreach (var reg in registrations)
        {
            var firstLetter = char.ToUpper(reg.Name.FirstOrDefault());
            var faixa = GetFaixa(firstLetter);
            faixas[faixa].Add(reg);
        }

        var columns = new[]
        {
            new ColumnDef("faixa", "Faixa"),
            new ColumnDef("name", "Nome"),
            new ColumnDef("termo", "Termo"),
            new ColumnDef("celular", "Celular"),
            new ColumnDef("relogio", "Relógio"),
            new ColumnDef("remedio", "Remédio"),
            new ColumnDef("carteira", "Carteira"),
            new ColumnDef("bolsaMao", "Bolsa de Mão"),
            new ColumnDef("chave", "Chave"),
            new ColumnDef("dataNasc", "Data Nasc"),
            new ColumnDef("assinatura", "Assinatura")
        };

        var data = new List<IDictionary<string, object?>>();

        foreach (var faixaKey in new[] { "A-D", "E-H", "I-L", "M-Z" })
        {
            var participants = faixas[faixaKey];
            
            foreach (var participant in participants)
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["faixa"] = faixaKey,
                    ["name"] = participant.Name,
                    ["termo"] = "",
                    ["celular"] = "",
                    ["relogio"] = "",
                    ["remedio"] = "",
                    ["carteira"] = "",
                    ["bolsaMao"] = "",
                    ["chave"] = "",
                    ["dataNasc"] = "",
                    ["assinatura"] = ""
                });
            }
        }

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Check-in",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = registrations.Count,
            ["faixaAD"] = faixas["A-D"].Count,
            ["faixaEH"] = faixas["E-H"].Count,
            ["faixaIL"] = faixas["I-L"].Count,
            ["faixaMZ"] = faixas["M-Z"].Count
        };

        return new ReportPayload(header, columns, data, summary, registrations.Count, 1, 0);
    }

    private static string GetFaixa(char letter) => letter switch
    {
        >= 'A' and <= 'D' => "A-D",
        >= 'E' and <= 'H' => "E-H",
        >= 'I' and <= 'L' => "I-L",
        _ => "M-Z"
    };

    private ReportPayload CreateEmptyPayload(ReportContext ctx)
    {
        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Check-in",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: ctx.RetreatId,
            RetreatName: ctx.RetreatName
        );

        var columns = new[] { new ColumnDef("message", "Mensagem") };
        var data = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["message"] = "Nenhum participante confirmado encontrado" }
        };

        var summary = new Dictionary<string, object?>
        {
            ["totalParticipants"] = 0,
            ["faixaAD"] = 0,
            ["faixaEH"] = 0,
            ["faixaIL"] = 0,
            ["faixaMZ"] = 0
        };

        return new ReportPayload(header, columns, data, summary, 0, 1, 0);
    }

    
    private sealed record ParticipantRow(Guid Id, string Name);
}
