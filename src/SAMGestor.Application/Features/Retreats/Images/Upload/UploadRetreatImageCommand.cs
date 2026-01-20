using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.Images.Upload;

/// <summary>
/// Comando para upload de imagem do retiro
/// Se já existir Banner/Thumbnail, substitui automaticamente
/// </summary>

public sealed record UploadRetreatImageCommand(
    Guid RetreatId,
    ImageType Type,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedByUserId,
    string? AltText = null,
    int Order = 0
) : IRequest<UploadRetreatImageResult>;