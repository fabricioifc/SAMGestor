using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Infrastructure.Services;

/// <summary>
/// Serviço de exportação de relatórios (CSV e PDF).
/// Apenas CSV e PDF são suportados. XLSX será implementado futuramente.
/// </summary>

public sealed class ReportExportService : IReportExporter
{
    public Task<(string ContentType, string FileName, byte[] Bytes)> ExportAsync(
        ReportPayload payload,
        string format,
        string? fileNameBase = null,
        CancellationToken ct = default)
    {
        fileNameBase ??= SanitizeFileName(payload.report.Title ?? "report");
        var ext = format.ToLowerInvariant();

        return ext switch
        {
            "csv" => Task.FromResult(ExportCsv(payload, fileNameBase)),
            "pdf" => Task.FromResult(ExportPdf(payload, fileNameBase)),
            "xlsx" => throw new NotSupportedException("XLSX será implementado futuramente (ClosedXML)."),
            _ => throw new ArgumentException($"Formato '{format}' inválido. Use: csv, pdf")
        };
    }

    // ========== CSV (Genérico - Para todos os templates) ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportCsv(
        ReportPayload payload,
        string name)
    {
        var sb = new StringBuilder();

        var headers = payload.columns.Select(c => EscapeCsv(c.Label));
        sb.AppendLine(string.Join(",", headers.ToArray()));

        foreach (var row in payload.data)
        {
            var values = payload.columns.Select(c =>
            {
                row.TryGetValue(c.Key, out var val);
                return EscapeCsv(FormatValue(val));
            });
            sb.AppendLine(string.Join(",", values.ToArray()));
        }

        if (payload.summary?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Resumo,Valor");
            foreach (var kv in payload.summary)
                sb.AppendLine($"{EscapeCsv(kv.Key)},{EscapeCsv(FormatValue(kv.Value))}");
        }

        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        return ("text/csv; charset=utf-8", $"{name}.csv", bytes);
    }

    // ========== PDF (Dispatcher por Template) ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdf(
        ReportPayload payload,
        string name)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        byte[]? logoPng = null;
        try
        {
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "images", "logo.png");
            if (File.Exists(logoPath))
                logoPng = File.ReadAllBytes(logoPath);
        }
        catch { }

        return payload.report.TemplateKey switch
        {
            "people-epitaph" => ExportPdfEpitaph(payload, name, logoPng),
            "contemplated-participants" => ExportPdfContemplatedParticipants(payload, name, logoPng),
            "tents-allocation" => ExportPdfTentAllocation(payload, name, logoPng),
            "rahamistas-per-familia" => ExportPdfRahamistasPerFamilia(payload, name, logoPng),
            "check-in-bota-fora" => ExportPdfCheckInBotaFora(payload, name, logoPng),
            "wellness-per-family" => ExportPdfWellnessPerFamily(payload, name, logoPng),
            "carta-five-minutes" => ExportPdfCartaFiveMinutes(payload, name),
            "tape-names" => ExportPdfTapeNames(payload, name),
            "bags-distribution" => ExportPdfBagsDistribution(payload, name),
            "shirts-by-size" => ExportPdfShirtsBySize(payload, name),
            _ => ExportPdfGeneric(payload, name)
        };
    }

    // ========== PDF CUSTOMIZADO: Lápides dos Participantes ==========
 private static (string ContentType, string FileName, byte[] Bytes) ExportPdfEpitaph(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Lápides dos Participantes";
    var retreatName = payload.report.RetreatName ?? "";
    int totalPages = 0;

    var pdfBytes = Document.Create(doc =>
    {
        // Agrupar por família
        var groupedByFamily = payload.data
            .GroupBy(d => GetStringValue(d, "familyName", "Sem Família"))
            .OrderBy(g => ExtractFamilyOrderKey(g.Key))
            .ToList();

        int pageCount = 0;

        // Cada família em sua própria página (ou múltiplas se não couber)
        foreach (var familyGroup in groupedByFamily)
        {
            var familyName = familyGroup.Key;
            var familyColor = GetStringValue(familyGroup.First(), "familyColor", "#CCCCCC");
            var epitaphs = familyGroup.ToList();

            // Calcular quantas páginas precisa para esta família
            var epitaphsPerPage = 8; 
            var familyPages = (epitaphs.Count + epitaphsPerPage - 1) / epitaphsPerPage;

            for (int familyPageNum = 0; familyPageNum < familyPages; familyPageNum++)
            {
                var pageEpitaphs = epitaphs
                    .Skip(familyPageNum * epitaphsPerPage)
                    .Take(epitaphsPerPage)
                    .ToList();

                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ===== HEADER PADRÃO =====
                    page.Header().Column(col =>
                    {
                        // Linha 1: Informações à esquerda + Logo à direita
                        col.Item()
                            .Row(row =>
                            {
                                // ESQUERDA - Título e informações
                                row.RelativeColumn(2)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text(title)
                                            .FontSize(13)
                                            .Bold();
                                        
                                        if (!string.IsNullOrWhiteSpace(retreatName))
                                            c.Item()
                                                .Text($"Retiro: {retreatName}")
                                                .FontSize(9)
                                                .FontColor("#5C5C5C");
                                        
                                        c.Item()
                                            .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                            .FontSize(8)
                                            .FontColor("#999999");
                                    });

                                // DIREITA - Logo
                                if (logoPng != null)
                                {
                                    row.RelativeColumn(1)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .AlignRight()
                                                .Height(45)
                                                .Image(logoPng);
                                            
                                            c.Item()
                                                .AlignRight()
                                                .PaddingTop(3)
                                                .Text("Comunidade Servos")
                                                .FontSize(6.5f)
                                                .Bold()
                                                .FontColor("#6D4C41");
                                            
                                            c.Item()
                                                .AlignRight()
                                                .Text("Adoradores da Misericórdia")
                                                .FontSize(6.5f)
                                                .FontColor("#8D6E63");
                                        });
                                }
                            });
                        
                        // Linha separadora
                        col.Item()
                            .PaddingTop(8)
                            .PaddingBottom(8)
                            .BorderBottom(1)
                            .BorderColor("#DDDDDD");
                        
                        // Título da família
                        col.Item()
                            .Background(Color.FromHex(familyColor))
                            .Padding(6)
                            .AlignCenter()
                            .Text(familyName)
                            .FontSize(11)
                            .SemiBold()
                            .FontColor(Colors.White);
                    });

                    // ===== CONTEÚDO =====
                    page.Content().Column(content =>
                    {
                        // Grid 2x4 - lotando a página na vertical
                        for (int i = 0; i < pageEpitaphs.Count; i += 2) // 2 por linha
                        {
                            content.Item().Row(row =>
                            {
                                // Primeira lápide
                                row.RelativeColumn().Padding(5).Element(container => 
                                    RenderEpitaph(container, pageEpitaphs[i], familyColor));

                                // Segunda lápide (se existir)
                                if (i + 1 < pageEpitaphs.Count)
                                {
                                    row.RelativeColumn().Padding(5).Element(container => 
                                        RenderEpitaph(container, pageEpitaphs[i + 1], familyColor));
                                }
                                else
                                {
                                    row.RelativeColumn();
                                }
                            });
                        }
                    });

                    // ===== FOOTER PADRÃO =====
                    page.Footer().Column(col =>
                    {
                        // Linha separadora
                        col.Item()
                            .PaddingTop(8)
                            .PaddingBottom(8)
                            .BorderTop(1)
                            .BorderColor("#DDDDDD");

                        // Conteúdo do footer
                        col.Item()
                            .Row(row =>
                            {
                                // Esquerda - Sistema
                                row.RelativeColumn(2)
                                    .AlignLeft()
                                    .Text("SAMGestor - Lápides dos Participantes")
                                    .FontSize(8)
                                    .FontColor("#666666");

                                // Direita - Paginação
                                row.RelativeColumn(1)
                                    .AlignRight()
                                    .Text($"Página {pageCount + 1}")
                                    .FontSize(8)
                                    .FontColor("#666666");
                            });
                    });
                });

                pageCount++;
            }
        }

        totalPages = pageCount;

        // ===== PÁGINA FINAL: RESUMO =====
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(15);
            page.DefaultTextStyle(x => x.FontSize(9));

            // HEADER na página final também
            page.Header().Column(col =>
            {
                col.Item()
                    .Row(row =>
                    {
                        row.RelativeColumn(2)
                            .Column(c =>
                            {
                                c.Item()
                                    .Text(title)
                                    .FontSize(13)
                                    .Bold();
                                
                                if (!string.IsNullOrWhiteSpace(retreatName))
                                    c.Item()
                                        .Text($"Retiro: {retreatName}")
                                        .FontSize(9)
                                        .FontColor("#5C5C5C");
                                
                                c.Item()
                                    .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                    .FontSize(8)
                                    .FontColor("#999999");
                            });

                        if (logoPng != null)
                        {
                            row.RelativeColumn(1)
                                .Column(c =>
                                {
                                    c.Item()
                                        .AlignRight()
                                        .Height(45)
                                        .Image(logoPng);
                                    
                                    c.Item()
                                        .AlignRight()
                                        .PaddingTop(3)
                                        .Text("Comunidade Servos")
                                        .FontSize(6.5f)
                                        .Bold()
                                        .FontColor("#6D4C41");
                                    
                                    c.Item()
                                        .AlignRight()
                                        .Text("Adoradores da Misericórdia")
                                        .FontSize(6.5f)
                                        .FontColor("#8D6E63");
                                });
                        }
                    });
                
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderBottom(1)
                    .BorderColor("#DDDDDD");
            });

            page.Content().Column(col =>
            {
                col.Item().PaddingTop(20).Text("Resumo Geral").FontSize(13).Bold();

                col.Item().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Informação");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Total");
                    });

                    var totalParticipants = GetIntValue(payload.summary, "totalParticipants", 0);
                    var withFamily = GetIntValue(payload.summary, "withFamily", 0);
                    var withoutFamily = GetIntValue(payload.summary, "withoutFamily", 0);
                    var withPhoto = GetIntValue(payload.summary, "withPhoto", 0);

                    table.Cell().Element(DataCell).Text("Total de Participantes");
                    table.Cell().Element(DataCell).AlignCenter().Text(totalParticipants.ToString()).SemiBold();

                    table.Cell().Element(DataCell).Text("Com Família Atribuída");
                    table.Cell().Element(DataCell).AlignCenter().Text(withFamily.ToString());

                    table.Cell().Element(DataCell).Text("Sem Família Atribuída");
                    table.Cell().Element(DataCell).AlignCenter().Text(withoutFamily.ToString());

                    table.Cell().Element(DataCell).Text("Com Foto");
                    table.Cell().Element(DataCell).AlignCenter().Text(withPhoto.ToString());
                });

                col.Item().PaddingTop(20).Text($"Total de Páginas de Lápides: {totalPages}").FontSize(9).Italic().FontColor("#666666");
            });

            // FOOTER na página final também
            page.Footer().Column(col =>
            {
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderTop(1)
                    .BorderColor("#DDDDDD");

                col.Item()
                    .Row(row =>
                    {
                        row.RelativeColumn(2)
                            .AlignLeft()
                            .Text("SAMGestor - Lápides dos Participantes")
                            .FontSize(8)
                            .FontColor("#666666");

                        row.RelativeColumn(1)
                            .AlignRight()
                            .Text($"Página {totalPages + 1}")
                            .FontSize(8)
                            .FontColor("#666666");
                    });
            });
        });
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Lapides_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}

// Renderiza cada lápide 
private static void RenderEpitaph(IContainer container, IDictionary<string, object?> epitaph, string familyColorHex)
{
    var name = GetStringValue(epitaph, "name", "-");
    var birthDate = GetStringValue(epitaph, "birthDate", "-");
    var city = GetStringValue(epitaph, "city", "-");
    var photoStorageKey = GetStringValue(epitaph, "photoStorageKey", "");

    var familyColor = Color.FromHex(familyColorHex);

    container
        .Border(2)
        .BorderColor(familyColor)
        .Background(Colors.White)
        .Padding(8)
        .Column(col =>
        {
            // Foto centralizada
            col.Item()
                .PaddingVertical(6)
                .AlignCenter()
                .Width(90)
                .Height(90)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Element(photoContainer =>
                {
                    if (!string.IsNullOrWhiteSpace(photoStorageKey))
                    {
                        try
                        {
                            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                            var photoPath = Path.Combine(wwwrootPath, photoStorageKey.Replace("/", "\\"));

                            if (File.Exists(photoPath))
                            {
                                photoContainer.Image(photoPath).FitUnproportionally();
                            }
                            else
                            {
                                photoContainer
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text("Sem Foto")
                                    .FontSize(7)
                                    .FontColor(Colors.Grey.Darken2);
                            }
                        }
                        catch
                        {
                            photoContainer
                                .AlignCenter()
                                .AlignMiddle()
                                .Text("Sem Foto")
                                .FontSize(7)
                                .FontColor(Colors.Grey.Darken2);
                        }
                    }
                    else
                    {
                        photoContainer
                            .AlignCenter()
                            .AlignMiddle()
                            .Text("Sem Foto")
                            .FontSize(7)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });

            // Nome
            col.Item()
                .PaddingTop(4)
                .AlignCenter()
                .Text(name)
                .FontSize(9)
                .SemiBold();

            // Data de nascimento
            col.Item()
                .PaddingTop(1)
                .AlignCenter()
                .Text($"★ {birthDate}")
                .FontSize(7.5f)
                .FontColor(Colors.Grey.Darken1);

            // Cidade
            col.Item()
                .PaddingTop(1)
                .AlignCenter()
                .Text(city)
                .FontSize(7.5f)
                .FontColor(Colors.Grey.Darken1);
        });
}

// Método para ordenação
private static string ExtractFamilyOrderKey(string familyName)
{
    var match = System.Text.RegularExpressions.Regex.Match(familyName, @"(\d+)");
    if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
    {
        return $"A{number:D3}"; // A001, A002, etc (vem primeiro)
    }
    return $"B{familyName}"; // Nomes alfabéticos depois
}


    // ========== PDF CUSTOMIZADO: Participantes Contemplados ==========
 private static (string ContentType, string FileName, byte[] Bytes) ExportPdfContemplatedParticipants(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Participantes Contemplados";
    var retreatName = payload.report.RetreatName ?? "";
    int totalPages = 0;

    var pdfBytes = Document.Create(doc =>
    {
        var participantsPerPage = 6; 

        var totalPageCount = (payload.data.Count + participantsPerPage - 1) / participantsPerPage;
        totalPages = totalPageCount;

        // Páginas com participantes
        for (int pageNum = 0; pageNum < totalPageCount; pageNum++)
        {
            var pageParticipants = payload.data
                .Skip(pageNum * participantsPerPage)
                .Take(participantsPerPage)
                .ToList();

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ===== HEADER (SÓ NA PRIMEIRA PÁGINA) =====
                {
                    page.Header().Column(col =>
                    {
                        // Linha 1: Informações à esquerda + Logo à direita
                        col.Item()
                            .Row(row =>
                            {
                                // ESQUERDA - Título e informações
                                row.RelativeColumn(2)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text(title)
                                            .FontSize(13)
                                            .Bold();
                                        
                                        if (!string.IsNullOrWhiteSpace(retreatName))
                                            c.Item()
                                                .Text($"Retiro: {retreatName}")
                                                .FontSize(9)
                                                .FontColor("#5C5C5C");
                                        
                                        c.Item()
                                            .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                            .FontSize(8)
                                            .FontColor("#999999");
                                    });

                                // DIREITA - Logo
                                if (logoPng != null)
                                {
                                    row.RelativeColumn(1)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .AlignRight()
                                                .Height(45)
                                                .Image(logoPng);
                                            
                                            c.Item()
                                                .AlignRight()
                                                .PaddingTop(3)
                                                .Text("Comunidade Servos")
                                                .FontSize(6.5f)
                                                .Bold()
                                                .FontColor("#6D4C41");
                                            
                                            c.Item()
                                                .AlignRight()
                                                .Text("Adoradores da Misericórdia")
                                                .FontSize(6.5f)
                                                .FontColor("#8D6E63");
                                        });
                                }
                            });
                        
                        // Linha separadora
                        col.Item()
                            .PaddingTop(8)
                            .PaddingBottom(8)
                            .BorderBottom(1)
                            .BorderColor("#DDDDDD");
                    });
                }

                // ===== CONTEÚDO =====
                page.Content().Column(col =>
                {
                    foreach (var participant in pageParticipants)
                    {
                        var name_val = GetStringValue(participant, "name", "-");
                        var age = GetStringValue(participant, "age", "-");
                        var city = GetStringValue(participant, "city", "-");
                        var phone = GetStringValue(participant, "phone", "-");
                        var email = GetStringValue(participant, "email", "-");
                        var shirtSize = GetStringValue(participant, "shirtSize", "-");
                        var weight = GetStringValue(participant, "weight", "-");
                        var height = GetStringValue(participant, "height", "-");
                        var profession = GetStringValue(participant, "profession", "-");
                        var instagram = GetStringValue(participant, "instagram", "-");
                        var religion = GetStringValue(participant, "religion", "-");
                        var cpf = GetStringValue(participant, "cpf", "-");
                        var status = GetStringValue(participant, "status", "-");
                        var photoUrl = GetStringValue(participant, "photoUrl", "");
                        var photoStorageKey = GetStringValue(participant, "photoStorageKey", "");

                        // COR DO STATUS
                        var (statusBgColor, statusTextColor) = GetStatusColor(status);

                        col.Item().PaddingBottom(12).Row(row =>
                        {
                            // FOTO (quadrado 90x90)
                            row.ConstantColumn(100).Column(photoCol =>
                            {
                                var photoContainer = photoCol.Item()
                                    .Width(90)
                                    .Height(90)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2);

                                if (!string.IsNullOrWhiteSpace(photoStorageKey))
                                {
                                    try
                                    {
                                        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                                        var photoPath = Path.Combine(wwwrootPath, photoStorageKey.Replace("/", "\\"));

                                        if (File.Exists(photoPath))
                                        {
                                            photoContainer.Image(photoPath).FitUnproportionally();
                                        }
                                        else
                                        {
                                            photoContainer
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text("Sem Foto")
                                                .FontSize(7)
                                                .FontColor(Colors.Grey.Darken2);
                                        }
                                    }
                                    catch
                                    {
                                        photoContainer
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text("Sem Foto")
                                            .FontSize(7)
                                            .FontColor(Colors.Grey.Darken2);
                                    }
                                }
                                else
                                {
                                    photoContainer
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text("Sem Foto")
                                        .FontSize(7)
                                        .FontColor(Colors.Grey.Darken2);
                                }
                            });

                            // INFORMAÇÕES
                            row.RelativeColumn().Column(infoCol =>
                            {
                                infoCol.Item()
                                    .Height(90)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(8)
                                    .Column(details =>
                                    {
                                        // Nome + Status (com cor de fundo)
                                        details.Item().Row(nameRow =>
                                        {
                                            nameRow.RelativeColumn()
                                                .Text(name_val)
                                                .FontSize(11)
                                                .SemiBold();

                                            nameRow.AutoItem()
                                                .Background(statusBgColor)
                                                .PaddingVertical(2)
                                                .PaddingHorizontal(6)
                                                .Text(status)
                                                .FontSize(7)
                                                .FontColor(statusTextColor)
                                                .SemiBold();    
                                        });

                                        // Dados básicos
                                        details.Item()
                                            .PaddingTop(2)
                                            .Text($"Idade: {age} anos | Cidade: {city}")
                                            .FontSize(7.5f)
                                            .FontColor(Colors.Grey.Darken1);

                                        details.Item()
                                            .Text($"Tel: {phone} | Email: {email}")
                                            .FontSize(7.5f)
                                            .FontColor(Colors.Grey.Darken1);

                                        // Dados pessoais
                                        details.Item()
                                            .Text($"CPF: {cpf} | Religião: {religion}")
                                            .FontSize(7.5f)
                                            .FontColor(Colors.Grey.Darken1);

                                        // Dados físicos
                                        details.Item()
                                            .Text($"Camiseta: {shirtSize} | Peso: {weight} | Altura: {height}")
                                            .FontSize(7.5f)
                                            .FontColor(Colors.Grey.Darken1);

                                        // Profissão e Instagram
                                        details.Item()
                                            .Text($"Profissão: {profession} | Instagram: {instagram}")
                                            .FontSize(7.5f)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                            });
                        });
                    }
                });

                // ===== FOOTER (EM TODAS AS PÁGINAS) =====
                page.Footer().Column(col =>
                {
                    // Linha separadora
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(8)
                        .BorderTop(1)
                        .BorderColor("#DDDDDD");

                    // Conteúdo do footer
                    col.Item()
                        .Row(row =>
                        {
                            // Esquerda - Sistema
                            row.RelativeColumn(2)
                                .AlignLeft()
                                .Text("SAMGestor - Participantes Contemplados")
                                .FontSize(8)
                                .FontColor("#666666");

                            // Direita - Paginação
                            row.RelativeColumn(1)
                                .AlignRight()
                                .Text($"Página {pageNum + 1}")
                                .FontSize(8)
                                .FontColor("#666666");
                        });
                });
            });
        }

        // ===== PÁGINA FINAL: RESUMO =====
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(9));

            // HEADER na página final também (SÓ SE FOR A ÚNICA PÁGINA OU ÚLTIMA)
            page.Header().Column(col =>
            {
                col.Item()
                    .Row(row =>
                    {
                        row.RelativeColumn(2)
                            .Column(c =>
                            {
                                c.Item()
                                    .Text(title)
                                    .FontSize(13)
                                    .Bold();
                                
                                if (!string.IsNullOrWhiteSpace(retreatName))
                                    c.Item()
                                        .Text($"Retiro: {retreatName}")
                                        .FontSize(9)
                                        .FontColor("#5C5C5C");
                                
                                c.Item()
                                    .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                    .FontSize(8)
                                    .FontColor("#999999");
                            });

                        if (logoPng != null)
                        {
                            row.RelativeColumn(1)
                                .Column(c =>
                                {
                                    c.Item()
                                        .AlignRight()
                                        .Height(45)
                                        .Image(logoPng);
                                    
                                    c.Item()
                                        .AlignRight()
                                        .PaddingTop(3)
                                        .Text("Comunidade Servos")
                                        .FontSize(6.5f)
                                        .Bold()
                                        .FontColor("#6D4C41");
                                    
                                    c.Item()
                                        .AlignRight()
                                        .Text("Adoradores da Misericórdia")
                                        .FontSize(6.5f)
                                        .FontColor("#8D6E63");
                                });
                        }
                    });
                
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderBottom(1)
                    .BorderColor("#DDDDDD");
            });

            page.Content().Column(col =>
            {
                col.Item().PaddingTop(20).Text("Resumo Geral").FontSize(13).Bold();

                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Status");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Total");
                    });

                    var totalParticipants = GetIntValue(payload.summary, "totalParticipants", 0);
                    var selected = GetIntValue(payload.summary, "selected", 0);
                    var pendingPayment = GetIntValue(payload.summary, "pendingPayment", 0);
                    var confirmed = GetIntValue(payload.summary, "confirmed", 0);
                    var canceled = GetIntValue(payload.summary, "canceled", 0);

                    // Total geral
                    table.Cell().Element(DataCell).Text("Total de Participantes").SemiBold();
                    table.Cell().Element(DataCell).AlignCenter().Text(totalParticipants.ToString()).SemiBold();

                    // Contemplados (selecionados)
                    var selectedCell = table.Cell().Element(DataCell);
                    selectedCell.Background(Color.FromHex("E3F2FD")).Text("Contemplados (Selecionados)");
                    table.Cell().Element(DataCell).Background(Color.FromHex("E3F2FD")).AlignCenter()
                        .Text(selected.ToString());

                    // Aguardando pagamento (mesmo que selecionados)
                    var pendingCell = table.Cell().Element(DataCell);
                    pendingCell.Text("Aguardando Pagamento");
                    table.Cell().Element(DataCell).AlignCenter().Text(pendingPayment.ToString());

                    // Confirmados (pagamento confirmado + confirmed)
                    var confirmedCell = table.Cell().Element(DataCell);
                    confirmedCell.Background(Color.FromHex("C8E6C9")).Text("Confirmados (Pagaram)");
                    table.Cell().Element(DataCell).Background(Color.FromHex("C8E6C9")).AlignCenter()
                        .Text(confirmed.ToString()).FontColor(Colors.Green.Darken2).SemiBold();

                    // Cancelados
                    var canceledCell = table.Cell().Element(DataCell);
                    canceledCell.Background(Color.FromHex("FFCDD2")).Text("Cancelados");
                    table.Cell().Element(DataCell).Background(Color.FromHex("FFCDD2")).AlignCenter()
                        .Text(canceled.ToString()).FontColor(Colors.Red.Darken2).SemiBold();
                }); 
            });

            // FOOTER na página final também
            page.Footer().Column(col =>
            {
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderTop(1)
                    .BorderColor("#DDDDDD");

                col.Item()
                    .Row(row =>
                    {
                        row.RelativeColumn(2)
                            .AlignLeft()
                            .Text("SAMGestor - Participantes Contemplados")
                            .FontSize(8)
                            .FontColor("#666666");

                        row.RelativeColumn(1)
                            .AlignRight()
                            .Text($"Página {totalPages + 1}")
                            .FontSize(8)
                            .FontColor("#666666");
                    });
            });
        });
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Contemplados_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}


// Método auxiliar para cores por status
private static (Color Background, Color TextColor) GetStatusColor(string status)
{
    return status switch
    {
        "Contemplado" => (Color.FromHex("E3F2FD"), Colors.Blue.Darken1), // Azul
        "Aguardando Pagamento" => (Color.FromHex("E3F2FD"), Colors.Blue.Darken1), // Azul
        "Pagamento Confirmado" => (Color.FromHex("C8E6C9"), Colors.Green.Darken2), // Verde
        "Confirmado" => (Color.FromHex("C8E6C9"), Colors.Green.Darken2), // Verde
        "Cancelado" => (Color.FromHex("FFCDD2"), Colors.Red.Darken2), // Vermelho
        _ => (Colors.White, Colors.Black)
    };
}


    // ========== PDF CUSTOMIZADO: Alocação de Barracas ==========
  private static (string ContentType, string FileName, byte[] Bytes) ExportPdfTentAllocation(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Alocação de Barracas";
    var retreatName = payload.report.RetreatName ?? "";
    var summary = payload.summary;

    // Agrupar dados por barraca
    var tentGroups = new Dictionary<string, List<IDictionary<string, object?>>>();
    var tentInfos = new Dictionary<string, (string Category, int Count)>();

    foreach (var row in payload.data)
    {
        var tentKey = GetStringValue(row, "tentNumber", "");
        if (string.IsNullOrWhiteSpace(tentKey))
            continue;

        if (!tentGroups.ContainsKey(tentKey))
        {
            tentGroups[tentKey] = new List<IDictionary<string, object?>>();
            var category = GetStringValue(row, "tentCategory", "");
            tentInfos[tentKey] = (category, 1);
        }

        tentGroups[tentKey].Add(row);
    }

    var pdfBytes = Document.Create(doc =>
    {
        var pageNum = 0;

        foreach (var tent in tentGroups.OrderBy(t => ExtractTentNumber(t.Key)))
        {
            var tentNumber = tent.Key;
            var participants = tent.Value;

            pageNum++;

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);

                // ===== HEADER PADRÃO COM LOGO =====
                page.Header().Column(col =>
                {
                    col.Item()
                        .Row(row =>
                        {
                            // ESQUERDA - Título e informações
                            row.RelativeColumn(2)
                                .Column(c =>
                                {
                                    c.Item()
                                        .Text(title)
                                        .FontSize(13)
                                        .Bold();
                                    
                                    if (!string.IsNullOrWhiteSpace(retreatName))
                                        c.Item()
                                            .Text($"Retiro: {retreatName}")
                                            .FontSize(9)
                                            .FontColor("#5C5C5C");
                                    
                                    c.Item()
                                        .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                        .FontSize(8)
                                        .FontColor("#999999");
                                });

                            // DIREITA - Logo
                            if (logoPng != null)
                            {
                                row.RelativeColumn(1)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .AlignRight()
                                            .Height(45)
                                            .Image(logoPng);
                                        
                                        c.Item()
                                            .AlignRight()
                                            .PaddingTop(3)
                                            .Text("Comunidade Servos")
                                            .FontSize(6.5f)
                                            .Bold()
                                            .FontColor("#6D4C41");
                                        
                                        c.Item()
                                            .AlignRight()
                                            .Text("Adoradores da Misericórdia")
                                            .FontSize(6.5f)
                                            .FontColor("#8D6E63");
                                    });
                            }
                        });
                    
                    // Linha separadora
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(8)
                        .BorderBottom(1)
                        .BorderColor("#DDDDDD");
                });

                // CONTEÚDO PRINCIPAL
                page.Content().Column(col =>
                {
                    // === CARD DE INFORMAÇÕES DA BARRACA (MAIS DISCRETO) ===
                    col.Item()
                        .PaddingBottom(15)
                        .Column(c =>
                        {
                            // Título "BARRACA" com barra fina
                            c.Item()
                                .BorderBottom(2)
                                .BorderColor("#2196F3")
                                .PaddingBottom(8)
                                .Row(row =>
                                {
                                    row.RelativeColumn()
                                        .Column(col2 =>
                                        {
                                            col2.Item()
                                                .Text("BARRACA")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor("#666666");
                                            col2.Item()
                                                .Text(tentNumber)
                                                .FontSize(22)
                                                .Bold()
                                                .FontColor("#1565C0");
                                        });

                                    row.RelativeColumn()
                                        .Column(col2 =>
                                        {
                                            col2.Item()
                                                .Text("CAPACIDADE")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor("#666666");
                                            col2.Item()
                                                .Text($"{participants.Count} pessoas")
                                                .FontSize(14)
                                                .Bold()
                                                .FontColor("#1565C0");
                                        });

                                    if (tentInfos.TryGetValue(tentNumber, out var info))
                                    {
                                        row.RelativeColumn()
                                            .Column(col2 =>
                                            {
                                                col2.Item()
                                                    .Text("TIPO")
                                                    .FontSize(8)
                                                    .Bold()
                                                    .FontColor("#666666");
                                                col2.Item()
                                                    .Text(info.Category)
                                                    .FontSize(11)
                                                    .FontColor("#333333");
                                            });
                                    }
                                });
                        });

                    // === TABELA DE PARTICIPANTES ===
                    col.Item()
                        .Column(table =>
                        {
                            // CABEÇALHO DA TABELA
                            table.Item()
                                .Background(Colors.Blue.Darken3)
                                .Padding(10)
                                .Row(row =>
                                {
                                    row.RelativeColumn(0.5f)
                                        .AlignCenter()
                                        .Text("#")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.White);

                                    row.RelativeColumn(3)
                                        .Text("NOME")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.White);

                                    row.RelativeColumn(1)
                                        .AlignCenter()
                                        .Text("SEXO")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.White);

                                    row.RelativeColumn(1.5f)
                                        .Text("FUNÇÃO")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.White);

                                    row.RelativeColumn(2)
                                        .Text("FAMÍLIA")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.White);
                                });

                            // LINHAS DE PARTICIPANTES
                            var rowIndex = 0;
                            foreach (var participant in participants)
                            {
                                var rowNum = rowIndex + 1;
                                var name = GetStringValue(participant, "participantName", "-");
                                var gender = GetStringValue(participant, "gender", "-");
                                var role = GetStringValue(participant, "role", "Rahamista");
                                var family = GetStringValue(participant, "familyName", "Sem Família");
                                var familyColor = GetStringValue(participant, "familyColor", "#CCCCCC");

                                var isAlternate = rowIndex % 2 == 1;
                                var backgroundColor = isAlternate ? Colors.Grey.Lighten3 : Colors.White;

                                table.Item()
                                    .Background(backgroundColor)
                                    .Padding(8)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Row(row =>
                                    {
                                        row.RelativeColumn(0.5f)
                                            .AlignCenter()
                                            .Text(rowNum.ToString())
                                            .FontSize(10)
                                            .Bold()
                                            .FontColor(Colors.Grey.Darken2);

                                        row.RelativeColumn(3)
                                            .Text(name)
                                            .FontSize(11);

                                        row.RelativeColumn(1)
                                            .AlignCenter()
                                            .Text(gender)
                                            .FontSize(10)
                                            .FontColor(gender == "M" ? Colors.Blue.Darken1 : Colors.Pink.Darken1)
                                            .Bold();

                                        row.RelativeColumn(1.5f)
                                            .Text(GetRoleLabel(role))
                                            .FontSize(9)
                                            .FontColor(GetRoleColor(role));

                                        row.RelativeColumn(2)
                                            .Background(HexToRgbColor(familyColor))
                                            .Padding(4)
                                            .Text(family)
                                            .FontSize(9)
                                            .Bold()
                                            .FontColor(IsLightColor(familyColor) ? Colors.Black : Colors.White);
                                    });

                                rowIndex++;
                            }
                        });
                });

                // ===== FOOTER PADRÃO =====
                page.Footer().Column(col =>
                {
                    // Linha separadora
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(8)
                        .BorderTop(1)
                        .BorderColor("#DDDDDD");

                    // Conteúdo do footer
                    col.Item()
                        .Row(row =>
                        {
                            // Esquerda - Sistema
                            row.RelativeColumn(2)
                                .AlignLeft()
                                .Text("SAMGestor - Alocação de Barracas")
                                .FontSize(8)
                                .FontColor("#666666");

                            // Direita - Paginação
                            row.RelativeColumn(1)
                                .AlignRight()
                                .Text($"Página {pageNum}")
                                .FontSize(8)
                                .FontColor("#666666");
                        });
                });
            });
        }
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Barracas_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}

   
// HELPER METHODS
   
private static Color HexToRgbColor(string hex)
{
    try
    {
        hex = hex.Replace("#", "");
        var r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
       
        uint colorValue = (uint)((r << 16) | (g << 8) | b);
        return new Color(colorValue);
    }
    catch
    {
        return Colors.Grey.Lighten3;
    }
}

   private static int ExtractTentNumber(string tentKey)
{
    var numStr = tentKey.Replace("#", "").Trim();
    return int.TryParse(numStr, out var num) ? num : int.MaxValue;
}

private static string GetRoleLabel(string role)
{
    return role switch
    {
        "Rahamista" => "🏕️ Rahamista",
        "Padrinho" => "👨 Padrinho",
        "Madrinha" => "👩 Madrinha",
        _ => role
    };
}

private static Color GetRoleColor(string role)
{
    return role switch
    {
        "Rahamista" => Colors.Grey.Darken2,
        "Padrinho" => Colors.Blue.Darken2,
        "Madrinha" => Colors.Pink.Darken2,
        _ => Colors.Grey.Darken1
    };
}

private static bool IsLightColor(string hexColor)
{
    try
    {
        var hex = hexColor.Replace("#", "");
        var r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.5;
    }
    catch
    {
        return false;
    }
}



    // ========== PDF CUSTOMIZADO: Rahamistas por Família ==========
 private static (string ContentType, string FileName, byte[] Bytes) ExportPdfRahamistasPerFamilia(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Rahamistas por Família";
    var retreatName = payload.report.RetreatName ?? "";

    var pdfBytes = Document.Create(doc =>
    {
        var families = payload.data.ToList();
        var totalFamilies = families.Count;
        var pageNum = 0;

        foreach (var family in families)
        {
            pageNum++;
            var familyName = GetStringValue(family, "familyName", "Sem Nome");
            var familyColor = GetStringValue(family, "familyColor", "#CCCCCC");
            var totalMembers = GetIntValue(family, "totalMembers", 0);
            var shirtSummary = GetStringValue(family, "shirtSummary", "");

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ===== HEADER PADRÃO COM LOGO =====
                page.Header().Column(col =>
                {
                    col.Item()
                        .Row(row =>
                        {
                            // ESQUERDA - Título e informações
                            row.RelativeColumn(2)
                                .Column(c =>
                                {
                                    c.Item()
                                        .Text(title)
                                        .FontSize(13)
                                        .Bold();
                                    
                                    if (!string.IsNullOrWhiteSpace(retreatName))
                                        c.Item()
                                            .Text($"Retiro: {retreatName}")
                                            .FontSize(9)
                                            .FontColor("#5C5C5C");
                                    
                                    c.Item()
                                        .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                        .FontSize(8)
                                        .FontColor("#999999");
                                });

                            // DIREITA - Logo
                            if (logoPng != null)
                            {
                                row.RelativeColumn(1)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .AlignRight()
                                            .Height(45)
                                            .Image(logoPng);
                                        
                                        c.Item()
                                            .AlignRight()
                                            .PaddingTop(3)
                                            .Text("Comunidade Servos")
                                            .FontSize(6.5f)
                                            .Bold()
                                            .FontColor("#6D4C41");
                                        
                                        c.Item()
                                            .AlignRight()
                                            .Text("Adoradores da Misericórdia")
                                            .FontSize(6.5f)
                                            .FontColor("#8D6E63");
                                    });
                            }
                        });
                    
                    // Linha separadora
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(8)
                        .BorderBottom(1)
                        .BorderColor("#DDDDDD");
                });

                page.Content().Column(col =>
                {
                    // ===== HEADER DA FAMÍLIA =====
                    col.Item().PaddingTop(10).PaddingBottom(15)
                        .Background(familyColor)
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeColumn().Text(familyName)
                                .FontSize(15)
                                .SemiBold()
                                .FontColor(GetContrastColor(familyColor));
        
                            row.RelativeColumn().AlignRight().Text($"{totalMembers} membros")
                                .FontSize(12)
                                .FontColor(GetContrastColor(familyColor));
                        });

                    // ===== TABELA DE PADRINHOS/MADRINHAS =====
                    var padrinhos = GetArrayValue(family, "padrinhos");
                    if (padrinhos.Any())
                    {
                        col.Item().PaddingTop(10).Text("Padrinhos/Madrinhas")
                            .FontSize(11).SemiBold();
                        
                        RenderMembersTable(col.Item(), padrinhos);
                    }

                    // ===== TABELA DE RAHAMISTAS MULHERES =====
                    var mulheres = GetArrayValue(family, "mulheres");
                    if (mulheres.Any())
                    {
                        col.Item().PaddingTop(12).Text("Rahamistas Mulheres")
                            .FontSize(11).SemiBold();
                        
                        RenderMembersTable(col.Item(), mulheres);
                    }

                    // ===== TABELA DE RAHAMISTAS HOMENS =====
                    var homens = GetArrayValue(family, "homens");
                    if (homens.Any())
                    {
                        col.Item().PaddingTop(12).Text("Rahamistas Homens")
                            .FontSize(11).SemiBold();
                        
                        RenderMembersTable(col.Item(), homens);
                    }

                    // Resumo da família
                    if (!string.IsNullOrEmpty(shirtSummary))
                    {
                        col.Item().PaddingTop(12).Text($"Camisetas: {shirtSummary}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });

                // ===== FOOTER PADRÃO =====
                page.Footer().Column(col =>
                {
                    // Linha separadora
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(8)
                        .BorderTop(1)
                        .BorderColor("#DDDDDD");

                    // Conteúdo do footer
                    col.Item()
                        .Row(row =>
                        {
                            // Esquerda - Sistema
                            row.RelativeColumn(2)
                                .AlignLeft()
                                .Text("SAMGestor - Rahamistas por Família")
                                .FontSize(8)
                                .FontColor("#666666");

                            // Direita - Paginação
                            row.RelativeColumn(1)
                                .AlignRight()
                                .Text($"Página {pageNum}")
                                .FontSize(8)
                                .FontColor("#666666");
                        });
                });
            });
        }

        // ===== PÁGINA FINAL COM RESUMO =====
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(9));

            // ===== HEADER PADRÃO COM LOGO =====
            page.Header().Column(col =>
            {
                col.Item()
                    .Row(row =>
                    {
                        // ESQUERDA - Título e informações
                        row.RelativeColumn(2)
                            .Column(c =>
                            {
                                c.Item()
                                    .Text(title)
                                    .FontSize(13)
                                    .Bold();
                                
                                if (!string.IsNullOrWhiteSpace(retreatName))
                                    c.Item()
                                        .Text($"Retiro: {retreatName}")
                                        .FontSize(9)
                                        .FontColor("#5C5C5C");
                                
                                c.Item()
                                    .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                    .FontSize(8)
                                    .FontColor("#999999");
                            });

                        // DIREITA - Logo
                        if (logoPng != null)
                        {
                            row.RelativeColumn(1)
                                .Column(c =>
                                {
                                    c.Item()
                                        .AlignRight()
                                        .Height(45)
                                        .Image(logoPng);
                                    
                                    c.Item()
                                        .AlignRight()
                                        .PaddingTop(3)
                                        .Text("Comunidade Servos")
                                        .FontSize(6.5f)
                                        .Bold()
                                        .FontColor("#6D4C41");
                                    
                                    c.Item()
                                        .AlignRight()
                                        .Text("Adoradores da Misericórdia")
                                        .FontSize(6.5f)
                                        .FontColor("#8D6E63");
                                });
                        }
                    });
                
                // Linha separadora
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderBottom(1)
                    .BorderColor("#DDDDDD");
            });

            page.Content().Column(col =>
            {
                col.Item().PaddingTop(80).AlignCenter()
                    .Text("Resumo Geral")
                    .FontSize(20)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken3);

                col.Item().PaddingTop(5).AlignCenter()
                    .LineHorizontal(2)
                    .LineColor(Colors.Grey.Lighten1);

                // Card de resumo
                col.Item().PaddingTop(50).PaddingLeft(100).PaddingRight(100)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten4)
                    .Padding(30)
                    .Column(cardCol =>
                    {
                        cardCol.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("Total de Famílias")
                                .FontSize(13)
                                .FontColor(Colors.Grey.Darken2);
                            
                            row.ConstantColumn(80).AlignRight().Text($"{GetSummaryValue(payload.summary, "totalFamilies")}")
                                .FontSize(24)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);
                        });

                        cardCol.Item().PaddingTop(15).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        cardCol.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeColumn().Text("Total de Participantes")
                                .FontSize(13)
                                .FontColor(Colors.Grey.Darken2);
                            
                            row.ConstantColumn(80).AlignRight().Text($"{GetSummaryValue(payload.summary, "totalParticipants")}")
                                .FontSize(24)
                                .SemiBold()
                                .FontColor(Colors.Green.Darken2);
                        });

                        cardCol.Item().PaddingTop(15).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                        cardCol.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeColumn().Text("Média por Família")
                                .FontSize(13)
                                .FontColor(Colors.Grey.Darken2);
                            
                            var totalFamiliesVal = Convert.ToInt32(GetSummaryValue(payload.summary, "totalFamilies"));
                            var totalParticipants = Convert.ToInt32(GetSummaryValue(payload.summary, "totalParticipants"));
                            var average = totalFamiliesVal > 0 ? (double)totalParticipants / totalFamiliesVal : 0;
                            
                            row.ConstantColumn(80).AlignRight().Text($"{average:F1}")
                                .FontSize(24)
                                .SemiBold()
                                .FontColor(Colors.Orange.Darken2);
                        });
                    });

                col.Item().PaddingTop(60).AlignCenter()
                    .Text($"Gerado em: {DateTime.Now:dd/MM/yyyy 'às' HH:mm}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });

            // ===== FOOTER PADRÃO =====
            page.Footer().Column(col =>
            {
                // Linha separadora
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderTop(1)
                    .BorderColor("#DDDDDD");

                // Conteúdo do footer
                col.Item()
                    .Row(row =>
                    {
                        // Esquerda - Sistema
                        row.RelativeColumn(2)
                            .AlignLeft()
                            .Text("SAMGestor - Rahamistas por Família")
                            .FontSize(8)
                            .FontColor("#666666");

                        // Direita - Paginação
                        row.RelativeColumn(1)
                            .AlignRight()
                            .Text($"Página {totalFamilies + 1}")
                            .FontSize(8)
                            .FontColor("#666666");
                    });
            });
        });

    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Rahamistas_{retreatNameSafe}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}


private static void RenderMembersTable(IContainer container, List<IDictionary<string, object?>> members)
{
    container.PaddingTop(5).Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(25);  // #
            columns.RelativeColumn(3);   // Nome
            columns.ConstantColumn(35);  // Idade
            columns.ConstantColumn(40);  // Peso
            columns.ConstantColumn(45);  // Altura
            columns.ConstantColumn(40);  // Camiseta
            columns.RelativeColumn(2);   // Cidade
            columns.RelativeColumn(1);   // Pagamento
        });

        table.Header(header =>
        {
            header.Cell().Element(HeaderStyle).Text("#");
            header.Cell().Element(HeaderStyle).Text("Nome");
            header.Cell().Element(HeaderStyle).Text("Idade");
            header.Cell().Element(HeaderStyle).Text("Peso");
            header.Cell().Element(HeaderStyle).Text("Altura");
            header.Cell().Element(HeaderStyle).Text("Tam");
            header.Cell().Element(HeaderStyle).Text("Cidade");
            header.Cell().Element(HeaderStyle).Text("Pgto");
        });

        int index = 1;
        foreach (var member in members)
        {
            table.Cell().Element(DataCell).Text(index.ToString()).AlignCenter();
            table.Cell().Element(DataCell).Text(GetStringValue(member, "name"));
            table.Cell().Element(DataCell).Text(GetIntValue(member, "idade").ToString()).AlignCenter();
            table.Cell().Element(DataCell).Text(GetStringValue(member, "peso")).AlignCenter();
            table.Cell().Element(DataCell).Text(GetStringValue(member, "altura")).AlignCenter();
            table.Cell().Element(DataCell).Text(GetStringValue(member, "shirtSize")).AlignCenter();
            table.Cell().Element(DataCell).Text(GetStringValue(member, "city"));
            table.Cell().Element(DataCell).Text(GetStringValue(member, "paymentStatus")).FontSize(7).AlignCenter();
            
            index++;
        }
    });
}

private static List<IDictionary<string, object?>> GetArrayValue(IDictionary<string, object?> dict, string key)
{
    if (dict.TryGetValue(key, out var value) && value is IEnumerable<object> enumerable)
    {
        return enumerable
            .Cast<IDictionary<string, object?>>()
            .ToList();
    }
    return new List<IDictionary<string, object?>>();
}

private static int GetIntValue(IDictionary<string, object?> dict, string key, int defaultValue = 0)
{
    if (dict.TryGetValue(key, out var value))
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var result) => result,
            _ => defaultValue
        };
    }
    return defaultValue;
}

private static string GetSummaryValue(IDictionary<string, object?>? summary, string key)
{
    if (summary?.TryGetValue(key, out var value) == true)
        return value?.ToString() ?? "0";
    return "0";
}



    // ========== PDF CUSTOMIZADO: Check-in Bota Fora ==========
private static (string ContentType, string FileName, byte[] Bytes) ExportPdfCheckInBotaFora(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Bota Fora x Rahamivida";
    var retreatName = payload.report.RetreatName ?? "";
    
    // TODOS OS PARTICIPANTES SEM AGRUPAR POR FAIXA
    var allParticipants = payload.data
        .Where(row => !string.IsNullOrWhiteSpace(GetStringValue(row, "name")))
        .ToList();

    var pageNum = 0;
    var isFirstPage = true;
    const int maxRowsPerPage = 40; // LIMITE DE LINHAS POR PÁGINA

    var pdfBytes = Document.Create(doc =>
    {
        // Dividir em chunks de 41 linhas
        for (int i = 0; i < allParticipants.Count; i += maxRowsPerPage)
        {
            pageNum++;
            var chunk = allParticipants.Skip(i).Take(maxRowsPerPage).ToList();
            var showHeader = isFirstPage;
            
            doc.Page(BuildFaixaPage(
                chunk, 
                "", 
                title, 
                retreatName, 
                showHeader, 
                i + 1,  // Começa de 1 na primeira página, 42 na segunda, etc
                logoPng, 
                pageNum
            ));
            
            isFirstPage = false;
        }
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Bota_Fora_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}

private static Action<PageDescriptor> BuildFaixaPage(
    List<IDictionary<string, object?>> pageData,
    string faixa,
    string? title,
    string? retreatName,
    bool showHeader,
    int startRowNum,
    byte[]? logoPng,
    int pageNum)
{
    return page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(10);
        page.DefaultTextStyle(x => x.FontSize(8));

        // Header só na primeira página
        if (showHeader)
        {
            page.Header().Column(col =>
            {
                col.Item()
                    .Row(row =>
                    {
                        // ESQUERDA - Título e informações
                        row.RelativeColumn(2)
                            .Column(c =>
                            {
                                c.Item()
                                    .Text(title)
                                    .FontSize(13)
                                    .Bold();
                                
                                if (!string.IsNullOrWhiteSpace(retreatName))
                                    c.Item()
                                        .Text($"Retiro: {retreatName}")
                                        .FontSize(9)
                                        .FontColor("#5C5C5C");
                                
                                c.Item()
                                    .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                    .FontSize(8)
                                    .FontColor("#999999");
                            });

                        // DIREITA - Logo
                        if (logoPng != null)
                        {
                            row.RelativeColumn(1)
                                .Column(c =>
                                {
                                    c.Item()
                                        .AlignRight()
                                        .Height(45)
                                        .Image(logoPng);
                                    
                                    c.Item()
                                        .AlignRight()
                                        .PaddingTop(3)
                                        .Text("Comunidade Servos")
                                        .FontSize(6.5f)
                                        .Bold()
                                        .FontColor("#6D4C41");
                                    
                                    c.Item()
                                        .AlignRight()
                                        .Text("Adoradores da Misericórdia")
                                        .FontSize(6.5f)
                                        .FontColor("#8D6E63");
                                });
                        }
                    });
                
                // Linha separadora
                col.Item()
                    .PaddingTop(8)
                    .PaddingBottom(8)
                    .BorderBottom(1)
                    .BorderColor("#DDDDDD");
            });
        }

        page.Content().PaddingVertical(5).Column(col =>
        {
            col.Item().PaddingHorizontal(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);      // #
                    columns.RelativeColumn(1.8f);    // Nome (reduzido)
                    columns.ConstantColumn(25);      // Termo
                    columns.ConstantColumn(25);      // Cel
                    columns.ConstantColumn(25);      // Relig
                    columns.ConstantColumn(25);      // Rem
                    columns.ConstantColumn(25);      // Cart
                    columns.ConstantColumn(25);      // Bolsa
                    columns.ConstantColumn(25);      // Chave
                    columns.RelativeColumn(1.5f);    // Assinatura
                });

                table.Header(header =>
                {
                    var headerCells = new[] { "#", "Nome", "Termo", "Cel", "Relig", "Rem", "Cart", "Bolsa", "Chave", "Assinatura" };
                    foreach (var cell in headerCells)
                    {
                        header.Cell()
                            .Background(Colors.Grey.Darken4)
                            .Padding(2)
                            .Text(cell)
                            .FontColor(Colors.White)
                            .SemiBold()
                            .FontSize(7)
                            .AlignCenter();
                    }
                });

                var currentRowNum = startRowNum;
                foreach (var row in pageData)
                {
                    var participantName = GetStringValue(row, "name");

                    table.Cell().Element(DataCell).AlignCenter().Text(currentRowNum.ToString());
                    table.Cell().Element(DataCell).Text(participantName);

                    // Checkboxes: Termo, Cel, Relig, Rem, Cart, Bolsa, Chave
                    for (int i = 0; i < 7; i++)
                    {
                        table.Cell()
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(1)
                            .Height(12)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text("☐")
                            .FontSize(8);
                    }

                    // Coluna de Assinatura (linha)
                    table.Cell()
                        .Border(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(1)
                        .Height(12)
                        .AlignCenter();

                    currentRowNum++;
                }
            });
        });

        // ===== FOOTER PADRÃO EM TODAS AS PÁGINAS =====
        page.Footer().Column(col =>
        {
            // Linha separadora
            col.Item()
                .PaddingTop(8)
                .PaddingBottom(8)
                .BorderTop(1)
                .BorderColor("#DDDDDD");

            // Conteúdo do footer
            col.Item()
                .Row(row =>
                {
                    // Esquerda - Sistema
                    row.RelativeColumn(2)
                        .AlignLeft()
                        .Text("SAMGestor - Bota Fora x Rahamivida")
                        .FontSize(8)
                        .FontColor("#666666");

                    // Direita - Paginação
                    row.RelativeColumn(1)
                        .AlignRight()
                        .Text($"Página {pageNum}")
                        .FontSize(8)
                        .FontColor("#666666");
                });
        });
    };
}



    // ========== PDF CUSTOMIZADO: Bem-Estar por Família ==========
private static (string ContentType, string FileName, byte[] Bytes) ExportPdfWellnessPerFamily(
    ReportPayload payload,
    string name,
    byte[]? logoPng)
{
    var title = payload.report.Title ?? "Bem-Estar por Família";
    var retreatName = payload.report.RetreatName ?? "";
    var pageNum = 0;

    var pdfBytes = Document.Create(doc =>
    {
        foreach (var family in payload.data)
        {
            pageNum++;
            var familyName = GetStringValue(family, "familyName", "Sem Nome");
            var familyColor = GetStringValue(family, "familyColor", "#CCCCCC");
            var memberCount = GetIntValue(family, "memberCount", 0);

            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ===== HEADER PADRÃO COM LOGO EM TODAS AS PÁGINAS =====
                page.Header().Column(col =>
                {
                    col.Item()
                        .Row(row =>
                        {
                            // ESQUERDA - Título e informações
                            row.RelativeColumn(2)
                                .Column(c =>
                                {
                                    c.Item()
                                        .Text(title)
                                        .FontSize(13)
                                        .Bold();
                                    
                                    if (!string.IsNullOrWhiteSpace(retreatName))
                                        c.Item()
                                            .Text($"Retiro: {retreatName}")
                                            .FontSize(9)
                                            .FontColor("#5C5C5C");
                                    
                                    c.Item()
                                        .Text($"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}")
                                        .FontSize(8)
                                        .FontColor("#999999");
                                });

                            // DIREITA - Logo
                            if (logoPng != null)
                            {
                                row.RelativeColumn(1)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .AlignRight()
                                            .Height(40)
                                            .Image(logoPng);
                                        
                                        c.Item()
                                            .AlignRight()
                                            .PaddingTop(2)
                                            .Text("Comunidade Servos")
                                            .FontSize(6)
                                            .Bold()
                                            .FontColor("#6D4C41");
                                        
                                        c.Item()
                                            .AlignRight()
                                            .Text("Adoradores da Misericórdia")
                                            .FontSize(6)
                                            .FontColor("#8D6E63");
                                    });
                            }
                        });
                    
                    // Linha separadora
                    col.Item()
                        .PaddingTop(6)
                        .PaddingBottom(6)
                        .BorderBottom(1)
                        .BorderColor("#DDDDDD");
                });

                page.Content().Column(col =>
                {
                    // Header da família (REDUZIDO)
                    col.Item().PaddingTop(8).PaddingBottom(8)
                        .Background(familyColor)
                        .Padding(7)
                        .Row(row =>
                        {
                            row.RelativeColumn().Text(familyName)
                                .FontSize(12)
                                .SemiBold()
                                .FontColor(GetContrastColor(familyColor));
                            
                            row.RelativeColumn().AlignRight().Text($"{memberCount} membros")
                                .FontSize(9)
                                .FontColor(GetContrastColor(familyColor));
                        });

                    // Tabela com 8 campos vazios
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // #
                            columns.RelativeColumn(2.5f); // Rahamista
                            columns.RelativeColumn(3.5f); // Medicamento/Observações
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("#");
                            header.Cell().Element(HeaderStyle).Text("Rahamista");
                            header.Cell().Element(HeaderStyle).Text("Medicamento / Observações");
                        });

                        // 8 linhas vazias
                        for (int i = 1; i <= 8; i++)
                        {
                            table.Cell().Element(DataCell).Text(i.ToString()).AlignCenter();
                            table.Cell()
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Height(35)
                                .AlignTop()
                                .Text("");

                            table.Cell()
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Height(35)
                                .AlignTop()
                                .Text("");
                        }
                    });
                });

                // ===== FOOTER PADRÃO EM TODAS AS PÁGINAS =====
                page.Footer().Column(col =>
                {
                    // Linha separadora
                    col.Item()
                        .PaddingTop(6)
                        .PaddingBottom(6)
                        .BorderTop(1)
                        .BorderColor("#DDDDDD");

                    // Conteúdo do footer
                    col.Item()
                        .Row(row =>
                        {
                            // Esquerda - Sistema
                            row.RelativeColumn(2)
                                .AlignLeft()
                                .Text("SAMGestor - Bem-Estar por Família")
                                .FontSize(8)
                                .FontColor("#666666");

                            // Direita - Paginação
                            row.RelativeColumn(1)
                                .AlignRight()
                                .Text($"Página {pageNum}")
                                .FontSize(8)
                                .FontColor("#666666");
                        });
                });
            });
        }
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"BemEstar_{retreatNameSafe}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}

    
    // ========== PDF CUSTOMIZADO: Carta 5 Minutos ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfCartaFiveMinutes(
    ReportPayload payload,
    string name)
{
    var retreatName = payload.report.RetreatName ?? "";

    var pdfBytes = Document.Create(doc =>
    {
        foreach (var row in payload.data)
        {
            var participantName = GetStringValue(row, "name", "-");
            var familyColorHex = GetStringValue(row, "familyColor", "#CCCCCC");
            var familyColor = Color.FromHex(familyColorHex);
            var contrastColor = GetContrastColor(familyColorHex);

            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);

                page.Header()
                    .Height(80)
                    .Background(familyColor)
                    .PaddingVertical(20)  
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(participantName)
                    .FontSize(32)
                    .SemiBold()
                    .FontColor(contrastColor);
                
                page.Content(); 
            });
            };  
    }).GeneratePdf();

    var fileName = $"CartaFiveMinutes_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}

    
    // ========== PDF CUSTOMIZADO: Fitas (Lista de Nomes) ==========
 private static (string ContentType, string FileName, byte[] Bytes) ExportPdfTapeNames(
    ReportPayload payload,
    string name)
{
    var title = payload.report.Title ?? "Fitas";
    var retreatName = payload.report.RetreatName ?? "";

    var pdfBytes = Document.Create(doc =>
    {
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(11));

            // Header só na primeira página
            page.Header().Column(col =>
            {
                col.Item().Text(title).FontSize(16).SemiBold();
                if (!string.IsNullOrWhiteSpace(retreatName))
                    col.Item().Text(retreatName).FontSize(12).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                col.Item().PaddingBottom(15);
            });

            // Content: QuestPDF cuida das quebras automaticamente
            page.Content().Column(col =>
            {
                foreach (var row in payload.data)
                {
                    var participantName = GetStringValue(row, "name", "-");

                    // Quadrado com borda e nome centralizado
                    col.Item()
                        .Border(1)
                        .BorderColor(Colors.Grey.Darken1)
                        .Padding(10)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(participantName)
                        .FontSize(13);

                    // Espaçamento entre quadrados
                    col.Item().PaddingBottom(8);
                }
            });
        });
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Fitas_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}
  
  
    // ========== PDF CUSTOMIZADO: Bolsas ==========
 private static (string ContentType, string FileName, byte[] Bytes) ExportPdfBagsDistribution(
    ReportPayload payload,
    string name)
{
    var title = payload.report.Title ?? "Bolsas";
    var retreatName = payload.report.RetreatName ?? "";
    var totalParticipants = GetIntValue(payload.summary, "totalParticipants", 0);

    var colAData = payload.data
        .Select(d => GetStringValue(d, "columnA"))
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .ToList();

    var colBData = payload.data
        .Select(d => GetStringValue(d, "columnB"))
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .ToList();

    var pdfBytes = Document.Create(doc =>
    {
        // PRIMEIRA PÁGINA
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().Text(title).FontSize(16).SemiBold();
                if (!string.IsNullOrWhiteSpace(retreatName))
                    col.Item().Text(retreatName).FontSize(12).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                col.Item().PaddingVertical(10);
            });

            page.Content().Column(col =>
            {
                col.Item().Row(gridRow =>
                {
                    gridRow.RelativeColumn().Column(colA =>
                    {
                        foreach (var participantName in colAData)
                        {
                            colA.Item()
                                .Border(1)
                                .BorderColor(Colors.Black)
                                .Padding(12)
                                .Text(participantName)
                                .FontSize(10);
                        }
                    });

                    gridRow.RelativeColumn().Column(colB =>
                    {
                        foreach (var participantName in colBData)
                        {
                            colB.Item()
                                .Border(1)
                                .BorderColor(Colors.Black)
                                .Padding(12)
                                .Text(participantName)
                                .FontSize(10);
                        }
                    });
                });
            });

            page.Footer().AlignCenter().Column(col =>
            {
                col.Item().Text($"Total de Participantes: {totalParticipants}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken2);
            });
        });
    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Bolsas_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);
}



    // ========== PDF CUSTOMIZADO: Camisetas por Tamanho ==========
  private static (string ContentType, string FileName, byte[] Bytes) ExportPdfShirtsBySize(
    ReportPayload payload,
    string name)
{
    var title = payload.report.Title ?? "Camisetas por Tamanho";
    var retreatName = payload.report.RetreatName ?? "";

    var pdfBytes = Document.Create(doc =>
    {
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Text(title).FontSize(16).SemiBold();
                if (!string.IsNullOrWhiteSpace(retreatName))
                    col.Item().Text(retreatName).FontSize(11).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            page.Content().Column(col =>
            {
                // ===== TABELA =====
                col.Item().PaddingBottom(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // #
                        columns.RelativeColumn(3);   // Tamanho
                        columns.ConstantColumn(50);  // Quantidade
                        columns.ConstantColumn(60);  // Percentual
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("#");
                        header.Cell().Element(HeaderStyle).Text("Tamanho");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Qtd");
                        header.Cell().Element(HeaderStyle).AlignCenter().Text("Percentual");
                    });

                    var totalShirts = GetIntValue(payload.summary, "totalShirts", 0);
                    var index = 1;

                    foreach (var row in payload.data)
                    {
                        var count = GetIntValue(row, "count", 0);
                        var percentage = totalShirts > 0 ? (count * 100.0 / totalShirts) : 0;

                        table.Cell().Element(DataCell).Text(index.ToString()).AlignCenter();
                        table.Cell().Element(DataCell).Text(GetStringValue(row, "sizeLabel", ""));
                        table.Cell().Element(DataCell).Text(count.ToString()).AlignCenter();
                        table.Cell().Element(DataCell).Text($"{percentage:F1}%").AlignCenter();

                        index++;
                    }
                });
            });

            page.Footer().AlignCenter().Column(col =>
            {
                col.Item().Text($"Total de Participantes: {GetIntValue(payload.summary, "totalParticipants", 0)}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
    
                col.Item().Text($"Total de Camisetas: {GetIntValue(payload.summary, "totalShirts", 0)}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        });

    }).GeneratePdf();

    var retreatNameSafe = retreatName.Replace(" ", "_").Replace("[", "").Replace("]", "");
    var fileName = $"Camisetas_{retreatNameSafe}_{DateTime.Now:dd-MM-yyyy}.pdf";
    return ("application/pdf", fileName, pdfBytes);

}

  
    // ========== PDF GENÉRICO (Fallback) ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfGeneric(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Relatório";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).SemiBold();
                    if (!string.IsNullOrWhiteSpace(retreatName))
                        col.Item().Text(retreatName).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);
                            foreach (var _ in payload.columns)
                                c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("#");
                            foreach (var column in payload.columns)
                                h.Cell().Element(HeaderStyle).Text(column.Label);
                        });

                        var idx = 1;
                        foreach (var row in payload.data)
                        {
                            var bgColor = (idx % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell()
                                .Padding(4)
                                .Background(bgColor)
                                .AlignCenter()
                                .Text(idx.ToString());

                            foreach (var column in payload.columns)
                            {
                                row.TryGetValue(column.Key, out var val);
                                var formatted = FormatValue(val);

                                table.Cell()
                                    .Padding(4)
                                    .Background(bgColor)
                                    .Text(formatted);
                            }

                            idx++;
                        }
                    });

                    if (payload.summary?.Count > 0)
                    {
                        col.Item().PaddingTop(15).Text("Resumo").FontSize(12).SemiBold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderStyle).Text("Descrição");
                                h.Cell().Element(HeaderStyle).Text("Valor");
                            });

                            foreach (var kv in payload.summary)
                            {
                                table.Cell().Padding(4).Text(kv.Key);
                                table.Cell().Padding(4).Text(FormatValue(kv.Value));
                            }
                        });
                    }
                });

                page.Footer().AlignRight()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2))
                    .Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== HELPERS ==========
    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            DateTime dt => dt.ToString("dd/MM/yyyy"),
            bool b => b ? "Sim" : "Não",
            _ => value.ToString() ?? ""
        };
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static string SanitizeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
    }

    private static string GetStringValue(
        IDictionary<string, object?> row,
        string key,
        string defaultValue = "")
    {
        row.TryGetValue(key, out var value);
        return value?.ToString() ?? defaultValue;
    }

    private static string GetContrastColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor.Length < 7)
            return "#000000";

        try
        {
            var hex = hexColor.TrimStart('#');
            var r = Convert.ToInt32(hex.Substring(0, 2), 16);
            var g = Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = Convert.ToInt32(hex.Substring(4, 2), 16);

            var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            return luminance > 0.5 ? "#000000" : "#FFFFFF";
        }
        catch
        {
            return "#000000";
        }
    }

    // ========== REUSABLE STYLES ==========
    private static IContainer HeaderStyle(IContainer c) =>
        c.Padding(5)
            .Background(Colors.Grey.Darken3)
            .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(10));

    private static IContainer DataCell(IContainer c) =>
        c.Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(3)
            .DefaultTextStyle(x => x.FontSize(9));
}