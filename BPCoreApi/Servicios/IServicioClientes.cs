using BPCoreApi.Contratos;

namespace BPCoreApi.Servicios;

public interface IServicioClientes
{
    Task<ClienteRespuesta?> ObtenerAsync(Guid clienteId, CancellationToken cancelacion);
    Task<RespuestaPaginada<MovimientoRespuesta>?> ObtenerMovimientosAsync(
        Guid clienteId,
        Guid cuentaId,
        int pagina,
        int tamanoPagina,
        string? tipo,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancelacion);
    void Invalidar(Guid clienteId);
}
