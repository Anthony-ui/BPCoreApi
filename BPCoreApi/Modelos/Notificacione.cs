using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Notificacione
{
    public Guid Id { get; set; }

    public Guid ClienteId { get; set; }

    public Guid? TransferenciaId { get; set; }

    public string Canal { get; set; } = null!;

    public string Proveedor { get; set; } = null!;

    public string DestinoEnmascarado { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public short Intentos { get; set; }

    public string? ReferenciaProveedor { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? EnviadoEn { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Transferencia? Transferencia { get; set; }
}
