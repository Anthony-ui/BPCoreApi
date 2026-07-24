using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Transferencia
{
    public Guid Id { get; set; }

    public Guid CuentaOrigenId { get; set; }

    public Guid? BeneficiarioId { get; set; }

    public string CuentaDestino { get; set; } = null!;

    public string InstitucionDestino { get; set; } = null!;

    public decimal Monto { get; set; }

    public string Moneda { get; set; } = null!;

    public string? Concepto { get; set; }

    public string Estado { get; set; } = null!;

    public string ClaveIdempotencia { get; set; } = null!;

    public string? ReferenciaCore { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ProcesadoEn { get; set; }

    public virtual Beneficiario? Beneficiario { get; set; }

    public virtual Cuenta CuentaOrigen { get; set; } = null!;

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();
}
