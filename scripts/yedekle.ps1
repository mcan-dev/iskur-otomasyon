<#
  Veritabanı dosyasını ve belgeler klasörünü tarihli bir klasöre kopyalar.
  Tutarlı yedek için uygulama durdurulmuş olmalı ya da sqlite3 ile VACUUM INTO kullanılmalı.

  Örnek:
    .\yedekle.ps1 -Veritabani C:\iskur\data\iskurauto.db -Belgeler C:\iskur\data\documents -Hedef D:\yedekler
#>
param(
    [Parameter(Mandatory = $true)] [string] $Veritabani,
    [Parameter(Mandatory = $true)] [string] $Belgeler,
    [Parameter(Mandatory = $true)] [string] $Hedef,
    [int] $SaklanacakGun = 30
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Veritabani)) { throw "Veritabanı bulunamadı: $Veritabani" }

$klasor = Join-Path $Hedef (Get-Date -Format 'yyyy-MM-dd_HHmm')
New-Item -ItemType Directory -Path $klasor -Force | Out-Null

$sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
$dbHedef = Join-Path $klasor 'iskurauto.db'
if ($sqlite) {
    & sqlite3 $Veritabani "VACUUM INTO '$dbHedef'"
    if ($LASTEXITCODE -ne 0) { throw 'sqlite3 yedeği alınamadı.' }
}
else {
    Copy-Item $Veritabani $dbHedef
    Write-Warning 'sqlite3 bulunamadı, dosya düz kopyalandı. Uygulama çalışıyorsa yedek tutarsız olabilir.'
}

if (Test-Path $Belgeler) {
    Copy-Item $Belgeler (Join-Path $klasor 'documents') -Recurse
}

# Eski yedekleri temizle
Get-ChildItem $Hedef -Directory |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$SaklanacakGun) } |
    Remove-Item -Recurse -Force

Write-Host "Yedek alındı: $klasor"
