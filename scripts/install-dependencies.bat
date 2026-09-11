@echo off
title PM Tap Hoa - Install Dependencies
cd /d "%~dp0\.."

echo ===================================================
echo   KIEM TRA & CAI DAT MOI TRUONG PM TAP HOA
echo ===================================================
echo.

:: 1. Kiem tra .NET
echo [1/3] Kiem tra moi truong .NET...
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    if exist "%USERPROFILE%\AppData\Local\Microsoft\dotnet\dotnet.exe" (
        set "PATH=%USERPROFILE%\AppData\Local\Microsoft\dotnet;%PATH%"
        echo   OK .NET da duoc tim thay (local).
    ) else (
        echo [CANH BAO] May tinh chua co .NET! Dang tu dong tai va cai dat...
        powershell -NoProfile -ExecutionPolicy Bypass -File "%CD%\dotnet-install.ps1" -Channel 10.0
        set "PATH=%USERPROFILE%\AppData\Local\Microsoft\dotnet;%PATH%"
    )
) else (
    echo   OK Da tim thay .NET tren may tinh.
)

:: 2. Restore NuGet packages
echo.
echo [2/3] Tai va khoi phuc thu vien NuGet...
dotnet restore PMTapHoa.Web/PMTapHoa.Web.csproj
dotnet restore PMTapHoa.Core/PMTapHoa.Core.csproj
if exist "PMTapHoa.Desktop\PMTapHoa.Desktop.csproj" (
    dotnet restore PMTapHoa.Desktop/PMTapHoa.Desktop.csproj
)

:: 3. Build kiem tra
echo.
echo [3/3] Bien dich kiem tra ung dung...
dotnet build PMTapHoa.Web/PMTapHoa.Web.csproj -c Release --no-restore

if %errorlevel% equ 0 (
    echo.
    echo ===================================================
    echo   OK TAT CA MODULE DA DUOC CAI DAT THANH CONG!
    echo   Chay phan mem: scripts\run-web.bat
    echo ===================================================
) else (
    echo.
    echo [LOI] Co loi xay ra. Kiem tra lai ket noi mang.
)

pause