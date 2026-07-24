using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Cliente
{
    public Guid Id { get; set; }

    public string Identificacion { get; set; } = null!;

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public DateOnly FechaNacimiento { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();

    public virtual ICollection<Cuenta> Cuenta { get; set; } = new List<Cuenta>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<Onboarding> Onboardings { get; set; } = new List<Onboarding>();

    public virtual Usuario? Usuario { get; set; }
}
