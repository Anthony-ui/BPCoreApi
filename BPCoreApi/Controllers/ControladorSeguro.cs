using Microsoft.AspNetCore.Mvc;

namespace BPCoreApi.Controllers;

public abstract class ControladorSeguro : ControllerBase
{
    protected bool EsClienteAutorizado(Guid clienteId)
    {
        var claim = User.FindFirst("cliente_id")?.Value;
        return Guid.TryParse(claim, out var clienteAutenticado) && clienteAutenticado == clienteId;
    }
}
