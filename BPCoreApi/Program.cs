using System.Threading.RateLimiting;
using BPCoreApi.Datos;
using BPCoreApi.Autorizacion;
using BPCoreApi.Infraestructura;
using BPCoreApi.Modelos;
using BPCoreApi.Servicios;
using BPCoreApi.TiempoReal;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var cadenaConexion = ObtenerCadenaConexion(builder.Configuration);

builder.Services.AddDbContext<ContextoBanco>(opciones =>
    opciones.UseNpgsql(cadenaConexion));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var ensambladoMigraciones = typeof(Program).Assembly.GetName().Name!;
builder.Services.AddIdentityServer(opciones =>
    {
        opciones.UserInteraction.LoginUrl = "/api/autenticacion/iniciar";
        opciones.UserInteraction.LogoutUrl = "/api/autenticacion/cerrar";
        opciones.Authentication.CookieAuthenticationScheme =
            IdentityServerConstants.DefaultCookieAuthenticationScheme;
        opciones.Events.RaiseFailureEvents = true;
        opciones.Events.RaiseSuccessEvents = true;
    })
    .AddConfigurationStore(opciones =>
    {
        opciones.DefaultSchema = "oidc";
        opciones.ConfigureDbContext = constructor =>
            constructor.UseNpgsql(cadenaConexion, postgres =>
            {
                postgres.MigrationsAssembly(ensambladoMigraciones);
                postgres.MigrationsHistoryTable("__migraciones_configuracion", "oidc");
            });
    })
    .AddOperationalStore(opciones =>
    {
        opciones.DefaultSchema = "oidc";
        opciones.ConfigureDbContext = constructor =>
            constructor.UseNpgsql(cadenaConexion, postgres =>
            {
                postgres.MigrationsAssembly(ensambladoMigraciones);
                postgres.MigrationsHistoryTable("__migraciones_operacion", "oidc");
            });
    });
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IServicioClientes, ServicioClientes>();
builder.Services.AddScoped<IServicioTransferencias, ServicioTransferencias>();
builder.Services.AddScoped<IServicioValidacionCuentas, ServicioValidacionCuentas>();
builder.Services.AddScoped<IServicioAuditoria, ServicioAuditoria>();
builder.Services.AddScoped<IProcesadorCoreBancario, ProcesadorCoreBancarioSimulado>();
builder.Services.AddScoped<IProveedorNotificaciones, ProveedorCorreoSimulado>();
builder.Services.AddScoped<IProveedorNotificaciones, ProveedorSmsSimulado>();
builder.Services.AddHealthChecks().AddCheck<VerificacionPostgreSql>("postgresql");

builder.Services
    .AddAuthentication(opciones =>
    {
        opciones.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opciones.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opciones =>
    {
        opciones.Authority = builder.Configuration["Autenticacion:Autoridad"];
        opciones.Audience = builder.Configuration["Autenticacion:Audiencia"];
        opciones.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opciones.TokenValidationParameters.ValidateIssuer = true;
        opciones.TokenValidationParameters.ValidateAudience = true;
        opciones.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(token)
                    && contexto.HttpContext.Request.Path.StartsWithSegments(
                        "/hubs/notificaciones"))
                {
                    contexto.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("consultar", politica =>
        politica.RequireAuthenticatedUser().RequireAssertion(contexto =>
            contexto.User.FindAll("scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains("banca.consultar")));
    opciones.AddPolicy("transferir", politica =>
        politica.RequireAuthenticatedUser().RequireAssertion(contexto =>
            contexto.User.FindAll("scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains("banca.transferir")));
});

builder.Services.AddCors(opciones =>
    opciones.AddPolicy("frontends", politica => politica
        .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.User.Identity?.Name ?? contexto.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("frontends");
app.UseRateLimiter();
app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();
app.UseMiddleware<MiddlewareCorrelacion>();
app.UseMiddleware<MiddlewareAuditoria>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/salud").AllowAnonymous();
app.MapControllers();
app.MapHub<HubNotificaciones>("/hubs/notificaciones");
await Auth.InicializarAsync(app.Services, app.Configuration);
app.Run();

static string ObtenerCadenaConexion(IConfiguration configuracion)
{
    var cadenaConfigurada = configuracion.GetConnectionString("Banco");
    if (!string.IsNullOrWhiteSpace(cadenaConfigurada))
    {
        return cadenaConfigurada;
    }

    var urlBaseDatos = configuracion["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(urlBaseDatos) &&
        Uri.TryCreate(urlBaseDatos, UriKind.Absolute, out var direccion))
    {
        var credenciales = direccion.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = direccion.Host,
            Port = direccion.Port,
            Database = direccion.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1
                ? Uri.UnescapeDataString(credenciales[1])
                : string.Empty,
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }

    var servidor = configuracion["PGHOST"];
    var baseDatos = configuracion["PGDATABASE"];
    var usuario = configuracion["PGUSER"];
    if (!string.IsNullOrWhiteSpace(servidor) &&
        !string.IsNullOrWhiteSpace(baseDatos) &&
        !string.IsNullOrWhiteSpace(usuario))
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = servidor,
            Port = int.TryParse(configuracion["PGPORT"], out var puerto) ? puerto : 5432,
            Database = baseDatos,
            Username = usuario,
            Password = configuracion["PGPASSWORD"],
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }

    throw new InvalidOperationException(
        "Falta la conexión PostgreSQL. Configure ConnectionStrings__Banco o DATABASE_URL.");
}

public partial class Program;
