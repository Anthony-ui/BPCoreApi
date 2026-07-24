using System.ComponentModel.DataAnnotations;

namespace BPCoreApi.Contratos;

public sealed class SolicitudCreacionUsuario
{
    [Required, StringLength(20)]
    public string Identificacion { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Nombres { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Apellidos { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Correo { get; init; } = string.Empty;

    [Required, StringLength(30)]
    public string Telefono { get; init; } = string.Empty;

    public DateOnly FechaNacimiento { get; init; }

    [Required, StringLength(80, MinimumLength = 5)]
    public string NombreUsuario { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Clave { get; init; } = string.Empty;

    [Required, StringLength(24, MinimumLength = 6)]
    public string NumeroCuenta { get; init; } = string.Empty;

    [RegularExpression("^(AHORROS|CORRIENTE)$")]
    public string TipoCuenta { get; init; } = "AHORROS";

    [Range(0, 999999999999.99)]
    public decimal SaldoInicial { get; init; }
}

public sealed record UsuarioCreadoRespuesta(
    Guid ClienteId,
    Guid UsuarioId,
    Guid CuentaId,
    string NombreUsuario,
    string NumeroCuenta);
