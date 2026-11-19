$ErrorActionPreference = "SilentlyContinue"

# 1. Start web app if not running
$conn = Get-NetTCPConnection -LocalPort 5218 -ErrorAction SilentlyContinue
if (-not $conn) {
    Write-Host "[1/2] Starting CloudPC on port 5218..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project CloudPCRental" -PassThru -WindowStyle Hidden
    
    # Wait until port 5218 is active
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 1
        $conn = Get-NetTCPConnection -LocalPort 5218 -ErrorAction SilentlyContinue
        if ($conn) { break }
    }
}
Write-Host "[1/2] CloudPC web server is active on port 5218."

# 2. Run cloudflared in foreground of this daemon task
Write-Host "[2/2] Starting Cloudflare Tunnel..."
& ".\cloudflared.exe" tunnel --url http://localhost:5218 --protocol http2
