$ErrorActionPreference = 'Stop'

$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$env:PGPASSWORD = '123'

$rolExiste = & $psql -h localhost -p 5432 -U postgres -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname = 'admin'"
if ($rolExiste -ne '1') {
    & $psql -h localhost -p 5432 -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE ROLE admin WITH LOGIN PASSWORD '123'; ALTER ROLE admin CREATEDB;"
}

$baseExiste = & $psql -h localhost -p 5432 -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'bp_banca'"
if ($baseExiste -ne '1') {
    & $psql -h localhost -p 5432 -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE bp_banca WITH OWNER = admin ENCODING = 'UTF8' TEMPLATE = template0;"
    & $psql -h localhost -p 5432 -U admin -d bp_banca -v ON_ERROR_STOP=1 -f "$PSScriptRoot\01_esquema.sql"
}

Remove-Item Env:\PGPASSWORD
Write-Host 'Base bp_banca creada y datos de demostración cargados.'
