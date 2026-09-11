@echo off
title PM Tap Hoa - Build Installer
cd /d "%~dp0\.."

echo ===================================================
echo   BUILD PHAN MEM PM TAP HOA - PORTABLE PACKAGE
echo ===================================================
echo.

:: Kiem tra .NET
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    if exist "%USERPROFILE%\AppData\Local\Microsoft\dotnet\dotnet.exe" (
        set "PATH=%USERPROFILE%\AppData\Local\Microsoft\dotnet;%PATH%"
    ) else (
        echo [LOI] Chua co .NET SDK! Chay scripts\install-dependencies.bat truoc.
        pause
        exit /b 1
    )
)

:: Tao thu muc output
if not exist "dist" mkdir dist

echo [1/3] Dang publish ung dung (self-contained portable)...
dotnet publish PMTapHoa.Web/PMTapHoa.Web.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o dist\PMTapHoa_Portable

if %errorlevel% neq 0 (
    echo [LOI] Build that bai! Kiem tra lai code.
    pause
    exit /b 1
)

echo.
echo [2/3] Copy file khoi dong...
copy "scripts\run-web.bat" "dist\PMTapHoa_Portable\start.bat" >nul 2>nul

echo.
echo [3/3] Tao file ZIP dong goi...
powershell -NoProfile -Command ^
    "Compress-Archive -Path 'dist\PMTapHoa_Portable\*' -DestinationPath 'dist\PMTapHoa_Portable.zip' -Force"

echo.
echo ===================================================
echo   BUILD HOAN TAT!
echo   File portable: dist\PMTapHoa_Portable\
echo   File ZIP:      dist\PMTapHoa_Portable.zip
echo   Copy thu muc hoac file ZIP sang may khach.
echo   Chay file start.bat de khoi dong.
echo ===================================================
echo.
pause