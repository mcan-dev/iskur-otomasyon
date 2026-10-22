<#
  SQL Server veritabanının yedeğini (.bak) ve belgeler klasörünün kopyasını tarihli bir klasöre alır.
  sqlcmd aracı gerekir (SQL Server ile birlikte ya da ayrıca kurulur).
  Yedek dosyasını SQL Server hizmetinin yazabildiği bir klasöre almak gerekir; Hedef klasörün
  hizmet hesabı için yazma izni olmalıdır.

  Örnek:
    .\yedekle.ps1 -Sunucu "(localdb)\MSSQLLocalDB" -Veritabani IskurAuto -Belgeler C:\iskur\data\documents -Hedef D:\yedekler
#>
param(
    [string] $Sunucu = '(localdb)\MSSQLLocalDB',
    [string] $Veritabani = 'IskurAuto',
    [Parameter(Mandatory = $true)] [string] $Belgeler,
    [Parameter(Mandatory = $true)] [string] $Hedef,
    [int] $SaklanacakGun = 30
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'sqlcmd bulunamadı.' }

$klasor = Join-Path $Hedef (Get-Date -Format 'yyyy-MM-dd_HHmm')
New-Item -ItemType Directory -Path $klasor -Force | Out-Null

$bak = Join-Path $klasor "$Veritabani.bak"
$sorgu = "BACKUP DATABASE [$Veritabani] TO DISK = N'$bak' WITH INIT, CHECKSUM"
& sqlcmd -S $Sunucu -E -b -Q $sorgu
if ($LASTEXITCODE -ne 0) { throw 'Veritabanı yedeği alınamadı.' }

if (Test-Path $Belgeler) {
    Copy-Item $Belgeler (Join-Path $klasor 'documents') -Recurse
}

# Eski yedekleri temizle
Get-ChildItem $Hedef -Directory |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$SaklanacakGun) } |
    Remove-Item -Recurse -Force

Write-Host "Yedek alındı: $klasor"
