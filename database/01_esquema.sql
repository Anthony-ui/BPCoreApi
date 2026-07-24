\set ON_ERROR_STOP on

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS banca;
CREATE SCHEMA IF NOT EXISTS identidad;
CREATE SCHEMA IF NOT EXISTS auditoria;

CREATE TABLE banca.clientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    identificacion varchar(20) NOT NULL UNIQUE,
    nombres varchar(100) NOT NULL,
    apellidos varchar(100) NOT NULL,
    correo varchar(254) NOT NULL,
    telefono varchar(30) NOT NULL,
    fecha_nacimiento date NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now(),
    actualizado_en timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE identidad.usuarios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL UNIQUE REFERENCES banca.clientes(id),
    -- Identificador (sub) emitido por Duende IdentityServer.
    sujeto_externo varchar(100) UNIQUE,
    nombre_usuario varchar(80) NOT NULL UNIQUE,
    bloqueado_hasta timestamptz,
    ultimo_acceso_en timestamptz,
    activo boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE identidad.factores_autenticacion (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL REFERENCES identidad.usuarios(id),
    tipo varchar(20) NOT NULL,
    referencia_proveedor varchar(200),
    verificado_en timestamptz,
    activo boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now(),
    UNIQUE (usuario_id, tipo),
    CONSTRAINT ck_tipo_factor CHECK (tipo IN ('CLAVE', 'HUELLA', 'ROSTRO', 'TOTP', 'PASSKEY'))
);

CREATE TABLE identidad.onboardings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid REFERENCES banca.clientes(id),
    estado varchar(30) NOT NULL DEFAULT 'INICIADO',
    proveedor_biometrico varchar(80) NOT NULL,
    referencia_verificacion varchar(200) NOT NULL UNIQUE,
    puntaje_rostro numeric(5,4),
    prueba_vida_superada boolean,
    iniciado_en timestamptz NOT NULL DEFAULT now(),
    finalizado_en timestamptz,
    -- Nunca se almacena la fotografía ni la plantilla biométrica.
    CONSTRAINT ck_puntaje_rostro CHECK (puntaje_rostro IS NULL OR puntaje_rostro BETWEEN 0 AND 1),
    CONSTRAINT ck_estado_onboarding CHECK (estado IN ('INICIADO', 'IDENTIDAD_VALIDADA', 'APROBADO', 'RECHAZADO'))
);

CREATE TABLE banca.cuentas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL REFERENCES banca.clientes(id),
    numero varchar(24) NOT NULL UNIQUE,
    tipo varchar(20) NOT NULL,
    moneda char(3) NOT NULL DEFAULT 'USD',
    saldo_disponible numeric(18,2) NOT NULL DEFAULT 0,
    activa boolean NOT NULL DEFAULT true,
    creado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_saldo_disponible CHECK (saldo_disponible >= 0),
    CONSTRAINT ck_tipo_cuenta CHECK (tipo IN ('AHORROS', 'CORRIENTE'))
);

CREATE TABLE banca.beneficiarios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL REFERENCES banca.clientes(id),
    alias varchar(80) NOT NULL,
    numero_cuenta varchar(24) NOT NULL,
    institucion_financiera varchar(120) NOT NULL,
    identificacion_beneficiario varchar(20) NOT NULL,
    nombre_beneficiario varchar(200) NOT NULL,
    es_cuenta_propia boolean NOT NULL DEFAULT false,
    creado_en timestamptz NOT NULL DEFAULT now(),
    UNIQUE (cliente_id, numero_cuenta, institucion_financiera)
);

CREATE TABLE banca.transferencias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cuenta_origen_id uuid NOT NULL REFERENCES banca.cuentas(id),
    beneficiario_id uuid REFERENCES banca.beneficiarios(id),
    cuenta_destino varchar(24) NOT NULL,
    institucion_destino varchar(120) NOT NULL,
    monto numeric(18,2) NOT NULL,
    moneda char(3) NOT NULL DEFAULT 'USD',
    concepto varchar(200),
    estado varchar(20) NOT NULL DEFAULT 'PENDIENTE',
    clave_idempotencia varchar(100) NOT NULL UNIQUE,
    referencia_core varchar(100),
    creado_en timestamptz NOT NULL DEFAULT now(),
    procesado_en timestamptz,
    CONSTRAINT ck_transferencia_monto CHECK (monto > 0),
    CONSTRAINT ck_estado_transferencia CHECK (estado IN ('PENDIENTE', 'PROCESANDO', 'COMPLETADA', 'RECHAZADA', 'REVERSADA'))
);

CREATE TABLE banca.movimientos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cuenta_id uuid NOT NULL REFERENCES banca.cuentas(id),
    transferencia_id uuid REFERENCES banca.transferencias(id),
    referencia_core varchar(100) NOT NULL,
    tipo varchar(10) NOT NULL,
    monto numeric(18,2) NOT NULL,
    saldo_resultante numeric(18,2),
    descripcion varchar(250) NOT NULL,
    ocurrido_en timestamptz NOT NULL,
    creado_en timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_movimiento_monto CHECK (monto > 0),
    CONSTRAINT ck_tipo_movimiento CHECK (tipo IN ('DEBITO', 'CREDITO')),
    UNIQUE (cuenta_id, referencia_core)
);

CREATE TABLE banca.notificaciones (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL REFERENCES banca.clientes(id),
    transferencia_id uuid REFERENCES banca.transferencias(id),
    canal varchar(20) NOT NULL,
    proveedor varchar(80) NOT NULL,
    destino_enmascarado varchar(254) NOT NULL,
    estado varchar(20) NOT NULL DEFAULT 'PENDIENTE',
    intentos smallint NOT NULL DEFAULT 0,
    referencia_proveedor varchar(150),
    creado_en timestamptz NOT NULL DEFAULT now(),
    enviado_en timestamptz,
    CONSTRAINT ck_canal_notificacion CHECK (canal IN ('EMAIL', 'SMS', 'PUSH')),
    CONSTRAINT ck_intentos_notificacion CHECK (intentos >= 0)
);

CREATE TABLE auditoria.eventos (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usuario_id uuid,
    cliente_id uuid,
    accion varchar(100) NOT NULL,
    recurso varchar(120) NOT NULL,
    recurso_id varchar(100),
    resultado varchar(30) NOT NULL,
    direccion_ip inet,
    agente_usuario varchar(500),
    correlacion_id uuid NOT NULL,
    datos jsonb NOT NULL DEFAULT '{}'::jsonb,
    ocurrido_en timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_cuentas_cliente ON banca.cuentas(cliente_id);
CREATE INDEX ix_movimientos_cuenta_fecha ON banca.movimientos(cuenta_id, ocurrido_en DESC);
CREATE INDEX ix_transferencias_cuenta_fecha ON banca.transferencias(cuenta_origen_id, creado_en DESC);
CREATE INDEX ix_notificaciones_estado ON banca.notificaciones(estado, creado_en);
CREATE INDEX ix_eventos_cliente_fecha ON auditoria.eventos(cliente_id, ocurrido_en DESC);
CREATE INDEX ix_eventos_correlacion ON auditoria.eventos(correlacion_id);

-- Datos mínimos para probar consultas y transferencias.
INSERT INTO banca.clientes
    (id, identificacion, nombres, apellidos, correo, telefono, fecha_nacimiento)
VALUES
    ('10000000-0000-0000-0000-000000000001', '0912345678', 'Ana', 'Pérez',
     'ana.perez@example.com', '+593999000111', DATE '1990-05-14')
ON CONFLICT DO NOTHING;

INSERT INTO identidad.usuarios (id, cliente_id, sujeto_externo, nombre_usuario)
VALUES
    ('20000000-0000-0000-0000-000000000001',
     '10000000-0000-0000-0000-000000000001',
     'usuario-demo', 'ana.perez')
ON CONFLICT DO NOTHING;

INSERT INTO banca.cuentas (id, cliente_id, numero, tipo, saldo_disponible)
VALUES
    ('30000000-0000-0000-0000-000000000001',
     '10000000-0000-0000-0000-000000000001',
     '001000000001', 'AHORROS', 2500.00),
    ('30000000-0000-0000-0000-000000000002',
     '10000000-0000-0000-0000-000000000001',
     '001000000002', 'CORRIENTE', 800.00)
ON CONFLICT DO NOTHING;

INSERT INTO banca.movimientos
    (cuenta_id, referencia_core, tipo, monto, saldo_resultante, descripcion, ocurrido_en)
VALUES
    ('30000000-0000-0000-0000-000000000001', 'CORE-DEMO-001',
     'CREDITO', 2500.00, 2500.00, 'Depósito inicial', now())
ON CONFLICT DO NOTHING;
