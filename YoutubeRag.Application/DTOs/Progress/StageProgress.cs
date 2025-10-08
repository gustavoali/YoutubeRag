namespace YoutubeRag.Application.DTOs.Progress;

/// <summary>
/// DTO para representar el progreso de un stage específico
/// </summary>
public class StageProgress
{
    /// <summary>
    /// Nombre del stage
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado del stage
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progreso del stage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Timestamp de inicio del stage
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp de completitud del stage
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Mensaje de error si el stage falló
    /// </summary>
    public string? ErrorMessage { get; set; }
}
