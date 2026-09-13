@echo off
chcp 65001 >nul
echo ===================================================
echo   NAP CODE MANG HINH PHU ESP32-C3 SUPER MINI (COM5)
echo ===================================================
echo.
echo [Chu y] Neu tren trinh duyet web dang ket noi USB, hay bam "Ngat ket noi" truoc khi chay tool nay!
echo.

set ARDUINO_CLI="C:\Program Files\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe"
set CONFIG_DIR=D:\Arduino15
set SKETCH_DIR=%~dp0esp32_c3_tft28_customer_display
set FQBN=esp32:esp32:esp32c3:CDCOnBoot=cdc
set PORT=COM5

if not exist %ARDUINO_CLI% (
    echo [LOI] Khong tim thay arduino-cli.exe tai %ARDUINO_CLI%
    pause
    exit /b 1
)

echo [1/2] Dang bien dich firmware (compile)...
%ARDUINO_CLI% compile --config-dir "%CONFIG_DIR%" --fqbn %FQBN% --libraries "D:\Arduino_Code\libraries" "%SKETCH_DIR%"
if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai!
    pause
    exit /b 1
)

echo.
echo [2/2] Dang nap firmware vao ESP32 qua cong %PORT% (upload)...
%ARDUINO_CLI% upload -p %PORT% --config-dir "%CONFIG_DIR%" --fqbn %FQBN% "%SKETCH_DIR%"
if errorlevel 1 (
    echo.
    echo [LOI] Nap that bai! Kiem tra lai cong %PORT% (co the dang bi trinh duyet mo).
    pause
    exit /b 1
)

echo.
echo ===================================================
echo   DA NAP THANH CONG FIRMWARE VAO ESP32-C3!
echo   Man hinh TFT 2.8 da san sang hien thi QR.
echo ===================================================
pause
