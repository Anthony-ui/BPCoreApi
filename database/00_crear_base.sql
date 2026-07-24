-- Ejecutar conectado a la base "postgres" con un usuario administrador.
-- Este script es idempotente cuando se invoca desde database/crear-base.ps1.

CREATE ROLE admin WITH LOGIN PASSWORD '123';
ALTER ROLE admin CREATEDB;

CREATE DATABASE bp_banca
    WITH OWNER = admin
         ENCODING = 'UTF8'
         TEMPLATE = template0;
