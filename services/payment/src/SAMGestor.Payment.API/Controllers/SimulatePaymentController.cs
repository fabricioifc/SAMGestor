//  DEVELOPMENT ONLY — bloquear no nginx em produção: location /api/dev/ { deny all; }

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Domain.Enums;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.API.Controllers;

[ApiController]
[Route("api/dev/payments")]
[ApiExplorerSettings(GroupName = "dev")]
public sealed class SimulatePaymentController : ControllerBase
{
    private readonly PaymentDbContext    _db;
    private readonly IEventBus           _bus;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SimulatePaymentController> _logger;

    public SimulatePaymentController(
        PaymentDbContext db, IEventBus bus,
        IWebHostEnvironment env,
        ILogger<SimulatePaymentController> logger)
    {
        _db = db; _bus = bus; _env = env; _logger = logger;
    }

    /// <summary>
    /// Aprova pagamento por registrationId — bypass da API do MercadoPago.
    /// Idempotente: 409 se já estava Paid. 404 se payment ainda não criado
    /// (evento PaymentRequestedV1 ainda em trânsito no RabbitMQ — k6 faz retry).
    /// </summary>
    [HttpPost("{registrationId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid registrationId, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogWarning("Acesso negado ao SimulatePaymentController. RegistrationId={Id}", registrationId);
            return Forbid();
        }

        var payment = await _db.Payments
            .SingleOrDefaultAsync(p => p.RegistrationId == registrationId, ct);

        if (payment is null)
        {
            _logger.LogWarning(
                "[DEV-SIM] Payment não encontrado para RegistrationId={Id}. " +
                "PaymentRequestedV1 pode estar em trânsito no RabbitMQ.", registrationId);

            return NotFound(new
            {
                error = "payment_not_found",
                registrationId,
                hint  = "Aguarde ~2s e tente novamente.",
            });
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            _logger.LogDebug("[DEV-SIM] Payment {Id} já estava Paid — idempotente.", payment.Id);
            return Conflict(new { message = "already_paid", paymentId = payment.Id });
        }

        var paidAt = DateTimeOffset.UtcNow;
        payment.MarkPaid($"dev-sim-{Guid.NewGuid():N}", paidAt);
        await _db.SaveChangesAsync(ct);

        var evt = new PaymentConfirmedV1(
            PaymentId:      payment.Id,
            RegistrationId: payment.RegistrationId,
            RetreatId:      payment.RetreatId,
            Amount:         payment.Amount,
            Method:         "pix",
            PaidAt:         paidAt
        );

        await _bus.EnqueueAsync(
            type:   EventTypes.PaymentConfirmedV1,
            source: "sam.payment.dev-simulate",
            data:   evt,
            ct:     ct
        );

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[DEV-SIM] Payment {PaymentId} aprovado para RegistrationId={RegId}. PaymentConfirmedV1 publicado.",
            payment.Id, registrationId);

        return Ok(new
        {
            paymentId      = payment.Id,
            registrationId = payment.RegistrationId,
            retreatId      = payment.RetreatId,
            amount         = payment.Amount,
            method         = "pix",
            paidAt,
        });
    }
}