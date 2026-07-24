using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Onboarding
{
    public Guid Id { get; set; }

    public Guid? ClienteId { get; set; }

    public string Estado { get; set; } = null!;

    public string ProveedorBiometrico { get; set; } = null!;

    public string ReferenciaVerificacion { get; set; } = null!;

    public decimal? PuntajeRostro { get; set; }

    public bool? PruebaVidaSuperada { get; set; }

    public DateTime IniciadoEn { get; set; }

    public DateTime? FinalizadoEn { get; set; }

    public virtual Cliente? Cliente { get; set; }
}
