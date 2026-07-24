using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Usuario
{
    public Guid Id { get; set; }

    public Guid ClienteId { get; set; }

    public string? SujetoExterno { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime? UltimoAccesoEn { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public string? HashClave { get; set; }

    public string? SelloSeguridad { get; set; }

    public short IntentosFallidos { get; set; }

    public DateTime? UltimoCambioClaveEn { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<FactoresAutenticacion> FactoresAutenticacions { get; set; } = new List<FactoresAutenticacion>();
}
