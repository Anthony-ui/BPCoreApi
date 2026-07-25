namespace BPCoreApi.Contratos;

public sealed record ClienteRespuesta(
    Guid Id,
    string Identificacion,
    string NombreCompleto,
    string Correo,
    string Telefono,
    DateTime? UltimoAccesoEn,
    IReadOnlyCollection<CuentaRespuesta> Cuentas);

public sealed record CuentaRespuesta(
    Guid Id,
    string NumeroEnmascarado,
    string Tipo,
    string Moneda,
    decimal SaldoDisponible);

public sealed record MovimientoRespuesta(
    Guid Id,
    string Tipo,
    decimal Monto,
    decimal? SaldoResultante,
    string Descripcion,
    DateTime OcurridoEn,
    string? Contraparte,
    string? CuentaContraparteEnmascarada);

public sealed record RespuestaPaginada<T>(
    IReadOnlyCollection<T> Elementos,
    int Pagina,
    int TamanoPagina,
    int Total);

public sealed record TransferenciaRespuesta(
    Guid Id,
    string Estado,
    string ReferenciaCore,
    decimal Monto,
    string Moneda,
    DateTime ProcesadoEn);

public sealed record ValidacionCuentaRespuesta(
    bool Valida,
    string Titular,
    string CuentaEnmascarada,
    Guid Comprobante,
    DateTime ExpiraEn);
