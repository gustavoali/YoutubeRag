namespace YoutubeRag.Application.DTOs.Progress;

/// <summary>
/// DTO para notificaciones de progreso de jobs
/// </summary>
public class JobProgressDto
{
    /// <summary>
    /// ID único del job
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// ID del video asociado
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// Tipo de job (Transcription, Embedding, etc)
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual del job
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progreso del job (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Stage actual del job
    /// </summary>
    public string? CurrentStage { get; set; }

    /// <summary>
    /// Mensaje descriptivo del estado actual
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Mensaje de error si el job falló
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Metadata adicional del job
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
