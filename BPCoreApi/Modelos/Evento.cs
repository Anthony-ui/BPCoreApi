using System;
using System.Collections.Generic;
using System.Net;

namespace BPCoreApi.Modelos;

public partial class Evento
{
    public long Id { get; set; }

    public Guid? UsuarioId { get; set; }

    public Guid? ClienteId { get; set; }

    public string Accion { get; set; } = null!;

    public string Recurso { get; set; } = null!;

    public string? RecursoId { get; set; }

    public string Resultado { get; set; } = null!;

    public IPAddress? DireccionIp { get; set; }

    public string? AgenteUsuario { get; set; }

    public Guid CorrelacionId { get; set; }

    public string Datos { get; set; } = null!;

    public DateTime OcurridoEn { get; set; }
}
