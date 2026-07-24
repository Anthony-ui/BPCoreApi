namespace BPCoreApi.Contratos;

public sealed class VerificacionFacialSolicitud
{
    public bool PruebaVidaSuperada { get; init; }
}

public sealed record VerificacionFacialRespuesta(
    Guid Id,
    string Estado,
    string ProveedorBiometrico,
    decimal? PuntajeRostro,
    bool? PruebaVidaSuperada,
    DateTime? FinalizadoEn);
