@echo off
chcp 65001 >nul
title CineBase - Seed Database
cls

echo ==========================================
echo    CineBase - Seed Database
echo    Popola il DB con film reali da TMDB
echo ==========================================
echo.

REM Verifica dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERRORE] .NET SDK non trovato!
    pause
    exit /b 1
)
echo [OK] .NET SDK trovato
dotnet --version
echo.

REM Verifica .env
if not exist "%~dp0backend\.env" (
    echo [ERRORE] File backend\.env non trovato!
    echo Copia backend\.env.example in backend\.env
    echo e imposta TMDB_BEARER_TOKEN.
    pause
    exit /b 1
)

REM Parsing argomenti
set SEED_ARGS=
set NEED_FORCE=0

:parse_args
if "%1"=="" goto end_parse
if /i "%1"=="--reset-shows" (
    set SEED_ARGS=--reset-shows
    set NEED_FORCE=1
)
if /i "%1"=="--reset-all" (
    set SEED_ARGS=--reset-all
    set NEED_FORCE=1
)
if /i "%1"=="--force" (
    set SEED_ARGS=%SEED_ARGS% --force
)
shift
goto parse_args
:end_parse

if "%NEED_FORCE%"=="1" (
    echo [INFO] Modalita reset attiva
    echo.
)

echo.
echo [INFO] Esecuzione seeder in corso...
echo I film vengono recuperati da TMDB.
echo Le date della programmazione sono automatiche
echo (basate sulla data odierna, sempre in avanti).
echo.

dotnet run --project "%~dp0backend\scripts\FilmApiSeeder\FilmApiSeeder.csproj" %SEED_ARGS%

if errorlevel 1 (
    echo.
    echo [ERRORE] Seed fallito.
    echo Verifica che:
    echo - MySQL/MariaDB sia in esecuzione
    echo - backend\.env abbia TMDB_BEARER_TOKEN valido
    echo - Le credenziali DB in backend\.env siano corrette
    pause
    exit /b 1
)

echo.
echo ==========================================
echo    Database popolato con successo!
echo ==========================================
echo.
echo Il seeder ha generato:
echo - Film reali da TMDB
echo - Registri, categorie
echo - 20 cinema italiani con sale e posti
echo - Programmazione dinamica (7 giorni in avanti)
echo.
echo Le date degli spettacoli sono calcolate
echo automaticamente da oggi + offset.
echo.
pause
