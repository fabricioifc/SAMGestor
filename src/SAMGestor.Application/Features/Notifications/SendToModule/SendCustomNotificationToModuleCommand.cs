using MediatR;

namespace SAMGestor.Application.Features.Notifications.SendToModule;

public sealed record SendCustomNotificationToModuleCommand(
    Guid RetreatId,
    string TargetModule, // "Fazer", "Servir", "Ambos"
    List<string>? StatusFilters, // null = todos os status
    string Subject,
    string Body,
    string? PreheaderText,
    string? CallToActionUrl,
    string? CallToActionText,
    string? SecondaryLinkUrl,
    string? SecondaryLinkText,
    string? ImageUrl
) : IRequest<SendCustomNotificationToModuleResult>;