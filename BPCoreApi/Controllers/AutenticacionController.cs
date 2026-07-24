using System.Text.Encodings.Web;
using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/autenticacion")]
public sealed class AutenticacionController(
    ContextoBanco context,
    IPasswordHasher<Usuario> generadorHash,
    IIdentityServerInteractionService interaccion) : ControllerBase
{
    private readonly ContextoBanco _context = context;
    private readonly IPasswordHasher<Usuario> _generadorHash = generadorHash;
    private readonly IIdentityServerInteractionService _interaccion = interaccion;

    [HttpGet("iniciar")]
    public IActionResult Iniciar([FromQuery(Name = "returnUrl")] string retorno) =>
        Content(ConstruirPagina(retorno), "text/html; charset=utf-8");

    [HttpPost("iniciar")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Iniciar(
        [FromForm] SolicitudInicioSesion solicitud,
        CancellationToken cancelacion)
    {
        if (!_interaccion.IsValidReturnUrl(solicitud.Retorno))
        {
            return BadRequest("La URL de retorno no es válida.");
        }

        var usuario = await _context.Usuarios
            .Include(x => x.Cliente)
            .SingleOrDefaultAsync(
                x => x.NombreUsuario == solicitud.Usuario && x.Activo,
                cancelacion);
        var bloqueado = usuario?.BloqueadoHasta > DateTime.UtcNow;
        var resultado = usuario?.HashClave is not null
            ? _generadorHash.VerifyHashedPassword(usuario, usuario.HashClave, solicitud.Clave)
            : PasswordVerificationResult.Failed;
        if (usuario is null || bloqueado || resultado == PasswordVerificationResult.Failed)
        {
            if (usuario is not null && !bloqueado)
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= 5)
                {
                    usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync(cancelacion);
            }
            return Content(
                ConstruirPagina(solicitud.Retorno, "Usuario o clave incorrectos."),
                "text/html; charset=utf-8");
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAccesoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancelacion);
        await HttpContext.SignInAsync(new IdentityServerUser(
            usuario.SujetoExterno ?? usuario.Id.ToString())
        {
            DisplayName = $"{usuario.Cliente.Nombres} {usuario.Cliente.Apellidos}",
            AdditionalClaims =
            [
                new("name", $"{usuario.Cliente.Nombres} {usuario.Cliente.Apellidos}"),
                new("email", usuario.Cliente.Correo),
                new("cliente_id", usuario.ClienteId.ToString())
            ]
        });
        return Redirect(solicitud.Retorno);
    }

    [HttpGet("cerrar")]
    public async Task<IActionResult> Cerrar(
        [FromQuery] string? logoutId,
        CancellationToken cancelacion)
    {
        await HttpContext.SignOutAsync(
            IdentityServerConstants.DefaultCookieAuthenticationScheme);
        var contexto = logoutId is null
            ? null
            : await _interaccion.GetLogoutContextAsync(logoutId, cancelacion);
        return Redirect(contexto?.PostLogoutRedirectUri ?? "http://localhost:4200");
    }

    private static string ConstruirPagina(string retorno, string? error = null)
    {
        var retornoSeguro = HtmlEncoder.Default.Encode(retorno);
        var errorHtml = error is null
            ? string.Empty
            : $"<div class=\"error\">{HtmlEncoder.Default.Encode(error)}</div>";
        return $$"""
        <!doctype html><html lang="es"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>Banco BP | Acceso seguro</title>
        <style>
        *{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:24px;background:#062f34;font-family:Segoe UI,Arial;color:#17383b}
        .panel{width:min(410px,100%);padding:34px;border-radius:16px;background:#fff;box-shadow:0 30px 80px #00191c88}
        .marca{width:50px;height:50px;display:grid;place-items:center;margin-bottom:28px;border:1px solid #c99d49;border-radius:14px 5px;color:#936a22;font:700 20px Georgia}
        small{color:#af7c26;font-weight:800;letter-spacing:.16em}h1{margin:10px 0 8px;font:500 32px Georgia;color:#10363a}p{margin:0 0 24px;color:#728485;font-size:13px}
        label{display:grid;gap:7px;margin-top:15px;font-size:11px;font-weight:700}input{height:45px;padding:0 12px;border:1px solid #ced9d8;border-radius:9px;font:inherit}
        input:focus{outline:3px solid #d5ab5940;border-color:#a97a2a}button{width:100%;height:47px;margin-top:23px;border:0;border-radius:9px;background:#0b4248;color:#fff;font-weight:750;cursor:pointer}
        .error{margin:17px 0 4px;padding:11px;border:1px solid #edcec8;border-radius:8px;background:#fff4f2;color:#944f42;font-size:11px}
        .seguro{margin-top:18px;color:#7d8d8e;font-size:10px;text-align:center}.seguro b{color:#2c7768}
        </style></head><body><main class="panel"><div class="marca">BP</div>
        <small>ACCESO SEGURO</small><h1>Bienvenido a BP</h1>
        <p>Ingresa tus credenciales para continuar.</p>{{errorHtml}}
        <form method="post" action="/api/autenticacion/iniciar">
        <input type="hidden" name="Retorno" value="{{retornoSeguro}}">
        <label>Usuario<input name="Usuario" autocomplete="username" required autofocus></label>
        <label>Clave<input name="Clave" type="password" autocomplete="current-password" required></label>
        <button type="submit">Ingresar de forma segura</button></form>
        <div class="seguro"><b>✓</b> Sesión protegida por OpenID Connect + PKCE</div>
        </main></body></html>
        """;
    }
}
