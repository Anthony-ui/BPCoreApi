using BPCoreApi.Contratos;
using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes/{clienteId:guid}/pagos")]
[Authorize(Policy = "transferir")]
public sealed class PagosController(IServicioTransferencias servicioTransferencias) : ControladorSeguro
{
    private readonly IServicioTransferencias _servicioTransferencias = servicioTransferencias;

    [HttpPost]
    [ProducesResponseType<TransferenciaRespuesta>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferenciaRespuesta>> Crear(
        Guid clienteId,
        [FromBody] PagoSolicitud solicitud,
        CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }

        var resultado = await _servicioTransferencias.TransferirAsync(
            clienteId,
            new TransferenciaSolicitud
            {
                CuentaOrigenId = solicitud.CuentaOrigenId,
                CuentaDestino = solicitud.Referencia,
                InstitucionDestino = solicitud.Servicio,
                Monto = solicitud.Monto,
                Moneda = "USD",
                Concepto = $"Pago {solicitud.Servicio}",
                ClaveIdempotencia = solicitud.ClaveIdempotencia,
                DestinoValidadoInternamente = true
            },
            cancelacion);

        return CreatedAtAction(nameof(Crear), new { clienteId, id = resultado.Id }, resultado);
    }
}
