using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Beneficiario
{
    public Guid Id { get; set; }

    public Guid ClienteId { get; set; }

    public string Alias { get; set; } = null!;

    public string NumeroCuenta { get; set; } = null!;

    public string InstitucionFinanciera { get; set; } = null!;

    public string IdentificacionBeneficiario { get; set; } = null!;

    public string NombreBeneficiario { get; set; } = null!;

    public bool EsCuentaPropia { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<Transferencia> Transferencia { get; set; } = new List<Transferencia>();
}
