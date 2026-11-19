@echo off
chcp 65001 >nul
echo Stopping Cloudflare Tunnel sharing...
taskkill /f /im cloudflared.exe >nul 2>&1
echo [OK] Cloudflare Tunnel stopped successfully.
pause
