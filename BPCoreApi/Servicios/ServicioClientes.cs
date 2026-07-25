using BPCoreApi.Contratos;
using BPCoreApi.Datos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BPCoreApi.Servicios;

public sealed class ServicioClientes(
    ContextoBanco context,
    IMemoryCache cache) : IServicioClientes
{
    private readonly ContextoBanco _context = context;
    private readonly IMemoryCache _cache = cache;

    public async Task<ClienteRespuesta?> ObtenerAsync(Guid clienteId, CancellationToken cancelacion)
    {
        var clave = $"cliente:{clienteId}";
        if (_cache.TryGetValue(clave, out ClienteRespuesta? cliente))
        {
            var ultimoAccesoEn = await _context.Usuarios
                .AsNoTracking()
                .Where(x => x.ClienteId == clienteId)
                .Select(x => x.UltimoAccesoEn)
                .SingleOrDefaultAsync(cancelacion);
            return cliente! with { UltimoAccesoEn = ultimoAccesoEn };
        }

        cliente = await _context.Clientes
            .AsNoTracking()
            .Where(x => x.Id == clienteId && x.Activo)
            .Select(x => new ClienteRespuesta(
                x.Id,
                x.Identificacion,
                $"{x.Nombres} {x.Apellidos}",
                x.Correo,
                x.Telefono,
                x.Usuario != null ? x.Usuario.UltimoAccesoEn : null,
                x.Cuenta.Where(c => c.Activa).Select(c => new CuentaRespuesta(
                    c.Id,
                    "****" + c.Numero.Substring(c.Numero.Length - 4),
                    c.Tipo,
                    c.Moneda.Trim(),
                    c.SaldoDisponible)).ToArray()))
            .SingleOrDefaultAsync(cancelacion);

        if (cliente is not null)
        {
            _cache.Set(clave, cliente, TimeSpan.FromMinutes(2));
        }

        return cliente;
    }

    public async Task<RespuestaPaginada<MovimientoRespuesta>?> ObtenerMovimientosAsync(
        Guid clienteId,
        Guid cuentaId,
        int pagina,
        int tamanoPagina,
        string? tipo,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancelacion)
    {
        var cuentaExiste = await _context.Cuentas
            .AnyAsync(x => x.Id == cuentaId && x.ClienteId == clienteId && x.Activa, cancelacion);
        if (!cuentaExiste)
        {
            return null;
        }

        var consulta = _context.Movimientos.AsNoTracking().Where(x => x.CuentaId == cuentaId);
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoNormalizado = tipo.Trim().ToUpperInvariant();
            consulta = consulta.Where(x => x.Tipo == tipoNormalizado);
        }
        if (desde.HasValue)
        {
            consulta = consulta.Where(x => x.OcurridoEn >= desde.Value);
        }
        if (hasta.HasValue)
        {
            consulta = consulta.Where(x => x.OcurridoEn < hasta.Value);
        }
        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .OrderByDescending(x => x.OcurridoEn)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new MovimientoRespuesta(
                x.Id,
                x.Tipo,
                x.Monto,
                x.SaldoResultante,
                x.Descripcion,
                x.OcurridoEn,
                x.Transferencia == null
                    ? null
                    : x.Tipo == "CREDITO"
                        ? x.Transferencia.CuentaOrigen.Cliente.Nombres + " "
                            + x.Transferencia.CuentaOrigen.Cliente.Apellidos
                        : x.Transferencia.Beneficiario != null
                            ? x.Transferencia.Beneficiario.NombreBeneficiario
                            : _context.Cuentas
                                .Where(c => c.Numero == x.Transferencia.CuentaDestino)
                                .Select(c => c.Cliente.Nombres + " " + c.Cliente.Apellidos)
                                .FirstOrDefault() ?? x.Transferencia.InstitucionDestino,
                x.Transferencia == null
                    ? null
                    : x.Tipo == "CREDITO"
                        ? "****" + x.Transferencia.CuentaOrigen.Numero.Substring(
                            x.Transferencia.CuentaOrigen.Numero.Length - 4)
                        : "****" + x.Transferencia.CuentaDestino.Substring(
                            x.Transferencia.CuentaDestino.Length - 4)))
            .ToArrayAsync(cancelacion);

        return new RespuestaPaginada<MovimientoRespuesta>(elementos, pagina, tamanoPagina, total);
    }

    public void Invalidar(Guid clienteId) => _cache.Remove($"cliente:{clienteId}");
}
