using BPCoreApi.Contratos;
using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes/{clienteId:guid}/transferencias")]
[Authorize(Policy = "transferir")]
public sealed class TransferenciasController(IServicioTransferencias servicioTransferencias) : ControladorSeguro
{
    private readonly IServicioTransferencias _servicioTransferencias = servicioTransferencias;

    [HttpPost]
    [ProducesResponseType<TransferenciaRespuesta>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferenciaRespuesta>> Crear(
        Guid clienteId,
        [FromBody] TransferenciaSolicitud solicitud,
        CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }
        var transferencia = await _servicioTransferencias.TransferirAsync(
            clienteId, solicitud, cancelacion);
        return Created($"/api/clientes/{clienteId}/transferencias/{transferencia.Id}", transferencia);
    }
}
