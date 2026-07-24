using System.ComponentModel.DataAnnotations;

namespace BPCoreApi.Contratos;

public sealed class SolicitudInicioSesion
{
    [Required]
    public string Usuario { get; init; } = string.Empty;

    [Required]
    public string Clave { get; init; } = string.Empty;

    [Required]
    public string Retorno { get; init; } = string.Empty;
}
