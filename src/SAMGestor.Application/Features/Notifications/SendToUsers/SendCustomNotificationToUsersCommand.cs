using MediatR;

namespace SAMGestor.Application.Features.Notifications.SendToUsers;

public sealed record SendCustomNotificationToUsersCommand(
    Guid RetreatId,
    List<Guid> UserIds,
    string Subject,
    string Body,
    string? PreheaderText,
    string? CallToActionUrl,
    string? CallToActionText,
    string? SecondaryLinkUrl,
    string? SecondaryLinkText,
    string? ImageUrl
) : IRequest<SendCustomNotificationToUsersResult>;