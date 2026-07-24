using System;
using System.Collections.Generic;
using BPCoreApi.Modelos;
using Microsoft.EntityFrameworkCore;

namespace BPCoreApi.Datos;

public partial class ContextoBanco : DbContext
{
    public ContextoBanco(DbContextOptions<ContextoBanco> options)
        : base(options)
    {
    }

    public virtual DbSet<Beneficiario> Beneficiarios { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Cuenta> Cuentas { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<FactoresAutenticacion> FactoresAutenticacions { get; set; }

    public virtual DbSet<Movimiento> Movimientos { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Onboarding> Onboardings { get; set; }

    public virtual DbSet<Transferencia> Transferencias { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Beneficiario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("beneficiarios_pkey");

            entity.ToTable("beneficiarios", "banca");

            entity.HasIndex(e => new { e.ClienteId, e.NumeroCuenta, e.InstitucionFinanciera }, "beneficiarios_cliente_id_numero_cuenta_institucion_financie_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Alias)
                .HasMaxLength(80)
                .HasColumnName("alias");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.EsCuentaPropia).HasColumnName("es_cuenta_propia");
            entity.Property(e => e.IdentificacionBeneficiario)
                .HasMaxLength(20)
                .HasColumnName("identificacion_beneficiario");
            entity.Property(e => e.InstitucionFinanciera)
                .HasMaxLength(120)
                .HasColumnName("institucion_financiera");
            entity.Property(e => e.NombreBeneficiario)
                .HasMaxLength(200)
                .HasColumnName("nombre_beneficiario");
            entity.Property(e => e.NumeroCuenta)
                .HasMaxLength(24)
                .HasColumnName("numero_cuenta");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Beneficiarios)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("beneficiarios_cliente_id_fkey");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clientes_pkey");

            entity.ToTable("clientes", "banca");

            entity.HasIndex(e => e.Identificacion, "clientes_identificacion_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.Correo)
                .HasMaxLength(254)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.Identificacion)
                .HasMaxLength(20)
                .HasColumnName("identificacion");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cuentas_pkey");

            entity.ToTable("cuentas", "banca");

            entity.HasIndex(e => e.Numero, "cuentas_numero_key").IsUnique();

            entity.HasIndex(e => e.ClienteId, "ix_cuentas_cliente");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .HasDefaultValueSql("'USD'::bpchar")
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.Numero)
                .HasMaxLength(24)
                .HasColumnName("numero");
            entity.Property(e => e.SaldoDisponible)
                .HasPrecision(18, 2)
                .HasColumnName("saldo_disponible");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cuenta)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cuentas_cliente_id_fkey");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventos_pkey");

            entity.ToTable("eventos", "auditoria");

            entity.HasIndex(e => new { e.ClienteId, e.OcurridoEn }, "ix_eventos_cliente_fecha").IsDescending(false, true);

            entity.HasIndex(e => e.CorrelacionId, "ix_eventos_correlacion");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .HasColumnName("accion");
            entity.Property(e => e.AgenteUsuario)
                .HasMaxLength(500)
                .HasColumnName("agente_usuario");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CorrelacionId).HasColumnName("correlacion_id");
            entity.Property(e => e.Datos)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("datos");
            entity.Property(e => e.DireccionIp).HasColumnName("direccion_ip");
            entity.Property(e => e.OcurridoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("ocurrido_en");
            entity.Property(e => e.Recurso)
                .HasMaxLength(120)
                .HasColumnName("recurso");
            entity.Property(e => e.RecursoId)
                .HasMaxLength(100)
                .HasColumnName("recurso_id");
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .HasColumnName("resultado");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        });

        modelBuilder.Entity<FactoresAutenticacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("factores_autenticacion_pkey");

            entity.ToTable("factores_autenticacion", "identidad");

            entity.HasIndex(e => new { e.UsuarioId, e.Tipo }, "factores_autenticacion_usuario_id_tipo_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.ReferenciaProveedor)
                .HasMaxLength(200)
                .HasColumnName("referencia_proveedor");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.VerificadoEn).HasColumnName("verificado_en");

            entity.HasOne(d => d.Usuario).WithMany(p => p.FactoresAutenticacions)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factores_autenticacion_usuario_id_fkey");
        });

        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("movimientos_pkey");

            entity.ToTable("movimientos", "banca");

            entity.HasIndex(e => new { e.CuentaId, e.OcurridoEn }, "ix_movimientos_cuenta_fecha").IsDescending(false, true);

            entity.HasIndex(e => new { e.CuentaId, e.ReferenciaCore }, "movimientos_cuenta_id_referencia_core_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.CuentaId).HasColumnName("cuenta_id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.Monto)
                .HasPrecision(18, 2)
                .HasColumnName("monto");
            entity.Property(e => e.OcurridoEn).HasColumnName("ocurrido_en");
            entity.Property(e => e.ReferenciaCore)
                .HasMaxLength(100)
                .HasColumnName("referencia_core");
            entity.Property(e => e.SaldoResultante)
                .HasPrecision(18, 2)
                .HasColumnName("saldo_resultante");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .HasColumnName("tipo");
            entity.Property(e => e.TransferenciaId).HasColumnName("transferencia_id");

            entity.HasOne(d => d.Cuenta).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.CuentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movimientos_cuenta_id_fkey");

            entity.HasOne(d => d.Transferencia).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.TransferenciaId)
                .HasConstraintName("movimientos_transferencia_id_fkey");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notificaciones_pkey");

            entity.ToTable("notificaciones", "banca");

            entity.HasIndex(e => new { e.Estado, e.CreadoEn }, "ix_notificaciones_estado");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Canal)
                .HasMaxLength(20)
                .HasColumnName("canal");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.DestinoEnmascarado)
                .HasMaxLength(254)
                .HasColumnName("destino_enmascarado");
            entity.Property(e => e.EnviadoEn).HasColumnName("enviado_en");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDIENTE'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.Intentos).HasColumnName("intentos");
            entity.Property(e => e.Proveedor)
                .HasMaxLength(80)
                .HasColumnName("proveedor");
            entity.Property(e => e.ReferenciaProveedor)
                .HasMaxLength(150)
                .HasColumnName("referencia_proveedor");
            entity.Property(e => e.TransferenciaId).HasColumnName("transferencia_id");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificaciones_cliente_id_fkey");

            entity.HasOne(d => d.Transferencia).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.TransferenciaId)
                .HasConstraintName("notificaciones_transferencia_id_fkey");
        });

        modelBuilder.Entity<Onboarding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("onboardings_pkey");

            entity.ToTable("onboardings", "identidad");

            entity.HasIndex(e => e.ReferenciaVerificacion, "onboardings_referencia_verificacion_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(30)
                .HasDefaultValueSql("'INICIADO'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FinalizadoEn).HasColumnName("finalizado_en");
            entity.Property(e => e.IniciadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("iniciado_en");
            entity.Property(e => e.ProveedorBiometrico)
                .HasMaxLength(80)
                .HasColumnName("proveedor_biometrico");
            entity.Property(e => e.PruebaVidaSuperada).HasColumnName("prueba_vida_superada");
            entity.Property(e => e.PuntajeRostro)
                .HasPrecision(5, 4)
                .HasColumnName("puntaje_rostro");
            entity.Property(e => e.ReferenciaVerificacion)
                .HasMaxLength(200)
                .HasColumnName("referencia_verificacion");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Onboardings)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("onboardings_cliente_id_fkey");
        });

        modelBuilder.Entity<Transferencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transferencias_pkey");

            entity.ToTable("transferencias", "banca");

            entity.HasIndex(e => new { e.CuentaOrigenId, e.CreadoEn }, "ix_transferencias_cuenta_fecha").IsDescending(false, true);

            entity.HasIndex(e => e.ClaveIdempotencia, "transferencias_clave_idempotencia_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BeneficiarioId).HasColumnName("beneficiario_id");
            entity.Property(e => e.ClaveIdempotencia)
                .HasMaxLength(100)
                .HasColumnName("clave_idempotencia");
            entity.Property(e => e.Concepto)
                .HasMaxLength(200)
                .HasColumnName("concepto");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.CuentaDestino)
                .HasMaxLength(24)
                .HasColumnName("cuenta_destino");
            entity.Property(e => e.CuentaOrigenId).HasColumnName("cuenta_origen_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDIENTE'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.InstitucionDestino)
                .HasMaxLength(120)
                .HasColumnName("institucion_destino");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .HasDefaultValueSql("'USD'::bpchar")
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.Monto)
                .HasPrecision(18, 2)
                .HasColumnName("monto");
            entity.Property(e => e.ProcesadoEn).HasColumnName("procesado_en");
            entity.Property(e => e.ReferenciaCore)
                .HasMaxLength(100)
                .HasColumnName("referencia_core");

            entity.HasOne(d => d.Beneficiario).WithMany(p => p.Transferencia)
                .HasForeignKey(d => d.BeneficiarioId)
                .HasConstraintName("transferencias_beneficiario_id_fkey");

            entity.HasOne(d => d.CuentaOrigen).WithMany(p => p.Transferencia)
                .HasForeignKey(d => d.CuentaOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transferencias_cuenta_origen_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios", "identidad");

            entity.HasIndex(e => e.ClienteId, "usuarios_cliente_id_key").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "usuarios_nombre_usuario_key").IsUnique();

            entity.HasIndex(e => e.SujetoExterno, "usuarios_sujeto_externo_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.BloqueadoHasta).HasColumnName("bloqueado_hasta");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_en");
            entity.Property(e => e.HashClave).HasColumnName("hash_clave");
            entity.Property(e => e.IntentosFallidos).HasColumnName("intentos_fallidos");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(80)
                .HasColumnName("nombre_usuario");
            entity.Property(e => e.SelloSeguridad)
                .HasMaxLength(64)
                .HasColumnName("sello_seguridad");
            entity.Property(e => e.SujetoExterno)
                .HasMaxLength(100)
                .HasColumnName("sujeto_externo");
            entity.Property(e => e.UltimoAccesoEn).HasColumnName("ultimo_acceso_en");
            entity.Property(e => e.UltimoCambioClaveEn).HasColumnName("ultimo_cambio_clave_en");

            entity.HasOne(d => d.Cliente).WithOne(p => p.Usuario)
                .HasForeignKey<Usuario>(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_cliente_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
