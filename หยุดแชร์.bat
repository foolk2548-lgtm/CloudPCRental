@echo off
chcp 65001 >nul
echo กำลังหยุดการแชร์ Cloudflare Tunnel...
taskkill /f /im cloudflared.exe >nul 2>&1
echo [OK] ปิดการแชร์เรียบร้อยแล้ว ลิงก์ออนไลน์หยุดทำงานแล้วครับ
pause
