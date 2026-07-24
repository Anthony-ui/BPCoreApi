namespace BPCoreApi.Infraestructura;

public sealed class MiddlewareCorrelacion(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        var correlacion = Guid.TryParse(
            contexto.Request.Headers["X-Correlation-ID"], out var recibida)
            ? recibida
            : Guid.NewGuid();
        contexto.Items["correlacion_id"] = correlacion;
        contexto.Response.Headers["X-Correlation-ID"] = correlacion.ToString();
        await siguiente(contexto);
    }
}
