using BPCoreApi.Contratos;
using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = "consultar")]
public sealed class ClientesController(IServicioClientes servicioClientes) : ControladorSeguro
{
    private readonly IServicioClientes _servicioClientes = servicioClientes;

    [HttpGet("{clienteId:guid}")]
    [ProducesResponseType<ClienteRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteRespuesta>> Obtener(
        Guid clienteId, CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }
        var cliente = await _servicioClientes.ObtenerAsync(clienteId, cancelacion);
        return cliente is null ? NotFound() : Ok(cliente);
    }
}
