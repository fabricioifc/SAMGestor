using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.API.Extensions;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Features.Service.Registrations.Confirmed;
using SAMGestor.Application.Features.Service.Registrations.Create;
using SAMGestor.Application.Features.Service.Registrations.GetAll;
using SAMGestor.Application.Features.Service.Registrations.GetById;
using SAMGestor.Application.Features.Service.Registrations.Update;
using SAMGestor.Application.Features.Service.Roster.Get;
using SAMGestor.Application.Features.Service.Roster.Unassigned;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Registration;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/registrations")]
[SwaggerOrder(11)] // Ordem no Swagger (depois do Registrations)
[SwaggerTag("Operações relacionadas às inscrições de serviço para retiros.")]
public class ServiceRegistrationsController(
    IMediator mediator,
    IStorageService storage,
    IServiceRegistrationRepository regRepo,
    IUnitOfWork uow
) : ControllerBase
{
    private CancellationToken CT => HttpContext?.RequestAborted ?? CancellationToken.None;

    #region Request Models

    public sealed class UpdateServiceRegistrationRequest
    {
        public string Name { get; set; } = default!;
        public string Cpf { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public DateOnly BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string City { get; set; } = default!;
        public MaritalStatus MaritalStatus { get; set; }
        public PregnancyStatus Pregnancy { get; set; }
        public ShirtSize ShirtSize { get; set; }
        public decimal WeightKg { get; set; }
        public decimal HeightCm { get; set; }
        public string Profession { get; set; } = default!;
        public EducationLevel EducationLevel { get; set; }
        public string StreetAndNumber { get; set; } = default!;
        public string Neighborhood { get; set; } = default!;
        public UF State { get; set; }
        public string PostalCode { get; set; } = default!;
        public string Whatsapp { get; set; } = default!;
        public RahaminVidaEdition RahaminVidaCompleted { get; set; }
        public RahaminAttempt PreviousUncalledApplications { get; set; }
        public string? PostRetreatLifeSummary { get; set; }
        public string ChurchLifeDescription { get; set; } = default!;
        public string PrayerLifeDescription { get; set; } = default!;
        public string FamilyRelationshipDescription { get; set; } = default!;
        public string SelfRelationshipDescription { get; set; } = default!;
        public Guid? PreferredSpaceId { get; set; }
    }

    #endregion

    /// <summary>
    /// Criar uma nova inscrição de serviço para um retiro específico.
    /// (Público)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        Summary = "Cria uma nova inscrição de serviço",
        Description = "Registra um novo membro de serviço em um retiro e retorna os dados da inscrição criada."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Inscrição criada com sucesso.", typeof(CreateServiceRegistrationResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Request inválido ou erros de validação.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Já existe inscrição para o CPF/E-mail informado.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro inesperado ao criar a inscrição.")]
    public async Task<IActionResult> Create(
        Guid retreatId,
        [FromBody] CreateServiceRegistrationCommand body)
    {
        if (body is null)
            return BadRequest();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var enriched = body with
        {
            RetreatId = retreatId,
            ClientIp = ip,
            UserAgent = userAgent
        };

        var result = await mediator.Send(enriched, CT);

        return CreatedAtRoute(
            routeName: nameof(GetServiceRegistrationById),
            routeValues: new { retreatId, id = result.ServiceRegistrationId },
            value: result
        );
    }

    /// <summary>
    /// Obter os detalhes completos de uma inscrição de serviço específica por ID.
    /// (Admin, Gestor, Consultor)
    /// </summary>
    [HttpGet("{id:guid}", Name = nameof(GetServiceRegistrationById))]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Obtém detalhes de uma inscrição de serviço",
        Description = "Retorna todos os dados de uma inscrição de serviço específica."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Inscrição encontrada.", typeof(GetServiceRegistrationResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Inscrição não encontrada.")]
    public async Task<IActionResult> GetServiceRegistrationById(Guid retreatId, Guid id)
    {
        var dto = await mediator.Send(new GetServiceRegistrationQuery(retreatId, id), CT);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Lista as inscrições de serviço de um retiro com filtros e paginação.
    /// (Admin, Gestor, Consultor)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Lista inscrições de serviço com filtros",
        Description = "Retorna lista paginada de inscrições de serviço com opções de filtro."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Lista de inscrições.", typeof(PagedResult<ServiceRegistrationDto>))]
    public async Task<ActionResult<PagedResult<ServiceRegistrationDto>>> List(
        [FromRoute] Guid retreatId,
        [FromQuery] ServiceRegistrationStatus? status = null,
        [FromQuery] Gender? gender = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] string? city = null,
        [FromQuery] UF? state = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? hasPhoto = null,
        [FromQuery] Guid? preferredSpaceId = null,
        [FromQuery] bool? isAssigned = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        static string? Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var result = await mediator.Send(
            new GetAllServiceRegistrationsQuery(
                retreatId,
                status,
                gender,
                minAge,
                maxAge,
                Clean(city),
                state,
                Clean(search),
                hasPhoto,
                preferredSpaceId,
                isAssigned,
                skip,
                take
            ),
            CT
        );

        return Ok(result);
    }

    /// <summary>
    /// Obter a lista de inscrições de serviço confirmadas para um retiro específico.
    /// (Admin, Gestor, Consultor)
    /// </summary>
    [HttpGet("confirmed")]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Lista inscrições confirmadas",
        Description = "Retorna lista de todas as inscrições de serviço confirmadas (pagamento aprovado)."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Lista de confirmados.", typeof(IReadOnlyList<GetConfirmedServiceRegistrationsResponse>))]
    public async Task<IActionResult> GetConfirmed([FromRoute] Guid retreatId)
    {
        var result = await mediator.Send(new GetConfirmedServiceRegistrationsQuery(retreatId), CT);
        return Ok(result);
    }

    /// <summary>
    /// Obter a lista de membros de serviço atribuídos para um retiro específico.
    /// (Admin, Gestor, Consultor)
    /// </summary>
    [HttpGet("roster")]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Lista roster de serviço",
        Description = "Retorna lista de membros de serviço já atribuídos a espaços."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Roster de serviço.", typeof(GetServiceRosterResponse))]
    public async Task<ActionResult<GetServiceRosterResponse>> GetRoster(Guid retreatId)
        => Ok(await mediator.Send(new GetServiceRosterQuery(retreatId), CT));

    /// <summary>
    /// Obter a lista de membros de serviço não atribuídos para um retiro específico.
    /// (Admin, Gestor, Consultor)
    /// </summary>
    [HttpGet("roster/unassigned")]
    [Authorize(Policy = Policies.ReadOnly)]
    [SwaggerOperation(
        Summary = "Lista membros não atribuídos",
        Description = "Retorna lista de membros de serviço que ainda não foram atribuídos a espaços."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Membros não atribuídos.", typeof(GetUnassignedServiceMembersResponse))]
    public async Task<ActionResult<GetUnassignedServiceMembersResponse>> GetUnassigned(Guid retreatId)
        => Ok(await mediator.Send(new GetUnassignedServiceMembersQuery(retreatId), CT));

    /// <summary>
    /// Faz upload da foto do membro de serviço.
    /// (Público)
    /// </summary>
    [HttpPost("{id:guid}/photo")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Upload de foto",
        Description = "Faz upload da foto do membro de serviço inscrito."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Foto enviada com sucesso.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Arquivo inválido.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Inscrição não encontrada.")]
    public async Task<IActionResult> UploadPhoto(Guid retreatId, Guid id, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Arquivo de foto é obrigatório.");

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png"))
            return BadRequest("A foto deve ser JPG ou PNG.");

        const int MaxPhotoBytes = 5 * 1024 * 1024; // 5MB
        if (file.Length > MaxPhotoBytes)
            return BadRequest("A foto deve ter no máximo 5MB.");

        var reg = await regRepo.GetByIdForUpdateAsync(id, CT);
        if (reg is null) return NotFound();

        if (reg.RetreatId != retreatId)
            return BadRequest("Inscrição não pertence a este retiro.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = contentType == "image/png" ? ".png" : ".jpg";

        var key = $"retreats/{retreatId}/service-regs/{id}/photo{ext}";
        using var stream = file.OpenReadStream();
        var (savedKey, size) = await storage.SaveAsync(stream, key, contentType!, CT);

        var publicUrl = new UrlAddress(storage.GetPublicUrl(savedKey));
        reg.SetPhoto(savedKey, contentType, size, DateTime.UtcNow, publicUrl);

        await uow.SaveChangesAsync(CT);

        return Created(publicUrl.Value, new { key = savedKey, url = publicUrl.Value, size });
    }

    /// <summary>
    /// Atualiza os dados de uma inscrição de serviço existente.
    /// Aceita dados via multipart/form-data incluindo opcionalmente photo (IFormFile).
    /// (Admin, Gestor)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [ApiExplorerSettings(IgnoreApi = true)]  // Oculto do Swagger
    public async Task<IActionResult> Update(
        Guid retreatId,
        Guid id,
        [FromForm] UpdateServiceRegistrationRequest request,
        [FromForm] IFormFile? photo)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        // Validar e processar foto
        string? photoKey = null, photoContentType = null, photoUrl = null;
        long? photoSize = null;

        if (photo is not null)
        {
            if (photo.Length == 0)
                return BadRequest("Arquivo de foto está vazio.");

            var photoType = photo.ContentType?.ToLowerInvariant();
            if (photoType is not ("image/jpeg" or "image/png"))
                return BadRequest("A foto deve ser JPG ou PNG.");

            const int MaxPhotoBytes = 5 * 1024 * 1024;
            if (photo.Length > MaxPhotoBytes)
                return BadRequest("A foto deve ter no máximo 5MB.");

            var reg = await regRepo.GetByIdAsync(id, CT);
            if (reg is null) return NotFound("Inscrição não encontrada.");

            if (reg.RetreatId != retreatId)
                return BadRequest("Inscrição não pertence a este retiro.");

            var ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = photoType == "image/png" ? ".png" : ".jpg";

            photoKey = $"retreats/{retreatId}/service-regs/{id}/photo{ext}";
            using var photoStream = photo.OpenReadStream();
            var (savedPhotoKey, savedPhotoSize) = await storage.SaveAsync(photoStream, photoKey, photoType!, CT);

            photoKey = savedPhotoKey;
            photoSize = savedPhotoSize;
            photoContentType = photoType;
            photoUrl = storage.GetPublicUrl(savedPhotoKey);
        }

        var command = new UpdateServiceRegistrationCommand(
            id,
            new FullName(request.Name),
            new CPF(request.Cpf),
            new EmailAddress(request.Email),
            request.Phone,
            request.BirthDate,
            request.Gender,
            request.City,
            request.MaritalStatus,
            request.Pregnancy,
            request.ShirtSize,
            request.WeightKg,
            request.HeightCm,
            request.Profession,
            request.EducationLevel,
            request.StreetAndNumber,
            request.Neighborhood,
            request.State,
            request.PostalCode,
            request.Whatsapp,
            request.RahaminVidaCompleted,
            request.PreviousUncalledApplications,
            request.PostRetreatLifeSummary,
            request.ChurchLifeDescription,
            request.PrayerLifeDescription,
            request.FamilyRelationshipDescription,
            request.SelfRelationshipDescription,
            request.PreferredSpaceId,
            photoKey,
            photoContentType,
            photoSize,
            photoUrl
        );

        var result = await mediator.Send(command, CT);
        return Ok(result);
    }

    /// <summary>
    /// Retorna as opções de enums e restrições para inscrições de serviço.
    /// (Público)
    /// </summary>
    [HttpGet("options")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Obtém opções de formulário",
        Description = "Retorna enums, constraints e regras para o formulário de inscrição de serviço."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Opções disponíveis.")]
    public IActionResult GetOptions()
    {
        return Ok(new
        {
            enums = new
            {
                gender = MapEnum<Gender>(),
                maritalStatus = MapEnum<MaritalStatus>(),
                pregnancy = MapEnum<PregnancyStatus>(),
                shirtSize = MapEnum<ShirtSize>(),
                uf = MapEnum<UF>(),
                educationLevel = MapEnum<EducationLevel>(),
                rahaminAttempt = MapEnum<RahaminAttempt>(flags: true),
                rahaminVidaCompleted = MapEnum<RahaminVidaEdition>(flags: true),
                serviceRegistrationStatus = MapEnum<ServiceRegistrationStatus>()
            },
            constraints = new
            {
                phoneDigitsMin = 10,
                phoneDigitsMax = 11,
                maxPhotoBytes = 5 * 1024 * 1024,
                acceptedPhotoTypes = new[] { "image/jpeg", "image/png" },
                minDescriptionLength = 50,
                maxDescriptionLength = 1000,
                minAge = 18,
                maxAge = 80
            },
            rules = new
            {
                pregnancyVisibleForGender = "Female"
            }
        });
    }

    #region Helper Methods

    private static object MapEnum<T>(bool flags = false) where T : Enum
    {
        var type = typeof(T);
        var isFlags = flags || type.GetCustomAttribute<FlagsAttribute>() != null;
        var items = Enum.GetValues(type).Cast<Enum>()
            .Where(v => !isFlags || IsSingleFlag(Convert.ToInt32(v)))
            .Select(v => new EnumOption
            {
                Name = v.ToString(),
                Value = Convert.ToInt32(v),
                Label = ToLabel(v.ToString())
            })
            .ToList();

        return new EnumGroup { IsFlags = isFlags, Items = items };
    }

    private static bool IsSingleFlag(int x) => x == 0 || (x & (x - 1)) == 0;

    private static string ToLabel(string name) => name.Replace('_', ' ');

    private sealed class EnumGroup
    {
        public bool IsFlags { get; set; }
        public List<EnumOption> Items { get; set; } = new();
    }

    private sealed class EnumOption
    {
        public string Name { get; set; } = default!;
        public int Value { get; set; }
        public string Label { get; set; } = default!;
    }

    #endregion
}
