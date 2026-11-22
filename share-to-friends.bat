@echo off
chcp 65001 >nul
title CloudPC Rental - Share to Friends Online
color 0B

echo ============================================================
echo    CloudPC Rental - Online Sharing for Friends
echo ============================================================
echo.

:: 1. Check if server is running on port 5218
netstat -ano | findstr :5218 >nul
if %errorlevel% equ 0 (
    echo [OK] CloudPC Web is already running on http://localhost:5218
) else (
    echo [!] CloudPC Web is not running yet.
    echo [*] Starting web server automatically, please wait a few seconds...
    start "CloudPC Web Server" cmd /k "cd /d "%~dp0CloudPCRental" && dotnet run"
    timeout /t 6 /nobreak >nul
)

echo.
echo ============================================================
echo  [HOW TO SHARE WITH FRIENDS]
echo  1. Look for the line containing:
echo     https://xxxxxxxx.trycloudflare.com
echo  2. Copy that link and send it to your friends via LINE / Discord!
echo  3. Your friends can open it on mobile, tablet, or desktop.
echo  4. When done, simply close this window to stop sharing.
echo ============================================================
echo.
echo Starting Cloudflare Tunnel, please wait...
echo.

"%~dp0cloudflared.exe" tunnel --url http://127.0.0.1:5218 --protocol http2

pause
