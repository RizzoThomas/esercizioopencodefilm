@echo off
chcp 65001 >nul
title Setup Database CineBase
cls

echo ==========================================
echo    CineBase - Setup Database
echo ==========================================
echo.

REM Verifica che dotnet sia installato
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERRORE] .NET SDK non trovato!
    echo Installa .NET 9.0 SDK da: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [OK] .NET SDK trovato
dotnet --version
echo.

REM Verifica che il file .env esista
if not exist "%~dp0backend\.env" (
    echo [AVVISO] File .env non trovato in backend\
    echo Creo .env da .env.example...
    if exist "%~dp0backend\.env.example" (
        copy "%~dp0backend\.env.example" "%~dp0backend\.env"
        echo [OK] File .env creato. Modificalo con le tue configurazioni database.
    ) else (
        echo [AVVISO] File .env.example non trovato.
        echo Assicurati di avere configurato il database manualmente.
    )
    echo.
)

echo [INFO] Navigo nella cartella del backend...
cd /d "%~dp0backend\FilmAPI"

echo.
echo [INFO] Verifica dipendenze NuGet...
dotnet restore
if errorlevel 1 (
    echo [ERRORE] Impossibile ripristinare i pacchetti NuGet
    pause
    exit /b 1
)

echo.
echo [INFO] Creazione/aggiornamento database in corso...
dotnet ef database update
if errorlevel 1 (
    echo.
    echo [ERRORE] Impossibile aggiornare il database.
    echo.
    echo Possibili cause:
    echo 1. MySQL/MariaDB non e in esecuzione
    echo 2. Credenziali database errate nel file .env
    echo 3. Il database non e raggiungibile
    echo.
    echo Verifica che:
    echo - MySQL/MariaDB sia avviato
    echo - Le credenziali in backend/.env siano corrette
    echo - Il database esista o possa essere creato
    echo.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo    Database configurato con successo!
echo ==========================================
echo.
echo Ora puoi eseguire run.bat per avviare
echo sia il backend che il frontend.
echo.
pause