\set ON_ERROR_STOP on

ALTER TABLE identidad.factores_autenticacion
    ALTER COLUMN tipo TYPE varchar(20) USING tipo::text;
ALTER TABLE identidad.factores_autenticacion
    ADD CONSTRAINT ck_tipo_factor
    CHECK (tipo IN ('CLAVE', 'HUELLA', 'ROSTRO', 'TOTP', 'PASSKEY'));

ALTER TABLE identidad.onboardings
    ALTER COLUMN estado DROP DEFAULT,
    ALTER COLUMN estado TYPE varchar(30) USING estado::text,
    ALTER COLUMN estado SET DEFAULT 'INICIADO';
ALTER TABLE identidad.onboardings
    ADD CONSTRAINT ck_estado_onboarding
    CHECK (estado IN ('INICIADO', 'IDENTIDAD_VALIDADA', 'APROBADO', 'RECHAZADO'));

ALTER TABLE banca.cuentas
    ALTER COLUMN tipo TYPE varchar(20) USING tipo::text;
ALTER TABLE banca.cuentas
    ADD CONSTRAINT ck_tipo_cuenta
    CHECK (tipo IN ('AHORROS', 'CORRIENTE'));

ALTER TABLE banca.transferencias
    ALTER COLUMN estado DROP DEFAULT,
    ALTER COLUMN estado TYPE varchar(20) USING estado::text,
    ALTER COLUMN estado SET DEFAULT 'PENDIENTE';
ALTER TABLE banca.transferencias
    ADD CONSTRAINT ck_estado_transferencia
    CHECK (estado IN ('PENDIENTE', 'PROCESANDO', 'COMPLETADA', 'RECHAZADA', 'REVERSADA'));

ALTER TABLE banca.movimientos
    ALTER COLUMN tipo TYPE varchar(10) USING tipo::text;
ALTER TABLE banca.movimientos
    ADD CONSTRAINT ck_tipo_movimiento
    CHECK (tipo IN ('DEBITO', 'CREDITO'));

DROP TYPE identidad.tipo_factor;
DROP TYPE identidad.estado_onboarding;
DROP TYPE banca.tipo_cuenta;
DROP TYPE banca.estado_transaccion;
DROP TYPE banca.tipo_movimiento;
