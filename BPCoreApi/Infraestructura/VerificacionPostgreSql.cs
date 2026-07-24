using BPCoreApi.Datos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BPCoreApi.Infraestructura;

public sealed class VerificacionPostgreSql(ContextoBanco context) : IHealthCheck
{
    private readonly ContextoBanco _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext contexto, CancellationToken cancelacion = default) =>
        await _context.Database.CanConnectAsync(cancelacion)
            ? HealthCheckResult.Healthy("PostgreSQL disponible.")
            : HealthCheckResult.Unhealthy("PostgreSQL no disponible.");
}
