using BPCoreApi.Servicios;

namespace BPCoreApi.Infraestructura;

public sealed class MiddlewareAuditoria(RequestDelegate siguiente, ILogger<MiddlewareAuditoria> logger)
{
    public async Task InvokeAsync(HttpContext contexto, IServicioAuditoria auditoria)
    {
        await siguiente(contexto);
        if (!contexto.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        try
        {
            await auditoria.RegistrarAsync(contexto, contexto.RequestAborted);
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, "No fue posible registrar el evento de auditoría.");
        }
    }
}
