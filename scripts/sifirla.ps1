<#
  Geliştirme veritabanını siler ve API'yi başlatarak demo verisiyle yeniden kurar.
  Yalnızca Development ortamında çalışır; üretim veritabanına dokunmaz.

  Varsayılan olarak uygulamanın kendi bağlantısı (LocalDB, IskurAuto) kullanılır.
  Başka bir SQL Server için -Sunucu verin.

  Örnekler:
    .\scripts\sifirla.ps1
    .\scripts\sifirla.ps1 -Sunucu "DESKTOP-B3EQC6H"
#>
param(
    [string] $Sunucu = '',
    [string] $Veritabani = 'IskurAuto'
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Development__ResetDatabase = 'true'
if ($Sunucu) {
    $env:ConnectionStrings__IskurAutoDb = "Server=$Sunucu;Database=$Veritabani;Trusted_Connection=True;TrustServerCertificate=True"
}

try {
    Write-Host 'Veritabanı silinip demo verisiyle yeniden kurulacak. Hazır olunca API açılır; durdurmak için Ctrl+C.' -ForegroundColor Yellow
    dotnet run --project src/IskurAuto.API
}
finally {
    Remove-Item Env:\Development__ResetDatabase -ErrorAction SilentlyContinue
}
