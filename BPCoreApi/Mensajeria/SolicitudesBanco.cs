using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BPCoreApi.Contratos;

public sealed class TransferenciaSolicitud
{
    [Required]
    public Guid CuentaOrigenId { get; init; }

    public Guid? BeneficiarioId { get; init; }

    [Required, StringLength(24, MinimumLength = 6)]
    public string CuentaDestino { get; init; } = string.Empty;

    [Required, StringLength(120)]
    public string InstitucionDestino { get; init; } = string.Empty;

    [Range(0.01, 999999999999.99)]
    public decimal Monto { get; init; }

    [RegularExpression("^[A-Z]{3}$")]
    public string Moneda { get; init; } = "USD";

    [StringLength(200)]
    public string? Concepto { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public string ClaveIdempotencia { get; init; } = string.Empty;

    public Guid? ComprobanteValidacionDestino { get; init; }

    [JsonIgnore]
    public bool DestinoValidadoInternamente { get; init; }
}

public sealed class ValidacionCuentaSolicitud
{
    [Required, StringLength(24, MinimumLength = 6)]
    public string CuentaDestino { get; init; } = string.Empty;

    [Required, StringLength(120)]
    public string InstitucionDestino { get; init; } = string.Empty;
}

public sealed class PagoSolicitud
{
    [Required]
    public Guid CuentaOrigenId { get; init; }

    [Required, StringLength(120)]
    public string Servicio { get; init; } = string.Empty;

    [Required, StringLength(60, MinimumLength = 4)]
    public string Referencia { get; init; } = string.Empty;

    [Range(0.01, 999999999999.99)]
    public decimal Monto { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public string ClaveIdempotencia { get; init; } = string.Empty;
}
