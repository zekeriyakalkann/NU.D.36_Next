<#
  Hafta 8 teslim paketi: BMU1421_<OgrenciNo>_H8.zip
  Föy: "zip'te Assets, ProjectSettings, Packages ve .meta dosyaları; kullanılmayan asset'ler silinmiş; oynanış videosu
  Video/bolum4.mp4 (en çok 7 dk)."

  Proje birikimli (Lab 2–7 aynı projede), bu yüzden çalışma alanından hiçbir şey silinmez. Betik:
    1. Unity'ye Bölüm 4'ün ihtiyaç listesini yazdırır (AnaMenu + Bolum4_KursunDaglari bağımlılıkları, bütün scriptler,
       render ayarları, girdi eylemleri, VARLIKLAR.md; NU36.Editor.TeslimKopyasi.Listele);
    2. yalnız bunları .meta dosyalarıyla birlikte ayrı bir kopyaya taşır (+ Packages, ProjectSettings);
    3. kopyayı Unity'de açar: build listesini iki sahneye indirir, kopuk referans arar, build alır (TeslimKopyasi.Dogrula);
    4. Assets, Packages, ProjectSettings (ve verilirse Video/bolum4.mp4) ile zip'i Teslim\ klasörüne yazar.

  Kullanım (çalışma alanının kökünden, Unity Editor kapalıyken):
    powershell -ExecutionPolicy Bypass -File Tools\Teslim\teslim_h8.ps1 -OgrenciNo 123456789 -Video "C:\yol\bolum4.mp4"
  Seçenekler: -BuildYok (doğrulama build'ini atla, ~10 dk kazandırır), -KopyayiSakla (%TEMP%
u36_<no> çalışma kopyasını silme).
  Var olan bir zip'in üzerine yazmaz. Asıl projeyi değiştirmez (yalnız okur; Logs\lab8\ altına kayıt yazar).
#>
param(
    [Parameter(Mandatory = $true)][string]$OgrenciNo,
    [string]$Video = "",
    [switch]$BuildYok,
    [switch]$KopyayiSakla
)
$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$proje = Join-Path $kok "NU.D.36_Unity"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
$kayit = Join-Path $kok "Logs\lab8"
$ad = "BMU1421_${OgrenciNo}_H8"
$teslim = Join-Path $kok "Teslim"
$zip = Join-Path $teslim "$ad.zip"
# Short path: Unity unpacks the packages under Library\PackageCache; a long path breaks Windows' 260-character limit.
$calisma = Join-Path ([System.IO.Path]::GetTempPath()) "nu36_$OgrenciNo"
$kopya = Join-Path $calisma "P"

if (-not (Test-Path $unity)) { throw "Unity bulunamadı: $unity" }
if (Get-Process | Where-Object { $_.Path -like '*Editor\Unity.exe' }) { throw "Unity Editor açık: önce kapat." }
if (Test-Path $zip) { throw "$zip zaten var; üzerine yazılmadı (önce sil ya da başka ad ver)." }
if ($Video -ne "" -and -not (Test-Path $Video)) { throw "Video bulunamadı: $Video" }
if (Test-Path $calisma) { Remove-Item -Recurse -Force $calisma }
New-Item -ItemType Directory -Force $kayit, $kopya | Out-Null

function Unity([string]$projeYolu, [string]$log, [string[]]$ek) {
    $a = @('-batchmode', '-projectPath', "`"$projeYolu`"", '-logFile', "`"$log`"") + $ek
    $p = Start-Process $unity -Wait -NoNewWindow -PassThru -ArgumentList $a
    return $p.ExitCode
}

# 1. List
$liste = Join-Path $kayit "teslim_listesi.txt"
$taban = Join-Path $kayit "teslim_taban_guid.txt"
Write-Host "1/4 Liste yazılıyor..."
$k = Unity $proje (Join-Path $kayit "teslim_listele.log") @('-executeMethod', 'NU36.Editor.TeslimKopyasi.Listele', '-teslimListe', "`"$liste`"", '-teslimTaban', "`"$taban`"")
if ($k -ne 0 -or -not (Test-Path $liste)) { throw "Liste yazılamadı (Logs\lab8\teslim_listele.log)." }
$dosyalar = Get-Content $liste -Encoding UTF8 | Where-Object { $_ -ne "" }

# 2. Copy (files + their .meta + the .meta of every folder on the way)
Write-Host "2/4 $($dosyalar.Count) dosya kopyalanıyor..."
$klasorler = New-Object System.Collections.Generic.HashSet[string]
foreach ($d in $dosyalar) {
    $kaynak = Join-Path $proje $d
    $hedef = Join-Path $kopya $d
    New-Item -ItemType Directory -Force (Split-Path -Parent $hedef) | Out-Null
    Copy-Item -LiteralPath $kaynak -Destination $hedef
    if (Test-Path -LiteralPath "$kaynak.meta") { Copy-Item -LiteralPath "$kaynak.meta" -Destination "$hedef.meta" }
    $u = Split-Path -Parent $d
    while ($u -and $u -ne "Assets") { [void]$klasorler.Add($u); $u = Split-Path -Parent $u }
}
foreach ($u in $klasorler) {
    $m = Join-Path $proje "$u.meta"
    if (Test-Path -LiteralPath $m) { Copy-Item -LiteralPath $m -Destination (Join-Path $kopya "$u.meta") }
}
foreach ($k2 in "Packages", "ProjectSettings") { Copy-Item -Recurse (Join-Path $proje $k2) (Join-Path $kopya $k2) }

# 3. Verify in the copy
Write-Host "3/4 Kopya Unity'de doğrulanıyor (derleme, kopuk referans$(if (-not $BuildYok) {', build'}))..."
$ek = @('-executeMethod', 'NU36.Editor.TeslimKopyasi.Dogrula', '-teslimTaban', "`"$taban`"")
if (-not $BuildYok) { $ek += @('-teslimBuild', "`"$(Join-Path $calisma 'Build\NU.D.36.exe')`"") }
$k = Unity $kopya (Join-Path $kayit "teslim_dogrula.log") $ek
$rapor = Join-Path $kopya "teslim_dogrulama.txt"
if (Test-Path $rapor) { Copy-Item $rapor (Join-Path $kayit "teslim_dogrulama.txt") -Force; Get-Content $rapor -Encoding UTF8 | ForEach-Object { Write-Host "    $_" } }
if ($k -ne 0) { throw "Kopya doğrulanamadı (Logs\lab8\teslim_dogrula.log, teslim_dogrulama.txt). Zip yazılmadı." }

# 4. Zip
Write-Host "4/4 Zip yazılıyor..."
$paket = Join-Path $calisma "paket"
New-Item -ItemType Directory -Force $paket | Out-Null
foreach ($k3 in "Assets", "Packages", "ProjectSettings") { Copy-Item -Recurse (Join-Path $kopya $k3) (Join-Path $paket $k3) }
if ($Video -ne "") {
    New-Item -ItemType Directory -Force (Join-Path $paket "Video") | Out-Null
    Copy-Item $Video (Join-Path $paket "Video\bolum4.mp4")
}
New-Item -ItemType Directory -Force $teslim | Out-Null
Compress-Archive -Path (Join-Path $paket "*") -DestinationPath $zip
if (-not $KopyayiSakla) { Remove-Item -Recurse -Force $calisma }
$boy = [Math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Hazır: $zip ($boy MB)"
if ($Video -eq "") { Write-Host "Not: Video eklenmedi. Videoyu kaydedince zip'i silip -Video ile yeniden çalıştır." }
