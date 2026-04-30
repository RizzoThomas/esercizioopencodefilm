@echo off
chcp 65001 >nul
title Avvio CineBase - Film App
cls

echo ==========================================
echo    CineBase - Avvio Applicazione
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

REM Verifica che il file .env esista nel backend
if not exist "%~dp0backend\.env" (
    echo [AVVISO] File .env non trovato in backend\
    echo.
    echo Esegui prima setup-db.bat per configurare il database:
    echo   setup-db.bat
    echo.
    pause
    exit /b 1
)

REM Avvia il Backend (porta 5000)
echo [INFO] Avvio Backend API su http://localhost:5000 ...
echo        Il backend applichera automaticamente le migrazioni
echo        e creera i dati di esempio (admin, film, cinema, ecc.)
echo.
start "CineBase - Backend API" cmd /k "cd /d "%~dp0backend\FilmAPI" && dotnet run --urls http://localhost:5000"

REM Attendi 8 secondi per dare tempo al backend di avviarsi e fare il seeding
echo [INFO] Attendo avvio backend e seeding dati...
timeout /t 8 /nobreak >nul

REM Avvia il Frontend (porta 5001)
echo [INFO] Avvio Frontend Web su http://localhost:5001 ...
start "CineBase - Frontend Web" cmd /k "cd /d "%~dp0frontend\CineBase.Web" && dotnet run --urls http://localhost:5001"

REM Attendi 3 secondi per dare tempo al frontend di avviarsi
timeout /t 3 /nobreak >nul

echo.
echo ==========================================
echo    Applicazioni avviate!
echo ==========================================
echo.
echo Backend API:   http://localhost:5000
echo Frontend Web:  http://localhost:5001
echo.
echo Dati di accesso predefiniti:
echo   Admin: admin@cinebase.it / Admin123!
echo.
echo Apro il browser sul Frontend...
start "" http://localhost:5001

echo.
echo Le finestre rimarranno aperte per visualizzare i log.
echo Chiudi le finestre per fermare i server.
echo.
echo Per ricreare il database, esegui: setup-db.bat
echo.
pause
    exit /b 1
)

echo [OK] .NET SDK trovato
dotnet --version
echo.

REM Avvia il Backend (porta 5000)
echo [INFO] Avvio Backend API su http://localhost:5000 ...
start "CineBase - Backend API" cmd /k "cd /d "%~dp0backend\FilmAPI" && dotnet run --urls http://localhost:5000"

REM Attendi 5 secondi per dare tempo al backend di avviarsi
timeout /t 5 /nobreak >nul

REM Avvia il Frontend (porta 5001)
echo [INFO] Avvio Frontend Web su http://localhost:5001 ...
start "CineBase - Frontend Web" cmd /k "cd /d "%~dp0frontend\CineBase.Web" && dotnet run --urls http://localhost:5001"

REM Attendi 3 secondi per dare tempo al frontend di avviarsi
timeout /t 3 /nobreak >nul

echo.
echo ==========================================
echo    Applicazioni avviate!
echo ==========================================
echo.
echo Backend API:   http://localhost:5000
echo Frontend Web:  http://localhost:5001
echo.
echo Apro il browser sul Frontend...
start "" http://localhost:5001

echo.
echo Le finestre rimarranno aperte per visualizzare i log.
echo Chiudi le finestre per fermare i server.
echo.
pause
