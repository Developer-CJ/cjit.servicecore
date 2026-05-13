@echo off
setlocal
cd /d "%~dp0"
title CJIT ServiceCore Desktop Edition v3

echo ==========================================================
echo  CJIT ServiceCore Desktop Edition v3
echo  Front-counter POS and service workflow system
echo ==========================================================
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK was not found.
    echo Install .NET 8 SDK from Microsoft, then run this again.
    pause
    exit /b 1
)

echo Restoring packages...
dotnet restore
if errorlevel 1 goto fail

echo Building release...
dotnet build -c Release
if errorlevel 1 goto fail

echo Launching CJIT ServiceCore...
dotnet run -c Release
goto end

:fail
echo.
echo Build failed. Copy the errors and send them back to ChatGPT.
pause
exit /b 1

:end
endlocal
