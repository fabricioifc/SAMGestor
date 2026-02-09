using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Notifications.GetHistory;
using SAMGestor.Application.Features.Notifications.SendToAdmins;
using SAMGestor.Application.Features.Notifications.SendToModule;
using SAMGestor.Application.Features.Notifications.SendToUsers;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Notification;

[ApiController]
[Route("admin/custom-notifications")]
[SwaggerTag("Envio de notificações customizadas para participantes e administradores. (Admin, Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public class CustomNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Envia notificação customizada para usuários específicos de um retiro.
    /// </summary>
    /// <param name="retreatId">ID do retiro</param>
    /// <param name="request">Dados da notificação</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>ID da notificação criada e total de destinatários</returns>
    /// <response code="200">Notificação enfileirada com sucesso</response>
    /// <response code="400">Dados inválidos ou nenhum destinatário encontrado</response>
    /// <response code="404">Retiro não encontrado</response>
    [HttpPost("retreats/{retreatId:guid}/send-to-users")]
    [SwaggerOperation(
        Summary = "Enviar para usuários específicos",
        Description = "Envia notificação customizada para usuários escolhidos (Registrations ou ServiceRegistrations). Aceita mistura de módulos Fazer e Servir.",
        OperationId = "SendCustomNotificationToUsers"
    )]
    public async Task<IActionResult> SendToUsers(
        [FromRoute] Guid retreatId,
        [FromBody] SendToUsersRequest request,
        CancellationToken ct)
    {
        var command = new SendCustomNotificationToUsersCommand(
            RetreatId: retreatId,
            UserIds: request.UserIds,
            Subject: request.Subject,
            Body: request.Body,
            PreheaderText: request.PreheaderText,
            CallToActionUrl: request.CallToActionUrl,
            CallToActionText: request.CallToActionText,
            SecondaryLinkUrl: request.SecondaryLinkUrl,
            SecondaryLinkText: request.SecondaryLinkText,
            ImageUrl: request.ImageUrl
        );

        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            notificationId = result.NotificationId,
            totalRecipients = result.TotalRecipients,
            status = result.Status
        });
    }

    /// <summary>
    /// Envia notificação customizada para módulo(s) completo(s) com filtros opcionais.
    /// </summary>
    /// <param name="retreatId">ID do retiro</param>
    /// <param name="request">Dados da notificação e filtros</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>ID da notificação criada e total de destinatários</returns>
    /// <response code="200">Notificação enfileirada com sucesso</response>
    /// <response code="400">Dados inválidos ou filtros incorretos</response>
    /// <response code="404">Retiro não encontrado</response>
    [HttpPost("retreats/{retreatId:guid}/send-to-module")]
    [SwaggerOperation(
        Summary = "Enviar para módulo(s)",
        Description = "Envia notificação para módulo Fazer, Servir ou Ambos. Permite filtrar por status (ex: Selected, PaymentConfirmed). Se StatusFilters for vazio/null, envia para todos os status.",
        OperationId = "SendCustomNotificationToModule"
    )]
    public async Task<IActionResult> SendToModule(
        [FromRoute] Guid retreatId,
        [FromBody] SendToModuleRequest request,
        CancellationToken ct)
    {
        var command = new SendCustomNotificationToModuleCommand(
            RetreatId: retreatId,
            TargetModule: request.TargetModule,
            StatusFilters: request.StatusFilters,
            Subject: request.Subject,
            Body: request.Body,
            PreheaderText: request.PreheaderText,
            CallToActionUrl: request.CallToActionUrl,
            CallToActionText: request.CallToActionText,
            SecondaryLinkUrl: request.SecondaryLinkUrl,
            SecondaryLinkText: request.SecondaryLinkText,
            ImageUrl: request.ImageUrl
        );

        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            notificationId = result.NotificationId,
            totalRecipients = result.TotalRecipients,
            status = result.Status
        });
    }

    /// <summary>
    /// Envia notificação customizada para usuários administrativos do sistema.
    /// </summary>
    /// <param name="request">Dados da notificação</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>ID da notificação criada e total de destinatários</returns>
    /// <response code="200">Notificação enfileirada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="403">Apenas administradores podem usar este endpoint</response>
    [HttpPost("send-to-admins")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SwaggerOperation(
        Summary = "Enviar para administradores",
        Description = "Envia notificação para usuários do sistema (Admin, Gestor, Consultor). Se UserIds for null/vazio, envia para TODOS. Requer permissão de Administrador.",
        OperationId = "SendCustomNotificationToAdmins"
    )]
    public async Task<IActionResult> SendToAdmins(
        [FromBody] SendToAdminsRequest request,
        CancellationToken ct)
    {
        var command = new SendCustomNotificationToAdminsCommand(
            UserIds: request.UserIds,
            Subject: request.Subject,
            Body: request.Body,
            PreheaderText: request.PreheaderText,
            CallToActionUrl: request.CallToActionUrl,
            CallToActionText: request.CallToActionText,
            SecondaryLinkUrl: request.SecondaryLinkUrl,
            SecondaryLinkText: request.SecondaryLinkText,
            ImageUrl: request.ImageUrl
        );

        var result = await _mediator.Send(command, ct);

        return Ok(new
        {
            notificationId = result.NotificationId,
            totalRecipients = result.TotalRecipients,
            status = result.Status
        });
    }

    /// <summary>
    /// Consulta o histórico de notificações customizadas enviadas para um retiro.
    /// </summary>
    /// <param name="retreatId">ID do retiro</param>
    /// <param name="skip">Quantidade de registros a pular (paginação)</param>
    /// <param name="take">Quantidade de registros a retornar (máx: 100)</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Lista de notificações enviadas com detalhes</returns>
    /// <response code="200">Histórico retornado com sucesso</response>
    /// <response code="404">Retiro não encontrado</response>
    [HttpGet("retreats/{retreatId:guid}/history")]
    [SwaggerOperation(
        Summary = "Consultar histórico",
        Description = "Retorna o histórico de todas as notificações customizadas enviadas para um retiro específico, ordenadas por data decrescente.",
        OperationId = "GetCustomNotificationHistory"
    )]
    public async Task<IActionResult> GetHistory(
        [FromRoute] Guid retreatId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (take > 100) take = 100; 

        var query = new GetCustomNotificationHistoryQuery(
            RetreatId: retreatId,
            Skip: skip,
            Take: take
        );

        var result = await _mediator.Send(query, ct);

        return Ok(new
        {
            retreatId = result.RetreatId,
            items = result.Items.Select(item => new
            {
                notificationId = item.NotificationId,
                sentByName = item.SentByName,
                sentAt = item.SentAt,
                targetType = item.TargetType,
                targetFilter = item.TargetFilterJson,
                subject = item.Subject,
                totalRecipients = item.TotalRecipients,
                status = item.Status,
                failureReason = item.FailureReason
            }),
            pagination = new
            {
                total = result.Total,
                skip = result.Skip,
                take = result.Take,
                hasMore = result.Skip + result.Take < result.Total
            }
        });
    }
}

#region Request DTOs

/// <summary>
/// Requisição para envio de notificação a usuários específicos
/// </summary>
public sealed record SendToUsersRequest
{
    /// <summary>
    /// Lista de IDs dos usuários (Registrations ou ServiceRegistrations)
    /// </summary>
    /// <example>["3fa85f64-5717-4562-b3fc-2c963f66afa6"]</example>
    public required List<Guid> UserIds { get; init; }

    /// <summary>
    /// Assunto do email (máx: 200 caracteres)
    /// </summary>
    /// <example>Informações Importantes sobre o Retiro</example>
    public required string Subject { get; init; }

    /// <summary>
    /// Corpo da mensagem (suporta HTML, máx: 50.000 caracteres)
    /// </summary>
    /// <example>&lt;p&gt;Olá! Segue informações importantes...&lt;/p&gt;</example>
    public required string Body { get; init; }

    /// <summary>
    /// Texto de preview do email (opcional, máx: 200 caracteres)
    /// </summary>
    public string? PreheaderText { get; init; }

    /// <summary>
    /// URL do botão de ação principal (opcional)
    /// </summary>
    public string? CallToActionUrl { get; init; }

    /// <summary>
    /// Texto do botão de ação principal (opcional)
    /// </summary>
    public string? CallToActionText { get; init; }

    /// <summary>
    /// URL do link secundário (opcional)
    /// </summary>
    public string? SecondaryLinkUrl { get; init; }

    /// <summary>
    /// Texto do link secundário (opcional)
    /// </summary>
    public string? SecondaryLinkText { get; init; }

    /// <summary>
    /// URL da imagem/banner (opcional)
    /// </summary>
    public string? ImageUrl { get; init; }
}

/// <summary>
/// Requisição para envio de notificação a módulo(s)
/// </summary>
public sealed record SendToModuleRequest
{
    /// <summary>
    /// Módulo alvo: "Fazer", "Servir" ou "Ambos"
    /// </summary>
    /// <example>Fazer</example>
    public required string TargetModule { get; init; }

    /// <summary>
    /// Filtros de status (opcional). Se vazio/null, envia para todos os status.
    /// Para Fazer: NotSelected, Selected, PendingPayment, PaymentConfirmed, Confirmed, Canceled
    /// Para Servir: Submitted, Notified, Confirmed, Declined, Cancelled
    /// </summary>
    /// <example>["Selected", "PaymentConfirmed"]</example>
    public List<string>? StatusFilters { get; init; }

    /// <summary>
    /// Assunto do email (máx: 200 caracteres)
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Corpo da mensagem (suporta HTML, máx: 50.000 caracteres)
    /// </summary>
    public required string Body { get; init; }

    public string? PreheaderText { get; init; }
    public string? CallToActionUrl { get; init; }
    public string? CallToActionText { get; init; }
    public string? SecondaryLinkUrl { get; init; }
    public string? SecondaryLinkText { get; init; }
    public string? ImageUrl { get; init; }
}

/// <summary>
/// Requisição para envio de notificação a administradores
/// </summary>
public sealed record SendToAdminsRequest
{
    /// <summary>
    /// Lista de IDs dos usuários administrativos (opcional). Se null/vazio, envia para TODOS.
    /// </summary>
    public List<Guid>? UserIds { get; init; }

    /// <summary>
    /// Assunto do email (máx: 200 caracteres)
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Corpo da mensagem (suporta HTML, máx: 50.000 caracteres)
    /// </summary>
    public required string Body { get; init; }

    public string? PreheaderText { get; init; }
    public string? CallToActionUrl { get; init; }
    public string? CallToActionText { get; init; }
    public string? SecondaryLinkUrl { get; init; }
    public string? SecondaryLinkText { get; init; }
    public string? ImageUrl { get; init; }
}

#endregion
