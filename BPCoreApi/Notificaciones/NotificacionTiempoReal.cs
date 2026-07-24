namespace BPCoreApi.TiempoReal;

public sealed record NotificacionTiempoReal(
    string Tipo,
    string Titulo,
    string Mensaje,
    decimal Monto,
    string Moneda,
    DateTime OcurridoEn);
