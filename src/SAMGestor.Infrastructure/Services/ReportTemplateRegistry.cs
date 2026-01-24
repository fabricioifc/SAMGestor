using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Infrastructure.Services;

public sealed class ReportTemplateRegistry : IReportTemplateRegistry
{
    private readonly IReadOnlyDictionary<string, IReportTemplate> _templates;

    public ReportTemplateRegistry(IEnumerable<IReportTemplate> templates)
    {
        _templates = templates
            .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(t => t.Key, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IReportTemplate> GetAllTemplates()
    {
        return _templates.Values
            .OrderBy(t => t.DefaultTitle)
            .ToList();
    }

    public IReportTemplate? GetTemplate(string key)
    {
        _templates.TryGetValue(key, out var template);
        return template;
    }
}