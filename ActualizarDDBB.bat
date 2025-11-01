

@echo off
setlocal enabledelayedexpansion

:: Obtener timestamp AAAAMMDDhhmmss
for /f %%a in ('wmic os get localdatetime ^| find "."') do set dt=%%a
set timestamp=%dt:~0,4%%dt:~4,2%%dt:~6,2%%dt:~8,2%%dt:~10,2%%dt:~12,2%

:: Nombre base para la migraci n (pod s cambiarlo)
set migrationName=AutoMigration_LicitAR

:: Ruta relativa del proyecto donde est  el DbContext
set project=LogicaDeNegocio

:: Ruta relativa del proyecto startup (donde est  Program.cs)
set startup=Veterinaria

:: Directorio donde se guardan las migraciones
set outputDir=Migrations/DDBB



echo === Generando migraciones para todos los DbContexts ===

dotnet ef migrations add AutoMigration_Prog4_%timestamp% --context AppDbContext --output-dir Migrations/DDBB --project LogicaDeNegocio --startup-project Veterinaria

echo === Aplicando migraciones ===

dotnet ef database update --context AppDbContext --project LogicaDeNegocio --startup-project Veterinaria

echo === Listo! ===
pause