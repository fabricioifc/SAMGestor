using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;


public interface IReportTemplateRegistry
{
    IReadOnlyList<IReportTemplate> GetAllTemplates();
    IReportTemplate? GetTemplate(string key);
}