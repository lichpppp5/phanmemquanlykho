@echo off
setlocal EnableExtensions EnableDelayedExpansion

cd /d "%~dp0\.."

set "PROJECT=PMTapHoa.Desktop\PMTapHoa.Desktop.csproj"
set "CONFIG=Release"
set "RID=win-x64"
set "APP_VERSION=1.0.0"
set "PUBLISH_DIR=%CD%\PMTapHoa.Desktop\bin\%CONFIG%\net8.0-windows\%RID%\publish"
set "TEMPLATE_ISS=%CD%\installer\PMTapHoa.Setup.template.iss"
set "GENERATED_ISS=%CD%\installer\PMTapHoa.Setup.generated.iss"
set "OUTPUT_DIR=%CD%\installer-output"
set "APP_EXE=PMTapHoa.Desktop.exe"

echo ===============================================
echo Building PM Tap Hoa installer package
echo ===============================================

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] dotnet SDK was not found. Please install .NET 8 SDK first.
  exit /b 1
)

if not exist "%PROJECT%" (
  echo [ERROR] Project file not found: %PROJECT%
  exit /b 1
)

echo [1/5] Restoring packages...
dotnet restore "%PROJECT%"
if errorlevel 1 (
  echo [ERROR] dotnet restore failed.
  exit /b 1
)

echo [2/5] Publishing self-contained app...
dotnet publish "%PROJECT%" -c %CONFIG% -r %RID% --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
  echo [ERROR] dotnet publish failed.
  exit /b 1
)

if not exist "%PUBLISH_DIR%" (
  echo [ERROR] Publish directory not found: %PUBLISH_DIR%
  exit /b 1
)

if not exist "%PUBLISH_DIR%\%APP_EXE%" (
  for %%F in ("%PUBLISH_DIR%\*.exe") do (
    set "APP_EXE=%%~nxF"
    goto :foundexe
  )
)

:foundexe
if not exist "%PUBLISH_DIR%\%APP_EXE%" (
  echo [ERROR] Could not detect published app .exe in: %PUBLISH_DIR%
  exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo [3/5] Generating Inno Setup script...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$template = Get-Content -Raw '%TEMPLATE_ISS%';" ^
  "$content = $template.Replace('__APP_VERSION__','%APP_VERSION%').Replace('__PUBLISH_DIR__','%PUBLISH_DIR%').Replace('__APP_EXE__','%APP_EXE%');" ^
  "Set-Content -Path '%GENERATED_ISS%' -Value $content -Encoding UTF8;"
if errorlevel 1 (
  echo [ERROR] Failed to generate Inno Setup script.
  exit /b 1
)

set "ISCC_PATH="
where iscc >nul 2>nul
if not errorlevel 1 (
  for /f "delims=" %%I in ('where iscc') do (
    set "ISCC_PATH=%%I"
    goto :gotiscc
  )
)

if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not defined ISCC_PATH if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"

:gotiscc
if defined ISCC_PATH (
  echo [4/5] Building setup .exe using Inno Setup...
  "%ISCC_PATH%" "%GENERATED_ISS%"
  if errorlevel 1 (
    echo [ERROR] Inno Setup compilation failed.
    exit /b 1
  )
  echo [5/5] Done.
  echo Installer output: %OUTPUT_DIR%
  exit /b 0
)

echo [4/5] Inno Setup not found. Creating portable zip package instead...
set "ZIP_PATH=%OUTPUT_DIR%\PMTapHoa_Portable_%APP_VERSION%.zip"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "if (Test-Path '%ZIP_PATH%') { Remove-Item '%ZIP_PATH%' -Force };" ^
  "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force;"
if errorlevel 1 (
  echo [ERROR] Failed to create portable zip package.
  exit /b 1
)

echo [5/5] Done.
echo Portable package output: %ZIP_PATH%
echo Tip: Install Inno Setup 6 to generate installer .exe automatically.
exit /b 0
