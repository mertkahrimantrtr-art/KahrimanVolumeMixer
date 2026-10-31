# KAHRIMAN VOLUME MIXER — yerel derleme betiği
# Kullanım (PowerShell):  .\build.ps1
# Gereksinim: .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '==> Testler çalıştırılıyor' -ForegroundColor Cyan
dotnet test KahrimanVolumeMixer.sln -c Release

Write-Host '==> Tek EXE yayımlanıyor' -ForegroundColor Cyan
dotnet publish src/KahrimanVolumeMixer/KahrimanVolumeMixer.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=embedded `
    -o publish

$exe = Join-Path $PSScriptRoot 'publish\KahrimanVolumeMixer.exe'
if (-not (Test-Path $exe)) { throw 'EXE üretilemedi.' }
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "==> Hazır: $exe ($sizeMb MB)" -ForegroundColor Green
