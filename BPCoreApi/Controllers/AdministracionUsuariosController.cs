using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/administracion/usuarios")]
[AllowAnonymous]
public sealed class AdministracionUsuariosController(
    ContextoBanco context,
    IPasswordHasher<Usuario> generadorHash) : ControllerBase
{
    private readonly ContextoBanco _context = context;
    private readonly IPasswordHasher<Usuario> _generadorHash = generadorHash;

    [HttpPost]
    [ProducesResponseType<UsuarioCreadoRespuesta>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioCreadoRespuesta>> Crear([FromBody] SolicitudCreacionUsuario solicitud,
                                                                  CancellationToken cancelacion)
    {
        var duplicado = await _context.Clientes.AnyAsync(
                x => x.Identificacion == solicitud.Identificacion
                    || x.Correo == solicitud.Correo,
                cancelacion)
            || await _context.Usuarios.AnyAsync(
                x => x.NombreUsuario == solicitud.NombreUsuario,
                cancelacion)
            || await _context.Cuentas.AnyAsync(
                x => x.Numero == solicitud.NumeroCuenta,
                cancelacion);
        if (duplicado)
        {
            return Conflict(new ProblemDetails
            {
                Title = "El usuario ya existe",
                Detail = "La identificación, correo, nombre de usuario o cuenta ya está registrada.",
                Status = StatusCodes.Status409Conflict
            });
        }

        await using var transaccion =
            await _context.Database.BeginTransactionAsync(cancelacion);
        var ahora = DateTime.UtcNow;
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Identificacion = solicitud.Identificacion.Trim(),
            Nombres = solicitud.Nombres.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            Correo = solicitud.Correo.Trim().ToLowerInvariant(),
            Telefono = solicitud.Telefono.Trim(),
            FechaNacimiento = solicitud.FechaNacimiento,
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            ClienteId = cliente.Id,
            Cliente = cliente,
            SujetoExterno = Guid.NewGuid().ToString(),
            NombreUsuario = solicitud.NombreUsuario.Trim(),
            Activo = true,
            CreadoEn = ahora,
            SelloSeguridad = Guid.NewGuid().ToString(),
            IntentosFallidos = 0,
            UltimoCambioClaveEn = ahora
        };
        usuario.HashClave = _generadorHash.HashPassword(usuario, solicitud.Clave);
        var cuenta = new Cuenta
        {
            Id = Guid.NewGuid(),
            ClienteId = cliente.Id,
            Cliente = cliente,
            Numero = solicitud.NumeroCuenta.Trim(),
            Tipo = solicitud.TipoCuenta,
            Moneda = "USD",
            SaldoDisponible = solicitud.SaldoInicial,
            Activa = true,
            CreadoEn = ahora
        };

        _context.Clientes.Add(cliente);
        _context.Usuarios.Add(usuario);
        _context.Cuentas.Add(cuenta);
        await _context.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);

        var respuesta = new UsuarioCreadoRespuesta(
            cliente.Id,
            usuario.Id,
            cuenta.Id,
            usuario.NombreUsuario,
            cuenta.Numero);
        return StatusCode(StatusCodes.Status201Created, respuesta);
    }
}
