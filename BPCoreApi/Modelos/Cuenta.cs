using System;
using System.Collections.Generic;

namespace BPCoreApi.Modelos;

public partial class Cuenta
{
    public Guid Id { get; set; }

    public Guid ClienteId { get; set; }

    public string Numero { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Moneda { get; set; } = null!;

    public decimal SaldoDisponible { get; set; }

    public bool Activa { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

    public virtual ICollection<Transferencia> Transferencia { get; set; } = new List<Transferencia>();
}
