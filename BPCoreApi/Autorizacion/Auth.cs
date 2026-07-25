using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Autorizacion;

public static class Auth
{
    public static async Task InicializarAsync(
        IServiceProvider servicios,
        IConfiguration configuracion,
        CancellationToken cancelacion = default)
    {
        await using var alcance = servicios.CreateAsyncScope();
        var contextoConfiguracion = alcance.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var contextoGrants = alcance.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

        await contextoConfiguracion.Database.MigrateAsync(cancelacion);
        await contextoGrants.Database.MigrateAsync(cancelacion);

        var origenSpa = configuracion["Autenticacion:Spa:Origen"]
            ?? throw new InvalidOperationException("Falta Autenticacion:Spa:Origen.");
        var clienteId = configuracion["Autenticacion:Spa:ClienteId"]
            ?? throw new InvalidOperationException("Falta Autenticacion:Spa:ClienteId.");
        var origenSpaPublicado = configuracion["Autenticacion:Spa:OrigenPublicado"];
        var urlSpaPublicada = configuracion["Autenticacion:Spa:UrlPublicada"]?.TrimEnd('/');
        var origenMovil = configuracion["Autenticacion:Movil:Origen"]
            ?? throw new InvalidOperationException("Falta Autenticacion:Movil:Origen.");
        var clienteMovilId = configuracion["Autenticacion:Movil:ClienteId"]
            ?? throw new InvalidOperationException("Falta Autenticacion:Movil:ClienteId.");
        var origenMovilAlterno = configuracion["Autenticacion:Movil:OrigenAlterno"]
            ?? throw new InvalidOperationException("Falta Autenticacion:Movil:OrigenAlterno.");

        if (!await contextoConfiguracion.ApiScopes.AnyAsync(cancelacion))
        {
            contextoConfiguracion.ApiScopes.AddRange(
                new ApiScope("banca.consultar", "Consultar productos y movimientos")
                    .ToEntity(),
                new ApiScope("banca.transferir", "Realizar transferencias")
                {
                    UserClaims = { "cliente_id" }
                }.ToEntity());
        }
        if (!await contextoConfiguracion.IdentityResources.AnyAsync(cancelacion))
        {
            contextoConfiguracion.IdentityResources.AddRange(
                new IdentityResources.OpenId().ToEntity(),
                new IdentityResources.Profile().ToEntity(),
                new IdentityResources.Email().ToEntity(),
                new IdentityResource("bp.cliente", "Cliente BP", ["cliente_id"]).ToEntity());
        }
        if (!await contextoConfiguracion.ApiResources.AnyAsync(cancelacion))
        {
            contextoConfiguracion.ApiResources.Add(
                new ApiResource("bp-banca-api", "API Banco BP")
                {
                    Scopes = { "banca.consultar", "banca.transferir" },
                    UserClaims = { "cliente_id" }
                }.ToEntity());
        }
        var clienteSpa = await contextoConfiguracion.Clients
            .Include(x => x.RedirectUris)
            .Include(x => x.PostLogoutRedirectUris)
            .Include(x => x.AllowedCorsOrigins)
            .SingleOrDefaultAsync(x => x.ClientId == clienteId, cancelacion);
        if (clienteSpa is null)
        {
            var configuracionClienteSpa = new Client
            {
                ClientId = clienteId,
                ClientName = "Banco BP Web",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,
                AllowPlainTextPkce = false,
                RedirectUris = { $"{origenSpa}/auth/callback" },
                PostLogoutRedirectUris = { origenSpa },
                AllowedCorsOrigins = { origenSpa },
                AllowedScopes =
                {
                    "openid", "profile", "email", "bp.cliente",
                    "banca.consultar", "banca.transferir"
                },
                AccessTokenLifetime = 900
            };
            if (!string.IsNullOrWhiteSpace(origenSpaPublicado)
                && !string.IsNullOrWhiteSpace(urlSpaPublicada))
            {
                configuracionClienteSpa.RedirectUris.Add(
                    $"{urlSpaPublicada}/auth/callback");
                configuracionClienteSpa.PostLogoutRedirectUris.Add(
                    $"{urlSpaPublicada}/");
                configuracionClienteSpa.AllowedCorsOrigins.Add(origenSpaPublicado);
            }
            contextoConfiguracion.Clients.Add(configuracionClienteSpa.ToEntity());
        }
        else if (!string.IsNullOrWhiteSpace(origenSpaPublicado)
                 && !string.IsNullOrWhiteSpace(urlSpaPublicada))
        {
            var uriRetorno = $"{urlSpaPublicada}/auth/callback";
            var uriCierre = $"{urlSpaPublicada}/";
            if (!clienteSpa.RedirectUris.Any(x => x.RedirectUri == uriRetorno))
            {
                clienteSpa.RedirectUris.Add(new()
                {
                    RedirectUri = uriRetorno
                });
            }
            if (!clienteSpa.PostLogoutRedirectUris.Any(
                    x => x.PostLogoutRedirectUri == uriCierre))
            {
                clienteSpa.PostLogoutRedirectUris.Add(new()
                {
                    PostLogoutRedirectUri = uriCierre
                });
            }
            if (!clienteSpa.AllowedCorsOrigins.Any(x => x.Origin == origenSpaPublicado))
            {
                clienteSpa.AllowedCorsOrigins.Add(new()
                {
                    Origin = origenSpaPublicado
                });
            }
        }
        if (!await contextoConfiguracion.Clients.AnyAsync(x => x.ClientId == clienteMovilId, cancelacion))
        {
            contextoConfiguracion.Clients.Add(new Client
            {
                ClientId = clienteMovilId,
                ClientName = "Banco BP Móvil",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,
                AllowPlainTextPkce = false,
                RedirectUris =
                {
                    $"{origenMovil}/auth/callback",
                    $"{origenMovilAlterno}/auth/callback",
                    "http://localhost/auth/callback"
                },
                PostLogoutRedirectUris = { origenMovil, origenMovilAlterno, "http://localhost" },
                AllowedCorsOrigins = { origenMovil, origenMovilAlterno, "http://localhost" },
                AllowedScopes =
                {
                    "openid", "profile", "email", "bp.cliente",
                    "banca.consultar", "banca.transferir"
                },
                AccessTokenLifetime = 900
            }.ToEntity());
        }
        await contextoConfiguracion.SaveChangesAsync(cancelacion);
    }
}
