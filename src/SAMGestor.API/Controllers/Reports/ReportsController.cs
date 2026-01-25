using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Features.Reports.ExportReport;
using SAMGestor.Application.Features.Reports.GenerateReport;
using SAMGestor.Application.Features.Reports.GetAvailableTemplates;
using SAMGestor.Application.Features.Reports.TemplatesList;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Reports;

[ApiController]
[Route("api/reports")]
[SwaggerTag("Operações relacionadas a relatórios de retiros. (Admin,Gestor,Consultor)")]
[Authorize(Policy = Policies.ReadOnly)] 
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ReportsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lista todos os templates de relatórios disponíveis no sistema.
    /// Retorna metadados sobre cada template (key, título, descrição, categoria).
    /// </summary>
    [HttpGet("templates")]
    [SwaggerOperation(
        Summary = "Lista todos os templates disponíveis",
        Description = "Retorna a lista completa de templates de relatórios disponíveis no sistema, " +
                      "com informações sobre categoria, descrição e chave para geração. " +
                      "Use este endpoint para popular dropdowns ou menus de seleção de relatórios."
    )]
    [SwaggerResponse(200, "Lista de templates retornada com sucesso", typeof(List<ReportTemplateInfoDto>))]
    public async Task<ActionResult<List<ReportTemplateInfoDto>>> GetTemplates(
        CancellationToken ct = default)
    {
        var query = new GetTemplatesSchemasQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista os templates de relatórios disponíveis para um retiro específico.
    /// Retorna metadados sobre cada relatório (título, descrição, se tem dados, etc).
    /// </summary>
    [HttpGet("retreats/{retreatId:guid}/templates")]
    [SwaggerOperation(
        Summary = "Lista templates disponíveis por retiro",
        Description = "Retorna todos os templates de relatórios disponíveis para o retiro, " +
                      "indicando quais já possuem dados e a quantidade estimada de registros. " +
                      "Use este endpoint quando precisar validar se um retiro tem dados para determinado relatório."
    )]
    [SwaggerResponse(200, "Lista de templates retornada com sucesso", typeof(List<ReportTemplateInfoDto>))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<ActionResult<List<ReportTemplateInfoDto>>> GetAvailableTemplates(
        [FromRoute] Guid retreatId,
        CancellationToken ct = default)
    {
        var query = new GetAvailableTemplatesQuery(retreatId);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gera um relatório específico,  dados estruturados em JSON.
    /// </summary>
    
    [HttpGet("retreats/{retreatId:guid}/generate/{templateKey}")]
    [SwaggerOperation(
        Description = "Retorna os dados do relatório em formato JSON estruturado, " +
                      "incluindo colunas, dados, resumo e paginação. " +
                      "O frontend renderiza esses dados na interface."
    )]
    [SwaggerResponse(200, "Relatório gerado com sucesso", typeof(ReportPayload))]
    [SwaggerResponse(404, "Retiro ou template não encontrado")]
    [SwaggerResponse(400, "Template inválido ou dados insuficientes")]
    public async Task<ActionResult<ReportPayload>> GenerateReport(
        [FromRoute] Guid retreatId,
        [FromRoute] string templateKey,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GenerateReportQuery(retreatId, templateKey, page, pageSize);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exporta um relatório em formato específico (CSV, PDF, XLSX).
    /// Retorna um arquivo para download.
    /// </summary>
    [HttpPost("retreats/{retreatId:guid}/export")]
    [SwaggerOperation(
        Summary = "Exporta relatório",
        Description = "Gera e retorna o relatório no formato solicitado (CSV, PDF ou XLSX) " +
                      "como um arquivo para download. " +
                      "Formatos aceitos: 'csv', 'pdf', 'xlsx'. " +
                      "Por padrão, exporta todos os registros (sem paginação)."
    )]
    [SwaggerResponse(200, "Arquivo gerado com sucesso")]
    [SwaggerResponse(400, "Formato inválido ou template não encontrado")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> ExportReport(
        [FromRoute] Guid retreatId,
        [FromBody] ExportReportRequestDto request,
        CancellationToken ct = default)
    {
        var command = new ExportReportCommand(
            retreatId,
            request.TemplateKey,
            request.Format,
            request.Page ?? 1,
            request.PageSize ?? 10000  // Exportação pega tudo por padrão
        );

        var result = await _mediator.Send(command, ct);
        
        return File(result.Bytes, result.ContentType, result.FileName);
    }
}

/// <summary>
/// Request para exportação de relatório
/// </summary>
public sealed record ExportReportRequestDto(
    [SwaggerParameter("Chave do template (ex: 'people-epitaph', 'tents-allocation', etc.)")]
    string TemplateKey,
    
    [SwaggerParameter("Formato do arquivo: 'csv', 'pdf' ou 'xlsx'")]
    string Format,
    
    [SwaggerParameter("Página para exportação (padrão: 1)")]
    int? Page = null,
    
    [SwaggerParameter("Quantidade de registros por página (padrão: 10000 - todos)")]
    int? PageSize = null
);
