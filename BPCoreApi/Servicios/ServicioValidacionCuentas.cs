using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BPCoreApi.Servicios;

public interface IServicioValidacionCuentas
{
    Task<ValidacionCuentaRespuesta> ValidarAsync(
        Guid clienteId,
        ValidacionCuentaSolicitud solicitud,
        CancellationToken cancelacion);

    bool Consumir(
        Guid clienteId,
        string cuentaDestino,
        string institucionDestino,
        Guid? comprobante);
}

public sealed class ServicioValidacionCuentas(
    ContextoBanco context,
    IMemoryCache cache) : IServicioValidacionCuentas
{
    private readonly ContextoBanco _context = context;
    private readonly IMemoryCache _cache = cache;

    public async Task<ValidacionCuentaRespuesta> ValidarAsync(
        Guid clienteId,
        ValidacionCuentaSolicitud solicitud,
        CancellationToken cancelacion)
    {
        var cuentaDestino = solicitud.CuentaDestino.Trim();
        var institucion = solicitud.InstitucionDestino.Trim();
        var esBancoBp = institucion.Equals("BP", StringComparison.OrdinalIgnoreCase)
            || institucion.Contains("Banco BP", StringComparison.OrdinalIgnoreCase);

        string titular;
        if (esBancoBp)
        {
            var cuenta = await _context.Cuentas
                .AsNoTracking()
                .Where(x => x.Numero == cuentaDestino && x.Activa)
                .Select(x => new { x.Cliente.Nombres, x.Cliente.Apellidos })
                .SingleOrDefaultAsync(cancelacion)
                ?? throw new ExcepcionNegocio("La cuenta destino de Banco BP no existe o está inactiva.");
            titular = $"{cuenta.Nombres} {cuenta.Apellidos}";
        }
        else
        {
            if (!cuentaDestino.All(char.IsDigit))
            {
                throw new ExcepcionNegocio("La cuenta destino debe contener únicamente números.");
            }
            titular = $"Cuenta verificada en {institucion}";
        }

        var comprobante = Guid.NewGuid();
        var expiraEn = DateTime.UtcNow.AddMinutes(5);
        _cache.Set(
            CrearClave(comprobante),
            new ValidacionTemporal(clienteId, cuentaDestino, institucion),
            expiraEn);

        var enmascarada = cuentaDestino.Length <= 4
            ? cuentaDestino
            : new string('*', cuentaDestino.Length - 4) + cuentaDestino[^4..];
        return new ValidacionCuentaRespuesta(true, titular, enmascarada, comprobante, expiraEn);
    }

    public bool Consumir(
        Guid clienteId,
        string cuentaDestino,
        string institucionDestino,
        Guid? comprobante)
    {
        if (!comprobante.HasValue
            || !_cache.TryGetValue(CrearClave(comprobante.Value), out ValidacionTemporal? validacion)
            || validacion is null)
        {
            return false;
        }

        var coincide = validacion.ClienteId == clienteId
            && validacion.CuentaDestino == cuentaDestino.Trim()
            && validacion.InstitucionDestino.Equals(
                institucionDestino.Trim(),
                StringComparison.OrdinalIgnoreCase);
        if (coincide)
        {
            _cache.Remove(CrearClave(comprobante.Value));
        }
        return coincide;
    }

    private static string CrearClave(Guid comprobante) => $"validacion-cuenta:{comprobante}";

    private sealed record ValidacionTemporal(
        Guid ClienteId,
        string CuentaDestino,
        string InstitucionDestino);
}
