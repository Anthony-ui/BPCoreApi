using BPCoreApi.Contratos;
using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes/{clienteId:guid}/cuentas/{cuentaId:guid}/movimientos")]
[Authorize(Policy = "consultar")]
public sealed class MovimientosController(IServicioClientes servicioClientes) : ControladorSeguro
{
    private readonly IServicioClientes _servicioClientes = servicioClientes;

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<MovimientoRespuesta>>> Obtener(
        Guid clienteId,
        Guid cuentaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? tipo = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        CancellationToken cancelacion = default)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        if (tipo is not null && tipo is not ("CREDITO" or "DEBITO"))
        {
            return BadRequest("El tipo debe ser CREDITO o DEBITO.");
        }
        var movimientos = await _servicioClientes.ObtenerMovimientosAsync(
            clienteId, cuentaId, pagina, tamanoPagina, tipo, desde, hasta, cancelacion);
        return movimientos is null ? NotFound() : Ok(movimientos);
    }
}
