using System.ComponentModel.DataAnnotations;

namespace BPCoreApi.Contratos;

public sealed class SolicitudDeposito
{
    [Required, StringLength(24, MinimumLength = 6)]
    public string NumeroCuenta { get; init; } = string.Empty;

    [Range(0.01, 999999999999.99)]
    public decimal Monto { get; init; }
}

public sealed record DepositoRespuesta(
    Guid MovimientoId,
    string NumeroCuenta,
    decimal Monto,
    decimal SaldoResultante,
    string Referencia,
    DateTime ProcesadoEn);
