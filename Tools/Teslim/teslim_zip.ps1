<#
  Hafta 6–7 teslim paketi: BMU1421_<OgrenciNo>_H6-7.zip
  Föy: "Unity projesi (yalnız Assets, ProjectSettings, Packages) ve laboratuvar GDD'si PDF'i tek zip'te."

  Kullanım (çalışma alanının kökünden):
    powershell -ExecutionPolicy Bypass -File Tools\Teslim\teslim_zip.ps1 -OgrenciNo 123456789 -GddPdf "C:\yol\GDD.pdf"

  Zip, Teslim\ klasörüne yazılır. Var olan bir zip'in üzerine yazmaz (önce sen sil ya da başka ad ver).
  Proje dosyalarını değiştirmez; yalnız okur. Unity Editor açıkken çalıştırma (dosyalar yazılıyor olabilir).
#>
param(
    [Parameter(Mandatory = $true)][string]$OgrenciNo,
    [string]$GddPdf = ""
)
$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$proje = Join-Path $kok "NU.D.36_Unity"
$hedefKlasor = Join-Path $kok "Teslim"
$ad = "BMU1421_${OgrenciNo}_H6-7"
$zip = Join-Path $hedefKlasor "$ad.zip"

if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw "Unity çalışıyor: önce kapat." }
if (Test-Path $zip) { throw "$zip zaten var; üzerine yazılmadı." }
foreach ($k in "Assets", "ProjectSettings", "Packages") {
    if (-not (Test-Path (Join-Path $proje $k))) { throw "Eksik klasör: $k" }
}
if ($GddPdf -ne "" -and -not (Test-Path $GddPdf)) { throw "GDD PDF bulunamadı: $GddPdf" }

$gecici = Join-Path ([System.IO.Path]::GetTempPath()) $ad
if (Test-Path $gecici) { Remove-Item -Recurse -Force $gecici }
New-Item -ItemType Directory -Force (Join-Path $gecici "NU.D.36_Unity") | Out-Null
foreach ($k in "Assets", "ProjectSettings", "Packages") {
    Copy-Item -Recurse (Join-Path $proje $k) (Join-Path $gecici "NU.D.36_Unity\$k")
}
if ($GddPdf -ne "") { Copy-Item $GddPdf (Join-Path $gecici "$ad`_GDD.pdf") }

New-Item -ItemType Directory -Force $hedefKlasor | Out-Null
Compress-Archive -Path (Join-Path $gecici "*") -DestinationPath $zip
Remove-Item -Recurse -Force $gecici
$boy = [Math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Hazır: $zip ($boy MB)"
if ($GddPdf -eq "") { Write-Host "Not: GDD PDF eklenmedi (-GddPdf ile ekle)." }
