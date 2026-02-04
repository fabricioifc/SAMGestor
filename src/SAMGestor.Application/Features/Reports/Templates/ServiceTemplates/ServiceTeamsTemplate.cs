using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates.ServiceTemplates;

/// <summary>
/// Relatório de equipes de serviço com coordenadores e membros.
/// Exibe informações completas de cada espaço de serviço com alertas.
/// </summary>
public sealed class ServiceTeamsTemplate : IReportTemplate
{
    public string Key => "service-teams";
    public string DefaultTitle => "Equipes de Serviço e Membros";

    private readonly IReportingReadDb _readDb;

    public ServiceTeamsTemplate(IReportingReadDb readDb)
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
                .Where(a => spaceIds.Contains(a.ServiceSpaceId)),
            ct);

        var registrationIds = assignmentsFromDb.Select(a => a.ServiceRegistrationId).Distinct().ToList();

        var registrationsFromDb = await _readDb.ToListAsync(
            _readDb.ServiceRegistrations
                .Where(r => registrationIds.Contains(r.Id)
                            && r.Status == ServiceRegistrationStatus.Confirmed),
            ct);

        var regDict = registrationsFromDb.ToDictionary(r => r.Id);

        var spaceData = new List<ServiceSpaceData>();
        var totalCoordinators = 0;
        var totalViceCoordinators = 0;

        foreach (var space in spacesFromDb)
        {
            var assignments = assignmentsFromDb
                .Where(a => a.ServiceSpaceId == space.Id)
                .ToList();

            var coordinatorReg = assignments
                .Where(a => a.Role == ServiceRole.Coordinator)
                .Select(a => regDict.TryGetValue(a.ServiceRegistrationId, out var reg) ? reg : null)
                .FirstOrDefault(r => r != null);

            var viceCoordinatorReg = assignments
                .Where(a => a.Role == ServiceRole.Vice)
                .Select(a => regDict.TryGetValue(a.ServiceRegistrationId, out var reg) ? reg : null)
                .FirstOrDefault(r => r != null);

            var memberRegs = assignments
                .Where(a => a.Role == ServiceRole.Member)
                .Select(a => regDict.TryGetValue(a.ServiceRegistrationId, out var reg) ? reg : null)
                .Where(r => r != null)
                .ToList();

            if (coordinatorReg != null) totalCoordinators++;
            if (viceCoordinatorReg != null) totalViceCoordinators++;

            var currentCount = memberRegs.Count + (coordinatorReg != null ? 1 : 0) +
                               (viceCoordinatorReg != null ? 1 : 0);

            var alerts = GenerateAlerts(
                space.Name,
                space.MinPeople,
                space.MaxPeople,
                currentCount,
                coordinatorReg != null,
                viceCoordinatorReg != null
            );

            spaceData.Add(new ServiceSpaceData
            {
                SpaceId = space.Id,
                SpaceName = space.Name,
                Description = space.Description ?? "-",
                MinCapacity = space.MinPeople,
                MaxCapacity = space.MaxPeople,
                CurrentCount = currentCount,
                Coordinator = coordinatorReg,
                ViceCoordinator = viceCoordinatorReg,
                Members = memberRegs!,
                Alerts = alerts
            });
        } 

        var totalRecords = spaceData.Count;
        var page = take > 0 ? (skip / take) + 1 : 1;

        var pagedSpaces = take > 0
            ? spaceData.Skip(skip).Take(take).ToList()
            : spaceData;

        var data = pagedSpaces.Select(BuildSpaceRow).ToList();

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
            ["totalSpaces"] = spaceData.Count,
            ["totalMembers"] = spaceData.Sum(s => s.CurrentCount),
            ["totalCoordinators"] = totalCoordinators,
            ["totalViceCoordinators"] = totalViceCoordinators,
            ["spacesWithAlerts"] = spaceData.Count(s => s.Alerts.Count > 0)
        };

        return new ReportPayload(header, columns, data, summary, totalRecords, page, take);
    } 

    private static ColumnDef[] GetColumns() => new[]
    {
        new ColumnDef("spaceName", "Espaço"),
        new ColumnDef("description", "Descrição"),
        new ColumnDef("capacity", "Capacidade"),
        new ColumnDef("currentCount", "Total Atual"),
        new ColumnDef("coordinator", "Coordenador(a)"),
        new ColumnDef("viceCoordinator", "Vice-Coordenador(a)"),
        new ColumnDef("members", "Membros"),
        new ColumnDef("alerts", "Alertas")
    };

    private IDictionary<string, object?> BuildSpaceRow(ServiceSpaceData space)
    {
        var coordinatorData = space.Coordinator != null
            ? new Dictionary<string, object?>
            {
                ["name"] = space.Coordinator.Name.Value,
                ["phone"] = space.Coordinator.Phone ?? "-",
                ["email"] = space.Coordinator.Email.Value,
                ["city"] = space.Coordinator.City ?? "-",
                ["age"] = CalculateAge(space.Coordinator.BirthDate)
            }
            : null;

        var viceCoordinatorData = space.ViceCoordinator != null
            ? new Dictionary<string, object?>
            {
                ["name"] = space.ViceCoordinator.Name.Value,
                ["phone"] = space.ViceCoordinator.Phone ?? "-",
                ["email"] = space.ViceCoordinator.Email.Value,
                ["city"] = space.ViceCoordinator.City ?? "-",
                ["age"] = CalculateAge(space.ViceCoordinator.BirthDate)
            }
            : null;

        var membersData = space.Members
            .Select(m => new Dictionary<string, object?>
            {
                ["name"] = m.Name.Value,
                ["phone"] = m.Phone ?? "-",
                ["city"] = m.City ?? "-",
                ["age"] = CalculateAge(m.BirthDate),
                ["role"] = "Member"
            })
            .ToList();

        return new Dictionary<string, object?>
        {
            ["spaceName"] = space.SpaceName,
            ["description"] = space.Description,
            ["capacity"] = $"{space.MinCapacity}-{space.MaxCapacity} pessoas",
            ["currentCount"] = space.CurrentCount,
            ["coordinator"] = coordinatorData,
            ["viceCoordinator"] = viceCoordinatorData,
            ["members"] = membersData,
            ["alerts"] = space.Alerts
        };
    }

    private static List<Dictionary<string, object?>> GenerateAlerts(
        string spaceName,
        int minCapacity,
        int maxCapacity,
        int currentCount,
        bool hasCoordinator,
        bool hasViceCoordinator)
    {
        var alerts = new List<Dictionary<string, object?>>();

        if (!hasCoordinator)
        {
            alerts.Add(new Dictionary<string, object?>
            {
                ["code"] = "MissingCoordinator",
                ["severity"] = "warning",
                ["message"] = $"Espaço '{spaceName}' sem Coordenador."
            });
        }

        if (!hasViceCoordinator)
        {
            alerts.Add(new Dictionary<string, object?>
            {
                ["code"] = "MissingVice",
                ["severity"] = "warning",
                ["message"] = $"Espaço '{spaceName}' sem Vice-Coordenador."
            });
        }

        if (currentCount < minCapacity)
        {
            alerts.Add(new Dictionary<string, object?>
            {
                ["code"] = "BelowMin",
                ["severity"] = "warning",
                ["message"] = $"Alocados ({currentCount}) abaixo do mínimo ({minCapacity}) em '{spaceName}'."
            });
        }

        if (currentCount > maxCapacity)
        {
            alerts.Add(new Dictionary<string, object?>
            {
                ["code"] = "OverMax",
                ["severity"] = "error",
                ["message"] = $"Alocados ({currentCount}) acima do máximo ({maxCapacity}) em '{spaceName}'."
            });
        }

        return alerts;
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
            TemplateKey: "service-teams",
            Title: "Equipes de Serviço e Membros",
            Category: "Serviço",
            GeneratedAt: DateTime.UtcNow,
            RetreatId: retreatId,
            RetreatName: ctx.RetreatName
        );

        var summary = new Dictionary<string, object?>
        {
            ["totalSpaces"] = 0,
            ["totalMembers"] = 0,
            ["totalCoordinators"] = 0,
            ["totalViceCoordinators"] = 0,
            ["spacesWithAlerts"] = 0
        };

        return new ReportPayload(header, columns, new List<IDictionary<string, object?>>(), summary, 0, 1, 0);
    }

    private sealed class ServiceSpaceData
    {
        public Guid SpaceId { get; set; }
        public string SpaceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MinCapacity { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentCount { get; set; }
        public ServiceRegistration? Coordinator { get; set; }
        public ServiceRegistration? ViceCoordinator { get; set; }
        public List<ServiceRegistration> Members { get; set; } = new();
        public List<Dictionary<string, object?>> Alerts { get; set; } = new();
    }
}
