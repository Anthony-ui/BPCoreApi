using BPCoreApi.Contratos;

namespace BPCoreApi.Servicios;

public interface IServicioTransferencias
{
    Task<TransferenciaRespuesta> TransferirAsync(
        Guid clienteId, TransferenciaSolicitud solicitud, CancellationToken cancelacion);
}
