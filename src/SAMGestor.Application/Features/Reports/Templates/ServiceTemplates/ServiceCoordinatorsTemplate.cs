using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates.ServiceTemplates;

/// <summary>
/// Relatório de coordenadores e vice-coordenadores das equipes de serviço.
/// Exibe informações detalhadas dos líderes de cada espaço.
/// Agora traz TODOS os espaços, mesmo sem coordenador/vice, com alertas.
/// </summary>
public sealed class ServiceCoordinatorsTemplate : IReportTemplate
{
    public string Key => "service-coordinators";
    public string DefaultTitle => "Coordenadores das Equipes de Serviço";

    private readonly IReportingReadDb _readDb;

    public ServiceCoordinatorsTemplate(IReportingReadDb readDb)
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
        var spacesFromDb = await _readDb.ToListAsync(
            _readDb.ServiceSpaces
                .Where(s => s.RetreatId == retreatId && s.IsActive)
                .OrderBy(s => s.Name),
            ct);

        var columns = GetColumns();

        if (spacesFromDb.Count == 0)
            return CreateEmptyPayload(ctx, retreatId, columns);

        var spaceIds = spacesFromDb.Select(s => s.Id).ToList();

        var assignmentsFromDb = await _readDb.ToListAsync(
            _readDb.ServiceAssignments
                .Where(a => spaceIds.Contains(a.ServiceSpaceId) 
                         && (a.Role == ServiceRole.Coordinator || a.Role == ServiceRole.Vice)),
            ct);

        var registrationIds = assignmentsFromDb.Select(a => a.ServiceRegistrationId).Distinct().ToList();

        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.ServiceRegistrations
                .Where(r => registrationIds.Contains(r.Id) 
                         && r.Status == ServiceRegistrationStatus.Confirmed)
                .Select(r => new {
                    r.Id,
                    Name = r.Name.Value,
                    Cpf = r.Cpf.Value,
                    Email = r.Email.Value,
                    r.Phone,
                    r.Whatsapp,
                    r.City,
                    r.BirthDate,
                    r.Gender,
                    r.Profession,
                    r.EducationLevel,
                    r.PreferredSpaceId,
                    PhotoUrl = r.PhotoUrl != null ? r.PhotoUrl.Value : null,
                    r.PhotoStorageKey
                }),
            ct);

        var regDict = registrationsFromDb.ToDictionary(r => r.Id);

        var coordinatorData = new List<CoordinatorData>();

        foreach (var space in spacesFromDb)
        {
            var spaceAssignments = assignmentsFromDb
                .Where(a => a.ServiceSpaceId == space.Id)
                .ToList();

            var coordinatorAssignment = spaceAssignments
                .FirstOrDefault(a => a.Role == ServiceRole.Coordinator);

            var viceCoordinatorAssignment = spaceAssignments
                .FirstOrDefault(a => a.Role == ServiceRole.Vice);

            var coordinator = coordinatorAssignment != null && 
                            regDict.TryGetValue(coordinatorAssignment.ServiceRegistrationId, out var coord)
                ? coord
                : null;

            var viceCoordinator = viceCoordinatorAssignment != null && 
                                regDict.TryGetValue(viceCoordinatorAssignment.ServiceRegistrationId, out var vice)
                ? vice
                : null;

            var alerts = new List<string>();
            if (coordinator == null)
                alerts.Add("Coordenador não atribuído");
            if (viceCoordinator == null)
                alerts.Add("Vice-Coordenador não atribuído");

            coordinatorData.Add(new CoordinatorData
            {
                SpaceName = space.Name,
                Coordinator = coordinator,
                CoordinatorAssignedAt = coordinatorAssignment?.AssignedAt,
                ViceCoordinator = viceCoordinator,
                ViceCoordinatorAssignedAt = viceCoordinatorAssignment?.AssignedAt,
                Alerts = alerts
            });
        }

        var totalSpaces = coordinatorData.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;

        var pagedData = take > 0
            ? coordinatorData.Skip(skip).Take(take).ToList()
            : coordinatorData;

        var data = pagedData.Select(BuildCoordinatorRow).ToList();

        var header = new ReportHeader(
            TemplateKey: Key,
            Title: DefaultTitle,
            Category: "Serviço",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalSpaces"] = totalSpaces,
            ["totalCoordinators"] = coordinatorData.Count(c => c.Coordinator != null),
            ["totalViceCoordinators"] = coordinatorData.Count(c => c.ViceCoordinator != null),
            ["spacesWithoutCoordinator"] = coordinatorData.Count(c => c.Coordinator == null),
            ["spacesWithoutVice"] = coordinatorData.Count(c => c.ViceCoordinator == null)
        };

        return new ReportPayload(header, columns, data, summary, totalSpaces, page, take);
    }

    private static ColumnDef[] GetColumns() => new[]
    {
        new ColumnDef("spaceName", "Espaço"),
        new ColumnDef("coordinator", "Coordenador(a)"),
        new ColumnDef("viceCoordinator", "Vice-Coordenador(a)"),
        new ColumnDef("alerts", "Alertas")
    };

    private IDictionary<string, object?> BuildCoordinatorRow(CoordinatorData data)
    {
        var coordinatorInfo = data.Coordinator != null
            ? new Dictionary<string, object?>
            {
                ["name"] = data.Coordinator.Name,
                ["cpf"] = data.Coordinator.Cpf,
                ["email"] = data.Coordinator.Email,
                ["phone"] = data.Coordinator.Phone,
                ["whatsapp"] = data.Coordinator.Whatsapp ?? data.Coordinator.Phone,
                ["city"] = data.Coordinator.City,
                ["age"] = CalculateAge(data.Coordinator.BirthDate),
                ["gender"] = data.Coordinator.Gender.ToString(),
                ["profession"] = data.Coordinator.Profession ?? "-",
                ["educationLevel"] = data.Coordinator.EducationLevel?.ToString() ?? "-",
                ["preferredSpaceId"] = data.Coordinator.PreferredSpaceId?.ToString() ?? "-",
                ["photoUrl"] = data.Coordinator.PhotoUrl,
                ["photoStorageKey"] = data.Coordinator.PhotoStorageKey,
                ["assignedAt"] = data.CoordinatorAssignedAt?.ToString("dd/MM/yyyy HH:mm")
            }
            : null;

        var viceCoordinatorInfo = data.ViceCoordinator != null
            ? new Dictionary<string, object?>
            {
                ["name"] = data.ViceCoordinator.Name,
                ["cpf"] = data.ViceCoordinator.Cpf,
                ["email"] = data.ViceCoordinator.Email,
                ["phone"] = data.ViceCoordinator.Phone,
                ["whatsapp"] = data.ViceCoordinator.Whatsapp ?? data.ViceCoordinator.Phone,
                ["city"] = data.ViceCoordinator.City,
                ["age"] = CalculateAge(data.ViceCoordinator.BirthDate),
                ["gender"] = data.ViceCoordinator.Gender.ToString(),
                ["profession"] = data.ViceCoordinator.Profession ?? "-",
                ["educationLevel"] = data.ViceCoordinator.EducationLevel?.ToString() ?? "-",
                ["preferredSpaceId"] = data.ViceCoordinator.PreferredSpaceId?.ToString() ?? "-",
                ["photoUrl"] = data.ViceCoordinator.PhotoUrl,
                ["photoStorageKey"] = data.ViceCoordinator.PhotoStorageKey,
                ["assignedAt"] = data.ViceCoordinatorAssignedAt?.ToString("dd/MM/yyyy HH:mm")
            }
            : null;

        return new Dictionary<string, object?>
        {
            ["spaceName"] = data.SpaceName,
            ["coordinator"] = coordinatorInfo,
            ["viceCoordinator"] = viceCoordinatorInfo,
            ["alerts"] = data.Alerts
        };
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private static ReportPayload CreateEmptyPayload(ReportContext ctx, Guid retreatId, ColumnDef[] columns)
    {
        var header = new ReportHeader(
            TemplateKey: "service-coordinators",
            Title: "Coordenadores das Equipes de Serviço",
            Category: "Serviço",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalSpaces"] = 0,
            ["totalCoordinators"] = 0,
            ["totalViceCoordinators"] = 0,
            ["spacesWithoutCoordinator"] = 0,
            ["spacesWithoutVice"] = 0
        };

        return new ReportPayload(header, columns, new List<IDictionary<string, object?>>(), summary, 0, 1, 0);
    }

    private sealed class CoordinatorData
    {
        public string SpaceName { get; set; } = string.Empty;
        public dynamic? Coordinator { get; set; }
        public DateTimeOffset? CoordinatorAssignedAt { get; set; }
        public dynamic? ViceCoordinator { get; set; }
        public DateTimeOffset? ViceCoordinatorAssignedAt { get; set; }
        public List<string> Alerts { get; set; } = new();
    }
}
