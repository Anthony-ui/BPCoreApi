using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class FactoresAutenticacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? ReferenciaProveedor { get; set; }

    public DateTime? VerificadoEn { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
