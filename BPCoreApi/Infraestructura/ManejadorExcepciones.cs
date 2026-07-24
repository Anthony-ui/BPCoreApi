using BPCoreApi.Servicios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Infraestructura;

public sealed class ManejadorExcepciones(
    IProblemDetailsService problemas,
    ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        var (estado, titulo) = excepcion switch
        {
            ExcepcionNegocio => (StatusCodes.Status422UnprocessableEntity, excepcion.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "No se pudo completar la operación."),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };
        logger.LogError(excepcion, "Solicitud finalizada con estado {Estado}", estado);
        contexto.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails { Status = estado, Title = titulo }
        });
    }
}
