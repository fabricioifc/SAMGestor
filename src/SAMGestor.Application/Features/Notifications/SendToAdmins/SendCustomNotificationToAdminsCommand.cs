using MediatR;

namespace SAMGestor.Application.Features.Notifications.SendToAdmins;

public sealed record SendCustomNotificationToAdminsCommand(
    List<Guid>? UserIds, // null = todos os admins
    string Subject,
    string Body,
    string? PreheaderText,
    string? CallToActionUrl,
    string? CallToActionText,
    string? SecondaryLinkUrl,
    string? SecondaryLinkText,
    string? ImageUrl
) : IRequest<SendCustomNotificationToAdminsResult>;