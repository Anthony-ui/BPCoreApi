\set ON_ERROR_STOP on

-- Retira las tablas temporales de ASP.NET Identity. La autenticación utiliza
DROP TABLE IF EXISTS identidad.usuarios_tokens;
DROP TABLE IF EXISTS identidad.usuarios_roles;
DROP TABLE IF EXISTS identidad.usuarios_logins;
DROP TABLE IF EXISTS identidad.usuarios_claims;
DROP TABLE IF EXISTS identidad.roles_claims;
DROP TABLE IF EXISTS identidad.roles_acceso;
DROP TABLE IF EXISTS identidad.usuarios_acceso;
DROP TABLE IF EXISTS identidad.claves_proteccion;
DROP TABLE IF EXISTS identidad.__migraciones_identidad;

ALTER TABLE identidad.usuarios
    ADD COLUMN IF NOT EXISTS hash_clave text,
    ADD COLUMN IF NOT EXISTS sello_seguridad varchar(64),
    ADD COLUMN IF NOT EXISTS intentos_fallidos smallint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS ultimo_cambio_clave_en timestamptz,
    ADD CONSTRAINT ck_intentos_usuario CHECK (intentos_fallidos >= 0);

UPDATE identidad.usuarios
SET nombre_usuario = 'tony2302',
    sujeto_externo = COALESCE(sujeto_externo, id::text),
    sello_seguridad = COALESCE(sello_seguridad, gen_random_uuid()::text),
    ultimo_cambio_clave_en = now()
WHERE cliente_id = '10000000-0000-0000-0000-000000000001';
