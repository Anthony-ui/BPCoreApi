using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Movimiento
{
    public Guid Id { get; set; }

    public Guid CuentaId { get; set; }

    public Guid? TransferenciaId { get; set; }

    public string ReferenciaCore { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public decimal Monto { get; set; }

    public decimal? SaldoResultante { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime OcurridoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cuenta Cuenta { get; set; } = null!;

    public virtual Transferencia? Transferencia { get; set; }
}
