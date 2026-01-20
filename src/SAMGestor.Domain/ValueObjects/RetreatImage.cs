using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.ValueObjects;

public sealed class RetreatImage : ValueObject
{
  
    public string ImageUrl { get; private set; } = string.Empty;
    public string StorageId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Tipo da imagem (Banner, Thumbnail, Gallery)
    /// </summary>
    public ImageType Type { get; private set; }
    
    /// <summary>
    /// Ordem de exibição (usado principalmente para galeria)
    /// </summary>
    public int Order { get; private set; }
    
    public DateTime UploadedAt { get; private set; }
    
    /// <summary>
    /// Texto alternativo para acessibilidade
    /// </summary>
    public string? AltText { get; private set; }

    private RetreatImage() 
    {
        ImageUrl = string.Empty;
        StorageId = string.Empty;
        Type = ImageType.Banner;
        Order = 0;
        UploadedAt = DateTime.UtcNow;
    }
    
    public RetreatImage(
        string imageUrl, 
        string storageId, 
        ImageType type, 
        int order,
        string? altText = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("URL da imagem é obrigatória.", nameof(imageUrl));
        
        if (string.IsNullOrWhiteSpace(storageId))
            throw new ArgumentException("ID de armazenamento é obrigatório.", nameof(storageId));
        
        if (order < 0)
            throw new ArgumentException("Ordem deve ser maior ou igual a zero.", nameof(order));

        ImageUrl = imageUrl.Trim();
        StorageId = storageId.Trim();
        Type = type;
        Order = order;
        UploadedAt = DateTime.UtcNow;
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
    }

    public RetreatImage WithOrder(int newOrder)
    {
        if (newOrder < 0)
            throw new ArgumentException("Ordem deve ser maior ou igual a zero.", nameof(newOrder));
        
        return new RetreatImage(ImageUrl, StorageId, Type, newOrder, AltText);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StorageId;
        yield return Type;
    }
}
