# Configuración mínima de Duende IdentityServer

Registrar el siguiente cliente público SPA en el producto Duende de la compañía:

```csharp
new Client
{
    ClientId = "bp-spa",
    ClientName = "Banco BP Web",
    AllowedGrantTypes = GrantTypes.Code,
    RequireClientSecret = false,
    RequirePkce = true,
    AllowPlainTextPkce = false,
    RedirectUris = { "http://localhost:4200/auth/callback" },
    PostLogoutRedirectUris = { "http://localhost:4200" },
    AllowedCorsOrigins = { "http://localhost:4200" },
    AllowedScopes =
    {
        IdentityServerConstants.StandardScopes.OpenId,
        IdentityServerConstants.StandardScopes.Profile,
        IdentityServerConstants.StandardScopes.Email,
        "banca.consultar",
        "banca.transferir"
    }
};
```

El perfil del usuario debe emitir el claim `cliente_id` con el UUID del cliente
asociado. Para el usuario de demostración de la base local:

```text
cliente_id = 10000000-0000-0000-0000-000000000001
```

La API valida:

- Emisor: `https://localhost:5001`.
- Audiencia: `bp-banca-api`.
- Scopes de consulta y transferencia.
- Que el `cliente_id` del token coincida con el cliente solicitado en la URL.

No se configura `client_secret` en Angular porque una SPA no puede proteger
secretos. Para producción bancaria se recomienda evolucionar a Duende BFF y
mantener los tokens fuera del navegador.
