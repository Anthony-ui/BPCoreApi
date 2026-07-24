namespace BPCoreApi.Servicios;

public interface IProcesadorCoreBancario
{
    Task<string> TransferirAsync(
        string cuentaOrigen,
        string cuentaDestino,
        decimal monto,
        string moneda,
        CancellationToken cancelacion);
}

public sealed class ProcesadorCoreBancarioSimulado : IProcesadorCoreBancario
{
    public Task<string> TransferirAsync(
        string cuentaOrigen,
        string cuentaDestino,
        decimal monto,
        string moneda,
        CancellationToken cancelacion) =>
        Task.FromResult($"CORE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..35]);
}
