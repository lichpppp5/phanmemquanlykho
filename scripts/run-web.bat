@echo off
title PM Tap Hoa - Web Server
cd /d "%~dp0\.."

echo ===================================================
echo   KHOI DONG PHAN MEM BAN HANG PM TAP HOA
echo ===================================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    if exist "%USERPROFILE%\AppData\Local\Microsoft\dotnet\dotnet.exe" (
        set "PATH=%USERPROFILE%\AppData\Local\Microsoft\dotnet;%PATH%"
    ) else (
        echo [LOI] May tinh chua co .NET!
        echo Vui long chay scripts\install-dependencies.bat truoc.
        pause
        exit /b 1
    )
)

echo Dang khoi chay may chu ban hang tai cong 8888...
powershell -NoProfile -Command "Start-Sleep -Milliseconds 1500; Start-Process 'http://localhost:8888'"
dotnet run --project PMTapHoa.Web/PMTapHoa.Web.csproj
pause