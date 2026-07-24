using System.Net;
using System.Text.Json;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;

namespace BPCoreApi.Servicios;

public sealed class ServicioAuditoria(ContextoBanco context) : IServicioAuditoria
{
    private readonly ContextoBanco _context = context;

    public async Task RegistrarAsync(HttpContext contexto, CancellationToken cancelacion)
    {
        _context.ChangeTracker.Clear();

        Guid? clienteId = Guid.TryParse(
            contexto.User.FindFirst("cliente_id")?.Value, out var cliente) ? cliente : null;
        Guid? usuarioId = Guid.TryParse(
            contexto.User.FindFirst("usuario_id")?.Value, out var usuario) ? usuario : null;

        _context.Eventos.Add(new Evento
        {
            UsuarioId = usuarioId,
            ClienteId = clienteId,
            Accion = Limitar(
                $"{contexto.Request.Method} {contexto.Request.Path}",
                100),
            Recurso = Limitar(
                contexto.GetEndpoint()?.DisplayName ?? "api",
                120),
            Resultado = contexto.Response.StatusCode < 400 ? "EXITOSO" : "FALLIDO",
            DireccionIp = contexto.Connection.RemoteIpAddress ?? IPAddress.None,
            AgenteUsuario = Limitar(
                contexto.Request.Headers.UserAgent.ToString(),
                500),
            CorrelacionId = contexto.Items.TryGetValue("correlacion_id", out var id) && id is Guid valor
                ? valor
                : Guid.NewGuid(),
            Datos = JsonSerializer.Serialize(new { estadoHttp = contexto.Response.StatusCode }),
            OcurridoEn = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancelacion);
    }

    private static string Limitar(string valor, int longitudMaxima) =>
        valor.Length <= longitudMaxima ? valor : valor[..longitudMaxima];
}
