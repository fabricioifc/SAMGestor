using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Retreats.Delete;
using SAMGestor.Application.Features.Retreats.EmergencyCodes.Deactivate;
using SAMGestor.Application.Features.Retreats.EmergencyCodes.Generate;
using SAMGestor.Application.Features.Retreats.EmergencyCodes.List;
using SAMGestor.Application.Features.Retreats.GetAll;
using SAMGestor.Application.Features.Retreats.GetById;
using SAMGestor.Application.Features.Retreats.GetPublicById;
using SAMGestor.Application.Features.Retreats.Images.Remove;
using SAMGestor.Application.Features.Retreats.Images.Reorder;
using SAMGestor.Application.Features.Retreats.Images.Upload;
using SAMGestor.Application.Features.Retreats.ManageStatus;
using SAMGestor.Application.Features.Retreats.Publish;
using SAMGestor.Application.Features.Retreats.Unpublish;
using SAMGestor.Application.Features.Retreats.Update;
using SAMGestor.Application.Features.Retreats.UpdateContact;
using SAMGestor.Application.Features.Retreats.UpdatePrivacyPolicy;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using Swashbuckle.AspNetCore.Annotations;


namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Operações relacionadas aos retiros")]
public class RetreatsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storage;

    public RetreatsController(IMediator mediator, ICurrentUser currentUser, IStorageService storage)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _storage = storage;
    }

    #region CRUD Básico

    /// <summary>
    /// Cria um novo retiro.
    /// </summary>
    /// <remarks>
    /// Cria um retiro no status "Rascunho". Para torná-lo visível, use o endpoint de publicação.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Criar retiro",
        Description = "Cria um novo retiro no sistema. Inicia no status 'Rascunho'."
    )]
    [SwaggerResponse(201, "Retiro criado com sucesso", typeof(CreateRetreatResponse))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(409, "Retiro com mesmo nome e edição já existe")]
    public async Task<IActionResult> CreateRetreat([FromBody] CreateRetreatCommandRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        
        var command = new CreateRetreatCommand(
            Name: request.Name,
            Edition: request.Edition,
            Theme: request.Theme,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            MaleSlots: request.MaleSlots,
            FemaleSlots: request.FemaleSlots,
            RegistrationStart: request.RegistrationStart,
            RegistrationEnd: request.RegistrationEnd,
            FeeFazer: request.FeeFazer,
            FeeServir: request.FeeServir,
            CreatedByUserId: userId,
            ShortDescription: request.ShortDescription,
            LongDescription: request.LongDescription,
            Location: request.Location,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.RetreatId }, result);
    }

    /// <summary>
    /// Obtém detalhes completos de um retiro (Administrativo).
    /// </summary>
    /// <remarks>
    /// Retorna TODAS as informações do retiro, incluindo dados internos.
    /// Apenas para Administradores, Gestores e Consultores.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Obter retiro por ID ",
        Description = "Retorna detalhes completos do retiro, incluindo informações administrativas."
    )]
    [SwaggerResponse(200, "Retiro encontrado", typeof(GetRetreatByIdResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _mediator.Send(new GetRetreatByIdQuery(id));
        return Ok(response);
    }

    /// <summary>
    /// Lista retiros com filtros e paginação (Administrativo).
    /// </summary>
    /// <remarks>
    /// Lista todos os retiros com possibilidade de filtrar por status e visibilidade.
    /// Apenas para Administradores, Gestores e Consultores.
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Listar retiros ",
        Description = "Lista retiros com filtros de status e visibilidade pública."
    )]
    [SwaggerResponse(200, "Lista de retiros", typeof(PagedResult<RetreatListDto>))]
    public async Task<ActionResult<PagedResult<RetreatListDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] RetreatStatus? status = null,
        [FromQuery] bool? isPubliclyVisible = null,
        CancellationToken ct = default)
    {
        var query = new ListRetreatsQuery(skip, take, status, isPubliclyVisible);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza os detalhes de um retiro existente.
    /// </summary>
    /// <remarks>
    /// Permite atualizar todos os campos do retiro, exceto o ID.
    /// Não é possível atualizar retiros cancelados ou finalizados.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Atualizar retiro",
        Description = "Atualiza informações de um retiro existente."
    )]
    [SwaggerResponse(200, "Retiro atualizado", typeof(UpdateRetreatResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    [SwaggerResponse(400, "Retiro cancelado/finalizado não pode ser atualizado")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRetreatCommandRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();

        var command = new UpdateRetreatCommand(
            Id: id,
            Name: request.Name,
            Edition: request.Edition,
            Theme: request.Theme,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            MaleSlots: request.MaleSlots,
            FemaleSlots: request.FemaleSlots,
            RegistrationStart: request.RegistrationStart,
            RegistrationEnd: request.RegistrationEnd,
            FeeFazer: request.FeeFazer,
            FeeServir: request.FeeServir,
            ModifiedByUserId: userId,
            ShortDescription: request.ShortDescription,
            LongDescription: request.LongDescription,
            Location: request.Location,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Exclui um retiro pelo seu ID.
    /// </summary>
    /// <remarks>
    /// Remove permanentemente o retiro do sistema.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Excluir retiro",
        Description = "Remove um retiro permanentemente do sistema."
    )]
    [SwaggerResponse(204, "Retiro excluído com sucesso")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteRetreatCommand(id));
        return NoContent();
    }

    #endregion

    #region Endpoints Públicos

    /// <summary>
    /// Obtém informações públicas de um retiro (SEM autenticação).
    /// </summary>
    /// <remarks>
    /// Endpoint público para participantes visualizarem detalhes do retiro antes de se inscrever.
    /// Retorna apenas informações relevantes (sem dados administrativos).
    /// Apenas retiros publicados e visíveis são retornados.
    /// </remarks>
    [HttpGet("public/{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Obter retiro público",
        Description = "Retorna informações públicas do retiro para participantes. Não requer autenticação."
    )]
    [SwaggerResponse(200, "Retiro encontrado", typeof(PublicRetreatResponse))]
    [SwaggerResponse(404, "Retiro não encontrado ou não público")]
    public async Task<IActionResult> GetPublicRetreat(Guid id)
    {
        var response = await _mediator.Send(new GetPublicRetreatByIdQuery(id));
        return Ok(response);
    }

    #endregion

    #region Publicação e Status

    /// <summary>
    /// Publica um retiro (torna visível ao público).
    /// </summary>
    /// <remarks>
    /// Valida se todos os campos obrigatórios estão preenchidos antes de publicar.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Publicar retiro",
        Description = "Torna o retiro visível ao público e disponível para inscrições."
    )]
    [SwaggerResponse(200, "Retiro publicado", typeof(PublishRetreatResponse))]
    [SwaggerResponse(400, "Retiro não pode ser publicado (campos faltantes)")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new PublishRetreatCommand(id, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Despublica um retiro (remove da visualização pública).
    /// </summary>
    /// <remarks>
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Despublicar retiro",
        Description = "Remove o retiro da visualização pública."
    )]
    [SwaggerResponse(200, "Retiro despublicado", typeof(UnpublishRetreatResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new UnpublishRetreatCommand(id, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Gerencia o status do retiro.
    /// </summary>
    /// <remarks>
    /// Permite transições de status: OpenRegistration, CloseRegistration, Start, Complete, Cancel.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Gerenciar status do retiro",
        Description = "Altera o status do retiro (abrir/fechar inscrições, iniciar, finalizar, cancelar)."
    )]
    [SwaggerResponse(200, "Status alterado", typeof(ManageStatusResponse))]
    [SwaggerResponse(400, "Transição de status inválida")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> ManageStatus(Guid id, [FromBody] ManageStatusRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new ManageStatusCommand(id, request.Action, userId, request.Reason);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion

    #region Contato e Política

    /// <summary>
    /// Atualiza informações de contato do retiro.
    /// </summary>
    /// <remarks>
    /// Permite atualizar apenas email e telefone de contato.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPut("{id:guid}/contact")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Atualizar contato",
        Description = "Atualiza email e telefone de contato do retiro."
    )]
    [SwaggerResponse(200, "Contato atualizado", typeof(UpdateContactResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdateContactRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new UpdateContactCommand(id, userId, request.ContactEmail, request.ContactPhone);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza a política de privacidade do retiro.
    /// </summary>
    /// <remarks>
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPut("{id:guid}/privacy-policy")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Atualizar política de privacidade",
        Description = "Define ou atualiza a política de privacidade do retiro (LGPD)."
    )]
    [SwaggerResponse(200, "Política atualizada", typeof(UpdatePrivacyPolicyResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> UpdatePrivacyPolicy(Guid id, [FromBody] UpdatePrivacyPolicyRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new UpdatePrivacyPolicyCommand(id, request.Title, request.Body, request.Version, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion

    #region Códigos de Emergência

    /// <summary>
    /// Gera um código de emergência para inscrições fora do prazo.
    /// </summary>
    /// <remarks>
    /// Permite criar códigos especiais para inscrições após o período normal.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPost("{id:guid}/emergency-codes")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Gerar código de emergência",
        Description = "Cria um código para permitir inscrições fora do prazo normal."
    )]
    [SwaggerResponse(201, "Código gerado", typeof(GenerateEmergencyCodeResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> GenerateEmergencyCode(Guid id, [FromBody] GenerateEmergencyCodeRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new GenerateEmergencyCodeCommand(
            id, userId, request.ValidityDays, request.Reason, request.MaxUses);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ListEmergencyCodes), new { id }, result);
    }

    /// <summary>
    /// Lista códigos de emergência de um retiro.
    /// </summary>
    /// <remarks>
    /// Retorna todos os códigos ou apenas os ativos.
    /// Apenas Administradores, Gestores e Consultores.
    /// </remarks>
    [HttpGet("{id:guid}/emergency-codes")]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Listar códigos de emergência",
        Description = "Lista códigos de emergência do retiro com filtro de ativos."
    )]
    [SwaggerResponse(200, "Lista de códigos", typeof(ListEmergencyCodesResponse))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> ListEmergencyCodes(Guid id, [FromQuery] bool onlyActive = true)
    {
        var query = new ListEmergencyCodesQuery(id, onlyActive);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Desativa um código de emergência.
    /// </summary>
    /// <remarks>
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpDelete("{id:guid}/emergency-codes/{code}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Desativar código de emergência",
        Description = "Desativa um código de emergência existente."
    )]
    [SwaggerResponse(200, "Código desativado", typeof(DeactivateEmergencyCodeResponse))]
    [SwaggerResponse(404, "Retiro ou código não encontrado")]
    public async Task<IActionResult> DeactivateEmergencyCode(Guid id, string code)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new DeactivateEmergencyCodeCommand(id, code, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion

    #region Imagens

    /// <summary>
    /// Upload de imagem do retiro (Banner, Thumbnail ou Galeria).
    /// </summary>
    /// <remarks>
    /// - Banner e Thumbnail: substitui automaticamente se já existir.
    /// - Galeria: adiciona nova imagem.
    /// Formatos aceitos: JPG, PNG (máx 5MB).
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Upload de imagem",
        Description = "Faz upload de imagem para o retiro (Banner, Thumbnail ou Galeria)."
    )]
    [SwaggerResponse(201, "Imagem enviada", typeof(UploadRetreatImageResult))]
    [SwaggerResponse(400, "Arquivo inválido")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> UploadImage(Guid id, [FromForm] UploadImageRequest request)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "Arquivo é obrigatório" });

        var contentType = request.File.ContentType?.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png"))
            return BadRequest(new { error = "Formato inválido. Use JPG ou PNG." });

        const int MaxSizeBytes = 5 * 1024 * 1024; // 5MB
        if (request.File.Length > MaxSizeBytes)
            return BadRequest(new { error = "Arquivo muito grande. Máximo: 5MB." });

        var userId = _currentUser.UserId!.Value.ToString();

        await using var stream = request.File.OpenReadStream();

        var command = new UploadRetreatImageCommand(
            RetreatId: id,
            Type: request.Type,
            FileStream: stream,
            FileName: request.File.FileName,
            ContentType: contentType,
            FileSizeBytes: request.File.Length,
            UploadedByUserId: userId,
            AltText: request.AltText,
            Order: request.Order
        );

        var result = await _mediator.Send(command);

        return Created(result.ImageUrl, new
        {
            retreatId = result.RetreatId,
            storageKey = result.StorageKey,
            imageUrl = result.ImageUrl,
            type = result.Type.ToString(),
            order = result.Order,
            uploadedAt = result.UploadedAt,
            replacedExisting = result.ReplacedExisting
        });
    }

    /// <summary>
    /// Remove uma imagem do retiro.
    /// </summary>
    /// <remarks>
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpDelete("{id:guid}/images/{**storageId}")] 
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Remover imagem",
        Description = "Remove uma imagem do retiro pelo StorageId."
    )]
    [SwaggerResponse(200, "Imagem removida", typeof(RemoveRetreatImageResult))]
    [SwaggerResponse(404, "Retiro ou imagem não encontrada")]
    public async Task<IActionResult> RemoveImage(Guid id, string storageId)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        
        var decodedStorageId = Uri.UnescapeDataString(storageId);
    
        var command = new RemoveRetreatImageCommand(id, decodedStorageId, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Reordena imagens da galeria.
    /// </summary>
    /// <remarks>
    /// Permite alterar a ordem de exibição de múltiplas imagens.
    /// Apenas Administradores e Gestores.
    /// </remarks>
    [HttpPut("{id:guid}/images/reorder")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Reordenar imagens",
        Description = "Altera a ordem de exibição das imagens da galeria."
    )]
    [SwaggerResponse(200, "Imagens reordenadas", typeof(ReorderGalleryImagesResult))]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> ReorderImages(Guid id, [FromBody] ReorderImagesRequest request)
    {
        var userId = _currentUser.UserId!.Value.ToString();
        var command = new ReorderGalleryImagesCommand(id, request.ImageOrders, userId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion
    
    /// <summary>
    /// Valida um código de emergência (SEM autenticação).
    /// </summary>
    /// <remarks>
    /// Permite que o frontend valide se um código é válido antes de liberar o formulário.
    /// </remarks>
    [HttpPost("public/{id:guid}/validate-emergency-code")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Validar código de emergência",
        Description = "Verifica se um código de emergência é válido para este retiro."
    )]
    [SwaggerResponse(200, "Resultado da validação")]
    [SwaggerResponse(404, "Retiro não encontrado")]
    public async Task<IActionResult> ValidateEmergencyCode(
        Guid id,
        [FromBody] ValidateEmergencyCodeRequest request,
        [FromServices] IRetreatRepository retreatRepo,
        CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdWithDetailsAsync(id, ct);

        if (retreat is null)
            return NotFound(new
            {
                valid = false,
                message = "Retiro não encontrado."
            });

        if (!retreat.IsPubliclyVisible)
            return NotFound(new
            {
                valid = false,
                message = "Retiro não está disponível."
            });

        var isValid = retreat.ValidateEmergencyCode(request.Code, DateTime.UtcNow);

        if (!isValid)
            return Ok(new
            {
                valid = false,
                message = "Código inválido, expirado ou já utilizado completamente."
            });

        return Ok(new
        {
            valid = true,
            message = "Código válido! Você pode prosseguir com a inscrição."
        });
    }

    public record ValidateEmergencyCodeRequest(string Code);

}

#region Request DTOs

public record CreateRetreatCommandRequest(
    FullName Name,
    string Edition,
    string Theme,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaleSlots,
    int FemaleSlots,
    DateOnly RegistrationStart,
    DateOnly RegistrationEnd,
    Domain.ValueObjects.Money FeeFazer,
    Domain.ValueObjects.Money FeeServir,
    string? ShortDescription = null,
    string? LongDescription = null,
    string? Location = null,
    string? ContactEmail = null,
    string? ContactPhone = null
);

public record UpdateRetreatCommandRequest(
    FullName Name,
    string Edition,
    string Theme,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaleSlots,
    int FemaleSlots,
    DateOnly RegistrationStart,
    DateOnly RegistrationEnd,
    Domain.ValueObjects.Money FeeFazer,
    Domain.ValueObjects.Money FeeServir,
    string? ShortDescription = null,
    string? LongDescription = null,
    string? Location = null,
    string? ContactEmail = null,
    string? ContactPhone = null
);

public record ManageStatusRequest(
    StatusAction Action,
    string? Reason = null
);

public record UpdateContactRequest(
    string? ContactEmail,
    string? ContactPhone
);

public record UpdatePrivacyPolicyRequest(
    string Title,
    string Body,
    string Version
);

public record GenerateEmergencyCodeRequest(
    int ValidityDays = 30,
    string? Reason = null,
    int? MaxUses = null
);

public record UploadImageRequest(
    IFormFile File,
    ImageType Type,
    string? AltText = null,
    int Order = 0
);

public record ReorderImagesRequest(
    List<ImageOrderDto> ImageOrders
);

#endregion
