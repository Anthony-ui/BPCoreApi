using System.Data;
using System.Text.Json;
using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;
using BPCoreApi.Servicios;
using BPCoreApi.TiempoReal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/administracion/depositos")]
public sealed class DepositosController(
    ContextoBanco context,
    IEnumerable<IProveedorNotificaciones> proveedores,
    IServicioClientes servicioClientes,
    IHubContext<HubNotificaciones> hubNotificaciones) : ControllerBase
{
    private readonly ContextoBanco _context = context;
    private readonly IReadOnlyCollection<IProveedorNotificaciones> _proveedores =
        proveedores.ToArray();
    private readonly IServicioClientes _servicioClientes = servicioClientes;
    private readonly IHubContext<HubNotificaciones> _hubNotificaciones = hubNotificaciones;

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<DepositoRespuesta>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepositoRespuesta>> Crear(
        [FromBody] SolicitudDeposito solicitud,
        CancellationToken cancelacion)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelacion);
        var numeroCuenta = solicitud.NumeroCuenta.Trim();
        var cuenta = await _context.Cuentas
            .Include(x => x.Cliente)
            .SingleOrDefaultAsync(
                x => x.Numero == numeroCuenta && x.Activa,
                cancelacion)
            ?? throw new ExcepcionNegocio("La cuenta indicada no existe o está inactiva.");

        var ahora = DateTime.UtcNow;
        var referencia =
            $"DEPOSITO-{ahora:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..43].ToUpperInvariant();
        cuenta.SaldoDisponible += solicitud.Monto;
        var movimiento = new Movimiento
        {
            Id = Guid.NewGuid(),
            CuentaId = cuenta.Id,
            Cuenta = cuenta,
            ReferenciaCore = referencia,
            Tipo = "CREDITO",
            Monto = solicitud.Monto,
            SaldoResultante = cuenta.SaldoDisponible,
            Descripcion = "Depósito administrativo",
            OcurridoEn = ahora,
            CreadoEn = ahora
        };
        _context.Movimientos.Add(movimiento);

        foreach (var proveedor in _proveedores)
        {
            var destino = proveedor.Canal == "EMAIL"
                ? cuenta.Cliente.Correo
                : cuenta.Cliente.Telefono;
            _context.Notificaciones.Add(new Notificacione
            {
                Id = Guid.NewGuid(),
                ClienteId = cuenta.ClienteId,
                Canal = proveedor.Canal,
                Proveedor = proveedor.Nombre,
                DestinoEnmascarado = proveedor.Enmascarar(destino),
                Estado = "PENDIENTE",
                CreadoEn = ahora
            });
        }

        _context.Eventos.Add(new Evento
        {
            ClienteId = cuenta.ClienteId,
            Accion = "DEPOSITO_CREADO",
            Recurso = "movimientos",
            RecursoId = movimiento.Id.ToString(),
            Resultado = "EXITOSO",
            DireccionIp = HttpContext.Connection.RemoteIpAddress,
            AgenteUsuario = Request.Headers.UserAgent.ToString(),
            CorrelacionId = Guid.NewGuid(),
            Datos = JsonSerializer.Serialize(new
            {
                solicitud.Monto,
                Cuenta = Enmascarar(numeroCuenta)
            }),
            OcurridoEn = ahora
        });

        await _context.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        _servicioClientes.Invalidar(cuenta.ClienteId);
        await _hubNotificaciones.Clients
            .Group(HubNotificaciones.CrearGrupo(cuenta.ClienteId))
            .SendAsync(
                "RecibirNotificacion",
                new NotificacionTiempoReal(
                    "DEPOSITO_RECIBIDO",
                    "Depósito recibido",
                    $"Se acreditó un depósito en tu cuenta terminada en {cuenta.Numero[^4..]}.",
                    solicitud.Monto,
                    cuenta.Moneda.Trim(),
                    ahora),
                cancelacion);

        return StatusCode(
            StatusCodes.Status201Created,
            new DepositoRespuesta(
                movimiento.Id,
                cuenta.Numero,
                movimiento.Monto,
                cuenta.SaldoDisponible,
                referencia,
                ahora));
    }

    private static string Enmascarar(string numero) =>
        numero.Length <= 4 ? numero : new string('*', numero.Length - 4) + numero[^4..];
}
