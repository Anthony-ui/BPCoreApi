using BPCoreApi.Contratos;
using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes/{clienteId:guid}/transferencias/validar-cuenta")]
[Authorize(Policy = "transferir")]
public sealed class ValidacionCuentasController(
    IServicioValidacionCuentas servicioValidacion) : ControladorSeguro
{
    private readonly IServicioValidacionCuentas _servicioValidacion = servicioValidacion;

    [HttpPost]
    public async Task<ActionResult<ValidacionCuentaRespuesta>> Validar(
        Guid clienteId,
        [FromBody] ValidacionCuentaSolicitud solicitud,
        CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }

        return Ok(await _servicioValidacion.ValidarAsync(clienteId, solicitud, cancelacion));
    }
}
