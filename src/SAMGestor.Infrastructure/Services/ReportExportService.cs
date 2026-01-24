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

        return payload.report.TemplateKey switch
        {
            "people-epitaph" => ExportPdfEpitaph(payload, name),
            "contemplated-participants" => ExportPdfContemplatedParticipants(payload, name),
            "tents-allocation" => ExportPdfTentAllocation(payload, name),
            "rahamistas-per-familia" => ExportPdfRahamistasPerFamilia(payload, name),
            "check-in-bota-fora" => ExportPdfCheckInBotaFora(payload, name),
            "wellness-per-family" => ExportPdfWellnessPerFamily(payload, name),
            "participant-individual-card" => ExportPdfParticipantIndividualCard(payload, name),
            "tape-names" => ExportPdfTapeNames(payload, name),
            "bags-distribution" => ExportPdfBagsDistribution(payload, name),
            "shirts-by-size" => ExportPdfShirtsBySize(payload, name),
            _ => ExportPdfGeneric(payload, name)
        };
    }

    // ========== PDF CUSTOMIZADO: Lápides dos Participantes ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfEpitaph(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Lápides dos Participantes";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).SemiBold();
                    if (!string.IsNullOrWhiteSpace(retreatName))
                        col.Item().Text(retreatName).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingVertical(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Foto");
                            header.Cell().Element(HeaderStyle).Text("Nome");
                            header.Cell().Element(HeaderStyle).Text("Nascimento");
                            header.Cell().Element(HeaderStyle).Text("Cidade");
                            header.Cell().Element(HeaderStyle).Text("Família");
                        });

                        foreach (var row in payload.data)
                        {
                            var photoUrl = GetStringValue(row, "photoUrl");
                            var participantName = GetStringValue(row, "name", "-");
                            var birthDate = GetStringValue(row, "birthDate", "-");
                            var city = GetStringValue(row, "city", "-");
                            var familyName = GetStringValue(row, "familyName", "-");
                            var familyColor = GetStringValue(row, "familyColor", "#CCCCCC");

                            table.Cell().Element(DataCell).AlignCenter().AlignMiddle()
                                .Text(!string.IsNullOrWhiteSpace(photoUrl) ? "🖼️" : "👤")
                                .FontSize(24);

                            table.Cell().Element(DataCell).Text(participantName);
                            table.Cell().Element(DataCell).Text(birthDate);
                            table.Cell().Element(DataCell).Text(city);

                            table.Cell()
                                .Background(familyColor)
                                .Padding(5)
                                .Text(familyName)
                                .FontColor(GetContrastColor(familyColor));
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} participantes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Participantes Contemplados ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfContemplatedParticipants(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Participantes Contemplados";
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
                    col.Item().PaddingVertical(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Nome");
                            header.Cell().Element(HeaderStyle).Text("Contato");
                            header.Cell().Element(HeaderStyle).Text("Status");
                        });

                        foreach (var row in payload.data)
                        {
                            var participantName = GetStringValue(row, "name", "-");
                            var contact = GetStringValue(row, "phone", "-");
                            var status = GetStringValue(row, "status", "-");

                            table.Cell().Element(DataCell).Text(participantName);
                            table.Cell().Element(DataCell).Text(contact);
                            table.Cell().Element(DataCell).Text(status);
                        }
                    });

                    if (payload.summary?.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("Resumo").FontSize(12).SemiBold();
                        foreach (var kv in payload.summary)
                        {
                            col.Item().Text($"{kv.Key}: {FormatValue(kv.Value)}")
                                .FontSize(9);
                        }
                    }
                });

                page.Footer()
                    .Height(20)
                    .AlignRight()
                    .AlignMiddle()
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

    // ========== PDF CUSTOMIZADO: Alocação de Barracas ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfTentAllocation(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Alocação de Barracas";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
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
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(0.8f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Barraca");
                            header.Cell().Element(HeaderStyle).Text("Nome");
                            header.Cell().Element(HeaderStyle).Text("Função");
                            header.Cell().Element(HeaderStyle).Text("Família");
                            header.Cell().Element(HeaderStyle).Text("Sexo");
                        });

                        foreach (var row in payload.data)
                        {
                            var tentNumber = GetStringValue(row, "tentNumber");
                            var participantName = GetStringValue(row, "name");
                            var role = GetStringValue(row, "role");
                            var familyName = GetStringValue(row, "familyName");
                            var familyColor = GetStringValue(row, "familyColor", "#FFFFFF");
                            var gender = GetStringValue(row, "gender");

                            table.Cell().Element(DataCell).Text(tentNumber).SemiBold();
                            table.Cell().Element(DataCell).Text(participantName);
                            table.Cell().Element(DataCell).Text(role);
                            table.Cell()
                                .Padding(3)
                                .Background(familyColor)
                                .Text(familyName)
                                .FontColor(GetContrastColor(familyColor));
                            table.Cell().Element(DataCell).AlignCenter().Text(gender);
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} participantes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Rahamistas por Família ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfRahamistasPerFamilia(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Rahamistas por Família";
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
                    foreach (var row in payload.data)
                    {
                        var familyName = GetStringValue(row, "familyName", "Sem Nome");
                        var familyColor = GetStringValue(row, "familyColor", "#CCCCCC");

                        col.Item().PaddingTop(15).PaddingBottom(5)
                            .Background(familyColor)
                            .Padding(8)
                            .Text(familyName)
                            .FontSize(14)
                            .SemiBold()
                            .FontColor(Colors.White);

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Nome");
                                header.Cell().Element(HeaderStyle).Text("Gênero");
                            });

                            var idx = 1;
                            foreach (var member in payload.data.Where(d => GetStringValue(d, "familyName") == familyName))                            {
                                var memberName = GetStringValue(member, "name");
                                var gender = GetStringValue(member, "gender");

                                table.Cell().Element(DataCell).Text(idx.ToString()).AlignCenter();
                                table.Cell().Element(DataCell).Text(memberName);
                                table.Cell().Element(DataCell).Text(gender);
                                idx++;
                            }
                        });
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} participantes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Check-in Bota Fora ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfCheckInBotaFora(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Bota Fora x Rahamivida";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(14).SemiBold();
                    if (!string.IsNullOrWhiteSpace(retreatName))
                        col.Item().Text(retreatName).FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(5).Column(col =>
                {
                    var faixas = new[] { "A-D", "E-H", "I-L", "M-Z" };

                    foreach (var faixa in faixas)
                    {
                        var faixaData = payload.data
                            .Where(row => GetStringValue(row, "faixa") == faixa)
                            .ToList();

                        if (!faixaData.Any())
                            continue;

                        col.Item().PaddingHorizontal(5).PaddingTop(10)
                            .Text($"FAIXA {faixa}")
                            .FontSize(10)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken4);

                        col.Item().PaddingHorizontal(5).PaddingBottom(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);
                                columns.RelativeColumn(2.5f);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(25);
                            });

                            table.Header(header =>
                            {
                                var headerCells = new[] { "#", "Nome", "Termo", "Cel", "Relig", "Rem", "Cart", "Bolsa", "Chave" };
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

                            var rowNum = 1;
                            foreach (var row in faixaData)
                            {
                                var participantName = GetStringValue(row, "name");
                                if (string.IsNullOrWhiteSpace(participantName)) continue;

                                table.Cell().Element(DataCell).AlignCenter().Text(rowNum.ToString());
                                table.Cell().Element(DataCell).Text(participantName);

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

                                rowNum++;
                            }
                        });

                        col.Item().PaddingVertical(5);
                    }

                    if (payload.summary?.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Resumo").FontSize(10).SemiBold();
                        foreach (var kv in payload.summary)
                        {
                            col.Item().Text($"{kv.Key}: {FormatValue(kv.Value)}")
                                .FontSize(8);
                        }
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Bem-Estar por Família ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfWellnessPerFamily(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Bem-Estar por Família";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
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
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Família");
                            header.Cell().Element(HeaderStyle).Text("Rahamista");
                            header.Cell().Element(HeaderStyle).Text("Medicamento / Observações");
                        });

                        foreach (var row in payload.data)
                        {
                            var family = GetStringValue(row, "family");
                            var memberName = GetStringValue(row, "name");
                            var familyColor = GetStringValue(row, "familyColor", "#FFFFFF");

                            table.Cell()
                                .Background(familyColor)
                                .Padding(5)
                                .Text(family)
                                .SemiBold()
                                .FontSize(10)
                                .FontColor(GetContrastColor(familyColor));

                            table.Cell().Element(DataCell).Text(memberName);

                            table.Cell()
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Height(30)
                                .AlignTop()
                                .Text("");
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} participantes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Ficha Individual do Participante ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfParticipantIndividualCard(
        ReportPayload payload,
        string name)
    {
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
            foreach (var row in payload.data)
            {
                var participantName = GetStringValue(row, "name");
                var familyColor = GetStringValue(row, "familyColor", "#CCCCCC");

                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);

                    page.Header().Height(100).Background(familyColor).Padding(20).Column(col =>
                    {
                        col.Item().AlignCenter().AlignMiddle()
                            .Text(participantName)
                            .FontSize(32)
                            .SemiBold()
                            .FontColor(GetContrastColor(familyColor));

                        col.Item().AlignCenter()
                            .Text(retreatName)
                            .FontSize(12)
                            .FontColor(GetContrastColor(familyColor));
                    });

                    page.Content().Padding(20).Column(col =>
                    {
                        for (int i = 0; i < 25; i++)
                        {
                            col.Item()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(20)
                                .PaddingVertical(2);
                        }
                    });

                    page.Footer().Padding(10).AlignRight()
                        .Text($"{DateTime.Now:dd/MM/yyyy}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });
            }
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).SemiBold();
                    if (!string.IsNullOrWhiteSpace(retreatName))
                        col.Item().Text(retreatName).FontSize(12).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(col =>
                {
                    foreach (var row in payload.data)
                    {
                        var number = GetStringValue(row, "number");
                        var participantName = GetStringValue(row, "name");

                        col.Item().Row(gridRow =>
                        {
                            gridRow.ConstantItem(40).AlignRight().PaddingRight(10)
                                .Text(number)
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            gridRow.RelativeItem().AlignLeft()
                                .Text(participantName)
                                .FontSize(11);
                        });

                        col.Item().PaddingVertical(2);
                    }
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().Text($"Total: {payload.data.Count} participantes")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return ("application/pdf", fileName, pdfBytes);
    }

    // ========== PDF CUSTOMIZADO: Bolsas ==========
    private static (string ContentType, string FileName, byte[] Bytes) ExportPdfBagsDistribution(
        ReportPayload payload,
        string name)
    {
        var title = payload.report.Title ?? "Bolsas";
        var retreatName = payload.report.RetreatName ?? "";

        var pdfBytes = Document.Create(doc =>
        {
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
                            colA.Item()
                                .BorderBottom(2)
                                .BorderColor(Colors.Black)
                                .PaddingBottom(10)
                                .Text("A")
                                .FontSize(14)
                                .SemiBold();


                            var colAData = payload.data
                                .Select(d => GetStringValue(d, "columnA"))                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .ToList();

                            foreach (var participantName in colAData)
                            {
                                colA.Item().PaddingVertical(8).Text(participantName).FontSize(11);
                            }
                        });

                        gridRow.RelativeColumn().Column(colB =>
                        {
                            colB.Item()
                                .BorderBottom(2)
                                .BorderColor(Colors.Black)
                                .PaddingBottom(10)
                                .Text("B")
                                .FontSize(14)
                                .SemiBold();

                            var colBData = payload.data
                                .Select(d => GetStringValue(d, "columnB"))                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .ToList();

                            foreach (var participantName in colBData)
                            {
                                colB.Item().PaddingVertical(8).Text(participantName).FontSize(11);
                            }
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} participantes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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
                    var tamanhos = new[] { "P", "M", "G", "GG", "XG", "XXG" };

                    foreach (var tamanho in tamanhos)
                    {
                        var shirtData = payload.data
                            .Where(row => GetStringValue(row, "shirtSize") == tamanho)
                            .ToList();

                        if (!shirtData.Any())
                            continue;

                        col.Item()
                            .PaddingTop(10)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingBottom(5)
                            .Text($"Tamanho {tamanho} ({shirtData.Count})")
                            .FontSize(12)
                            .SemiBold();


                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(3);
                            });

                            var idx = 1;
                            foreach (var row in shirtData)
                            {
                                var participantName = GetStringValue(row, "name");

                                table.Cell().Element(DataCell).AlignCenter().Text(idx.ToString());
                                table.Cell().Element(DataCell).Text(participantName);
                                idx++;
                            }
                        });
                    }

                    if (payload.summary?.Count > 0)
                    {
                        col.Item().PaddingTop(15).Text("Resumo por Tamanho").FontSize(11).SemiBold();
                        foreach (var kv in payload.summary)
                        {
                            col.Item().Text($"{kv.Key}: {FormatValue(kv.Value)}")
                                .FontSize(9);
                        }
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Total: {payload.data.Count} camisetas")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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