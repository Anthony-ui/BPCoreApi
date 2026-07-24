using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using BPCoreApi.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Controllers;

[ApiController]
[Route("api/clientes/{clienteId:guid}/onboarding/facial")]
[Authorize(Policy = "consultar")]
public sealed class OnboardingController(ContextoBanco context) : ControladorSeguro
{
    private readonly ContextoBanco _context = context;

    [HttpGet]
    public async Task<ActionResult<VerificacionFacialRespuesta?>> Obtener(
        Guid clienteId,
        CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }

        var onboarding = await _context.Onboardings.AsNoTracking()
            .Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.IniciadoEn)
            .FirstOrDefaultAsync(cancelacion);

        return onboarding is null ? NoContent() : Ok(Mapear(onboarding));
    }

    [HttpPost]
    public async Task<ActionResult<VerificacionFacialRespuesta>> Verificar(
        Guid clienteId,
        VerificacionFacialSolicitud solicitud,
        CancellationToken cancelacion)
    {
        if (!EsClienteAutorizado(clienteId))
        {
            return Forbid();
        }

        var ahora = DateTime.UtcNow;
        var aprobada = solicitud.PruebaVidaSuperada;
        var onboarding = new Onboarding
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Estado = aprobada ? "APROBADO" : "RECHAZADO",
            ProveedorBiometrico = "SIMULADOR_WEB_BP",
            ReferenciaVerificacion = $"FAC-{Guid.NewGuid():N}",
            PuntajeRostro = aprobada ? 0.97m : 0.35m,
            PruebaVidaSuperada = aprobada,
            IniciadoEn = ahora,
            FinalizadoEn = ahora
        };

        _context.Onboardings.Add(onboarding);
        _context.Eventos.Add(new Evento
        {
            ClienteId = clienteId,
            Accion = "VERIFICACION_FACIAL",
            Recurso = "onboarding",
            RecursoId = onboarding.Id.ToString(),
            Resultado = aprobada ? "EXITOSO" : "RECHAZADO",
            CorrelacionId = Guid.NewGuid(),
            OcurridoEn = ahora
        });
        await _context.SaveChangesAsync(cancelacion);

        return Ok(Mapear(onboarding));
    }

    private static VerificacionFacialRespuesta Mapear(Onboarding onboarding) => new(
        onboarding.Id,
        onboarding.Estado,
        onboarding.ProveedorBiometrico,
        onboarding.PuntajeRostro,
        onboarding.PruebaVidaSuperada,
        onboarding.FinalizadoEn);
}
