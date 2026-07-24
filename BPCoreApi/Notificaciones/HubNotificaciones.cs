using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BPCoreApi.TiempoReal;

[Authorize]
public sealed class HubNotificaciones : Hub
{
    public override async Task OnConnectedAsync()
    {
        var clienteId = Context.User?.FindFirst("cliente_id")?.Value;
        if (!string.IsNullOrWhiteSpace(clienteId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CrearGrupo(clienteId));
        }
        await base.OnConnectedAsync();
    }

    public static string CrearGrupo(Guid clienteId) => CrearGrupo(clienteId.ToString());

    private static string CrearGrupo(string clienteId) => $"cliente:{clienteId}";
}
