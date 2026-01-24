using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

/// <summary>
/// Relatório de Check-in "Bota Fora x Rahamivida"
/// Usado na chegada do retiro para marcar itens recebidos/entregues.
/// Participantes confirmados e pagos, divididos em 4 tabelas por faixa alfabética.
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

        // 3. Agrupa por faixa alfabética
        var faixas = new Dictionary<string, List<CheckInParticipant>>
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

            faixas[faixa].Add(new CheckInParticipant
            {
                RegistrationId = reg.Id,
                Name = reg.Name
            });
        }
        
        var columns = new[]
        {
            new ColumnDef("faixa", "Faixa"),
            new ColumnDef("name", "Nome"),
            new ColumnDef("termo", "Termo"),
            new ColumnDef("celular", "Celular"),
            new ColumnDef("relogio", "Relogio"),
            new ColumnDef("remedio", "Remédio"),
            new ColumnDef("carteira", "Carteira"),
            new ColumnDef("bolsaMao", "Bolsa de Mão"),
            new ColumnDef("chave", "Chave"),
            new ColumnDef("dataNasc", "DataNasc."),
            new ColumnDef("assinatura", "Assinatura")
        };

        var data = new List<IDictionary<string, object?>>();

        foreach (var (faixaKey, participants) in faixas)
        {
            if (!participants.Any())
                continue;

            var isFirstInFaixa = true;

            foreach (var participant in participants)
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["faixa"] = isFirstInFaixa ? faixaKey : "",
                    ["name"] = participant.Name,
                    ["termo"] = "",     
                    ["celular"] = "",    
                    ["Relogio"] = "",   
                    ["remedio"] = "",   
                    ["carteira"] = "",   
                    ["bolsaMao"] = "",   
                    ["chave"] = "",      
                    ["dataNasc"] = "",   
                    ["assinatura"] = ""  
                });

                isFirstInFaixa = false;
            }
            
            if (faixas.Values.Any(f => f.Any()))
            {
                data.Add(new Dictionary<string, object?>
                {
                    ["faixa"] = "",
                    ["name"] = "",
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

        var totalParticipants = registrations.Count;

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
            ["totalParticipants"] = totalParticipants,
            ["faixaAD"] = faixas["A-D"].Count,
            ["faixaEH"] = faixas["E-H"].Count,
            ["faixaIL"] = faixas["I-L"].Count,
            ["faixaMZ"] = faixas["M-Z"].Count
        };

        return new ReportPayload(header, columns, data, summary, totalParticipants, 1, 0);
    }

    private string GetFaixa(char letter)
    {
        return letter switch
        {
            >= 'A' and <= 'D' => "A-D",
            >= 'E' and <= 'H' => "E-H",
            >= 'I' and <= 'L' => "I-L",
            >= 'M' and <= 'Z' => "M-Z",
            _ => "M-Z"
        };
    }

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
            new Dictionary<string, object?> { ["message"] = "Nenhum participante confirmado e pago encontrado" }
        };

        return new ReportPayload(header, columns, data, new Dictionary<string, object?>(), 0, 1, 0);
    }

    private class CheckInParticipant
    {
        public Guid RegistrationId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
