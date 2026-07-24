namespace BPCoreApi.Servicios;

public interface IServicioAuditoria
{
    Task RegistrarAsync(HttpContext contexto, CancellationToken cancelacion);
}
