namespace SAMGestor.Domain.Enums;

/// <summary>
/// Tipo de imagem associada ao retiro
/// </summary>
public enum ImageType
{
    /// <summary>
    /// Imagem principal/banner do retiro (1 por retiro)
    /// </summary>
    Banner = 0,
    
    /// <summary>
    /// Imagem para thumbnail/miniatura em listagens
    /// </summary>
    Thumbnail = 1,
    
    /// <summary>
    /// Imagens da galeria de fotos
    /// </summary>
    Gallery = 2
}