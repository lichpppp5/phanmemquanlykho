@echo off
title PM Tap Hoa - Setup Local Domain

echo ===================================================
echo   CAU HINH TEN MIEN TAPHOA.LOCAL VAO FILE HOSTS
echo ===================================================
echo.

:: Kiem tra quyen Admin
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "HOSTS_FILE=%WINDIR%\System32\drivers\etc\hosts"

:: Kiem tra da co taphoa.local chua
findstr /I "taphoa.local" "%HOSTS_FILE%" >nul 2>&1
if %errorlevel% equ 0 (
    echo   OK Ten mien taphoa.local da ton tai trong file hosts.
) else (
    echo.>> "%HOSTS_FILE%"
    echo 127.0.0.1    taphoa.local>> "%HOSTS_FILE%"
    echo   OK Da them thanh cong: 127.0.0.1   taphoa.local
)

:: Xoa cache DNS
ipconfig /flushdns >nul 2>&1
echo   OK Da lam moi bo nho dem DNS.
echo.
echo ===================================================
echo   HOAN TAT! BAY GIO BAN CO THE TRUY CAP:
echo   http://taphoa.local:8888
echo   hoac: http://localhost:8888
echo ===================================================
echo.
pause