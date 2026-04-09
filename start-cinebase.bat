@echo off
setlocal

set "ROOT_DIR=%~dp0"
set "BACKEND_DIR=%ROOT_DIR%backend\FilmAPI"
set "FRONTEND_DIR=%ROOT_DIR%frontend\CineBase.Web"

if not exist "%BACKEND_DIR%\FilmAPI.csproj" (
  echo [ERRORE] Progetto backend non trovato in: "%BACKEND_DIR%"
  pause
  exit /b 1
)

if not exist "%FRONTEND_DIR%\CineBase.Web.csproj" (
  echo [ERRORE] Progetto frontend non trovato in: "%FRONTEND_DIR%"
  pause
  exit /b 1
)

echo Avvio CineBase...
echo - Backend:  http://localhost:5000
echo - Frontend: http://localhost:5001
echo.

start "CineBase Backend" cmd /k "cd /d "%BACKEND_DIR%" && dotnet run"
timeout /t 2 /nobreak >nul
start "CineBase Frontend" cmd /k "cd /d "%FRONTEND_DIR%" && dotnet run"

timeout /t 3 /nobreak >nul
start "" "http://localhost:5001"

echo Avvio completato. Chiudi le finestre Backend/Frontend per fermare l'app.
endlocal
