namespace BPCoreApi.Servicios;

public interface IProveedorNotificaciones
{
    string Canal { get; }
    string Nombre { get; }
    string Enmascarar(string destino);
}

public sealed class ProveedorCorreoSimulado : IProveedorNotificaciones
{
    public string Canal => "EMAIL";
    public string Nombre => "SendGrid";
    public string Enmascarar(string destino)
    {
        var partes = destino.Split('@');
        return partes.Length == 2 ? $"{partes[0][0]}***@{partes[1]}" : "***";
    }
}

public sealed class ProveedorSmsSimulado : IProveedorNotificaciones
{
    public string Canal => "SMS";
    public string Nombre => "Twilio";
    public string Enmascarar(string destino) =>
        destino.Length > 4 ? $"***{destino[^4..]}" : "***";
}
