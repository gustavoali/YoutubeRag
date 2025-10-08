namespace YoutubeRag.Application.DTOs.Progress;

/// <summary>
/// DTO para notificaciones de progreso general de un video
/// </summary>
public class VideoProgressDto
{
    /// <summary>
    /// ID del video
    /// </summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// Estado general de procesamiento
    /// </summary>
    public string ProcessingStatus { get; set; } = string.Empty;

    /// <summary>
    /// Estado de transcripción
    /// </summary>
    public string TranscriptionStatus { get; set; } = string.Empty;

    /// <summary>
    /// Estado de embeddings
    /// </summary>
    public string EmbeddingStatus { get; set; } = string.Empty;

    /// <summary>
    /// Progreso general (0-100)
    /// </summary>
    public int OverallProgress { get; set; }

    /// <summary>
    /// Progreso por stages individuales
    /// </summary>
    public List<StageProgress> Stages { get; set; } = new();

    /// <summary>
    /// Timestamp de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
