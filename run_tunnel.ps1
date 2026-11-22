$ErrorActionPreference = "SilentlyContinue"

# 1. Check if port 5218 is running
$conn = Get-NetTCPConnection -LocalPort 5218 -ErrorAction SilentlyContinue
if (-not $conn) {
    Write-Output "Starting CloudPC web server on port 5218..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project CloudPCRental" -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 7
} else {
    Write-Output "CloudPC web server is already active on port 5218."
}

# 2. Stop any existing cloudflared process
Get-Process -Name "cloudflared" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 3. Start cloudflared tunnel
if (Test-Path "tunnel.log") { Remove-Item "tunnel.log" -Force }
$tunnelProc = Start-Process -FilePath ".\cloudflared.exe" -ArgumentList "tunnel --url http://127.0.0.1:5218 --protocol http2" -RedirectStandardError "tunnel.log" -PassThru -WindowStyle Hidden

# 4. Poll tunnel.log for the public trycloudflare.com URL
$foundUrl = $null
for ($i = 0; $i -lt 25; $i++) {
    Start-Sleep -Seconds 1
    if (Test-Path "tunnel.log") {
        $content = Get-Content "tunnel.log" -Raw
        if ($content -match "https://([a-z0-9\-]+)\.trycloudflare\.com" -and $matches[1] -ne "api") {
            $foundUrl = $matches[0]
            break
        }
    }
}

if ($foundUrl) {
    Write-Output "SUCCESS_TUNNEL_URL: $foundUrl"
} else {
    Write-Output "WAITING_LOGS:"
    if (Test-Path "tunnel.log") {
        Get-Content "tunnel.log" -Tail 20
    }
}
