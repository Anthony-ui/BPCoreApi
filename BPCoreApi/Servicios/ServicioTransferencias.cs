using System.Data;
using System.Text.Json;
using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;
using BPCoreApi.TiempoReal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Servicios;

public sealed class ServicioTransferencias(
    ContextoBanco context,
    IProcesadorCoreBancario procesadorCore,
    IEnumerable<IProveedorNotificaciones> proveedores,
    IServicioClientes servicioClientes,
    IServicioValidacionCuentas servicioValidacionCuentas,
    IHubContext<HubNotificaciones> hubNotificaciones) : IServicioTransferencias
{
    private readonly ContextoBanco _context = context;
    private readonly IProcesadorCoreBancario _procesadorCore = procesadorCore;
    private readonly IReadOnlyCollection<IProveedorNotificaciones> _proveedores = proveedores.ToArray();
    private readonly IServicioClientes _servicioClientes = servicioClientes;
    private readonly IServicioValidacionCuentas _servicioValidacionCuentas = servicioValidacionCuentas;
    private readonly IHubContext<HubNotificaciones> _hubNotificaciones = hubNotificaciones;

    public async Task<TransferenciaRespuesta> TransferirAsync(
        Guid clienteId, TransferenciaSolicitud solicitud, CancellationToken cancelacion)
    {
        var repetida = await _context.Transferencias.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClaveIdempotencia == solicitud.ClaveIdempotencia, cancelacion);
        if (repetida is not null)
        {
            return Mapear(repetida);
        }
        if (!solicitud.DestinoValidadoInternamente
            && !_servicioValidacionCuentas.Consumir(
                clienteId,
                solicitud.CuentaDestino,
                solicitud.InstitucionDestino,
                solicitud.ComprobanteValidacionDestino))
        {
            throw new ExcepcionNegocio(
                "Debes validar la cuenta destino antes de realizar la transferencia.");
        }

        await using var transaccionDb =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);

        var cuenta = await _context.Cuentas
            .Include(x => x.Cliente)
            .SingleOrDefaultAsync(
                x => x.Id == solicitud.CuentaOrigenId && x.ClienteId == clienteId && x.Activa,
                cancelacion)
            ?? throw new ExcepcionNegocio("La cuenta de origen no existe o no pertenece al cliente.");

        if (!string.Equals(cuenta.Moneda.Trim(), solicitud.Moneda, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExcepcionNegocio("La moneda no corresponde con la cuenta de origen.");
        }
        if (cuenta.SaldoDisponible < solicitud.Monto)
        {
            throw new ExcepcionNegocio("Saldo insuficiente.");
        }

        var esTransferenciaBp =
            solicitud.InstitucionDestino.Equals("BP", StringComparison.OrdinalIgnoreCase)
            || solicitud.InstitucionDestino.Contains(
                "Banco BP",
                StringComparison.OrdinalIgnoreCase);
        Cuenta? cuentaDestinoBp = null;
        if (esTransferenciaBp)
        {
            cuentaDestinoBp = await _context.Cuentas
                .Include(x => x.Cliente)
                .SingleOrDefaultAsync(
                    x => x.Numero == solicitud.CuentaDestino && x.Activa,
                    cancelacion)
                ?? throw new ExcepcionNegocio(
                    "La cuenta destino de Banco BP no existe o está inactiva.");
            if (cuentaDestinoBp.Id == cuenta.Id)
            {
                throw new ExcepcionNegocio(
                    "La cuenta de origen y la cuenta destino no pueden ser la misma.");
            }
            if (!string.Equals(
                    cuentaDestinoBp.Moneda.Trim(),
                    solicitud.Moneda,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ExcepcionNegocio(
                    "La moneda de la cuenta destino no corresponde con la transferencia.");
            }
        }

        var referenciaCore = await _procesadorCore.TransferirAsync(
            cuenta.Numero, solicitud.CuentaDestino, solicitud.Monto, solicitud.Moneda, cancelacion);
        cuenta.SaldoDisponible -= solicitud.Monto;
        if (cuentaDestinoBp is not null)
        {
            cuentaDestinoBp.SaldoDisponible += solicitud.Monto;
        }

        var ahora = DateTime.UtcNow;
        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            CuentaOrigenId = cuenta.Id,
            BeneficiarioId = solicitud.BeneficiarioId,
            CuentaDestino = solicitud.CuentaDestino,
            InstitucionDestino = solicitud.InstitucionDestino,
            Monto = solicitud.Monto,
            Moneda = solicitud.Moneda,
            Concepto = solicitud.Concepto,
            Estado = "COMPLETADA",
            ClaveIdempotencia = solicitud.ClaveIdempotencia,
            ReferenciaCore = referenciaCore,
            CreadoEn = ahora,
            ProcesadoEn = ahora
        };
        _context.Transferencias.Add(transferencia);
        _context.Movimientos.Add(new Movimiento
        {
            Id = Guid.NewGuid(),
            CuentaId = cuenta.Id,
            TransferenciaId = transferencia.Id,
            ReferenciaCore = referenciaCore,
            Tipo = "DEBITO",
            Monto = solicitud.Monto,
            SaldoResultante = cuenta.SaldoDisponible,
            Descripcion = solicitud.Concepto ?? "Transferencia",
            OcurridoEn = ahora,
            CreadoEn = ahora
        });
        if (cuentaDestinoBp is not null)
        {
            _context.Movimientos.Add(new Movimiento
            {
                Id = Guid.NewGuid(),
                CuentaId = cuentaDestinoBp.Id,
                TransferenciaId = transferencia.Id,
                ReferenciaCore = referenciaCore,
                Tipo = "CREDITO",
                Monto = solicitud.Monto,
                SaldoResultante = cuentaDestinoBp.SaldoDisponible,
                Descripcion = solicitud.Concepto ?? "Transferencia recibida",
                OcurridoEn = ahora,
                CreadoEn = ahora
            });
        }

        foreach (var proveedor in _proveedores)
        {
            var destino = proveedor.Canal == "EMAIL"
                ? cuenta.Cliente.Correo
                : cuenta.Cliente.Telefono;
            _context.Notificaciones.Add(new Notificacione
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                TransferenciaId = transferencia.Id,
                Canal = proveedor.Canal,
                Proveedor = proveedor.Nombre,
                DestinoEnmascarado = proveedor.Enmascarar(destino),
                Estado = "PENDIENTE",
                CreadoEn = ahora
            });
        }
        if (cuentaDestinoBp is not null)
        {
            foreach (var proveedor in _proveedores)
            {
                var destino = proveedor.Canal == "EMAIL"
                    ? cuentaDestinoBp.Cliente.Correo
                    : cuentaDestinoBp.Cliente.Telefono;
                _context.Notificaciones.Add(new Notificacione
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cuentaDestinoBp.ClienteId,
                    TransferenciaId = transferencia.Id,
                    Canal = proveedor.Canal,
                    Proveedor = proveedor.Nombre,
                    DestinoEnmascarado = proveedor.Enmascarar(destino),
                    Estado = "PENDIENTE",
                    CreadoEn = ahora
                });
            }
        }

        _context.Eventos.Add(new Evento
        {
            ClienteId = clienteId,
            Accion = "TRANSFERENCIA_CREADA",
            Recurso = "transferencias",
            RecursoId = transferencia.Id.ToString(),
            Resultado = "EXITOSO",
            CorrelacionId = Guid.NewGuid(),
            Datos = JsonSerializer.Serialize(new { solicitud.Monto, solicitud.Moneda }),
            OcurridoEn = ahora
        });
        await _context.SaveChangesAsync(cancelacion);
        await transaccionDb.CommitAsync(cancelacion);
        _servicioClientes.Invalidar(clienteId);
        if (cuentaDestinoBp is not null)
        {
            _servicioClientes.Invalidar(cuentaDestinoBp.ClienteId);
            await _hubNotificaciones.Clients
                .Group(HubNotificaciones.CrearGrupo(cuentaDestinoBp.ClienteId))
                .SendAsync(
                    "RecibirNotificacion",
                    new NotificacionTiempoReal(
                        "TRANSFERENCIA_RECIBIDA",
                        "Transferencia recibida",
                        $"Recibiste una transferencia en tu cuenta terminada en {cuentaDestinoBp.Numero[^4..]}.",
                        solicitud.Monto,
                        solicitud.Moneda,
                        ahora),
                    cancelacion);
        }
        return Mapear(transferencia);
    }

    private static TransferenciaRespuesta Mapear(Transferencia transferencia) => new(
        transferencia.Id,
        transferencia.Estado,
        transferencia.ReferenciaCore ?? string.Empty,
        transferencia.Monto,
        transferencia.Moneda.Trim(),
        transferencia.ProcesadoEn ?? transferencia.CreadoEn);
}

public sealed class ExcepcionNegocio(string mensaje) : Exception(mensaje);
