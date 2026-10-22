# Kurulum ve işletim

## Gereksinimler

- .NET 8 SDK (geliştirme) veya .NET 8 ASP.NET Core Runtime (yalnızca çalıştırma)
- `dotnet-ef` aracı (yalnızca model değiştirip yeni migration üretirken)
- Başka bir veritabanı sunucusu gerekmez; veriler tek bir SQLite dosyasında tutulur

## Geliştirme ortamında çalıştırma

```bash
dotnet restore
dotnet run --project src/IskurAuto.API
```

Veritabanı ilk açılışta kendiliğinden oluşur (migration dosyaları depoda hazırdır, uygulama açılırken uygulanır).
Development ortamında Swagger ana sayfada açılır ve demo hesaplar yüklenir (listesi README'de).

Testler:

```bash
dotnet test
```

## Yapılandırma

Ayarlar `src/IskurAuto.API/appsettings.json` içindedir. Gizli değerler dosyaya yazılmaz.

| Anahtar | Varsayılan | Açıklama |
|---|---|---|
| `ConnectionStrings:IskurAutoDb` | `Data Source=iskurauto.db` | SQLite dosyası. Yol, uygulamanın çalıştığı klasöre göre çözülür. Sunucuda tam yol vermek daha güvenlidir. |
| `JwtSettings:SecretKey` | örnek değer | En az 32 karakter. Üretimde örnek değerle uygulama açılmaz. |
| `JwtSettings:Issuer` / `Audience` | `IskurAutoAPI` / `IskurAutoClients` | Token doğrulaması için. |
| `JwtSettings:ExpirationMinutes` | 120 | Token süresi. |
| `Cors:AllowedOrigins` | `localhost:3000`, `localhost:5173` | Arayüzün yayınlandığı adres(ler). |
| `PasswordReset:FrontendBaseUrl` | `http://localhost:5173` | Sıfırlama e-postasındaki bağlantının adresi (arayüzün yayınlandığı adres). |
| `PasswordReset:TokenMinutes` | 30 | Sıfırlama bağlantısının geçerlilik süresi. |
| `Smtp:Host`, `Port`, `UseSsl`, `User`, `Password`, `From` | boş | Parola sıfırlama e-postası için posta sunucusu. `Host` boşsa e-posta gönderilmez; Development'ta içeriği (bağlantı dahil) konsola yazılır. |
| `Storage:DocumentsPath` | `App_Data/documents` | İzin belgelerinin saklandığı klasör. Yoksa oluşturulur; dosyalar rastgele adla yazılır. |
| `WorkPlan:MinDaysPerWeek` / `MaxDaysPerWeek` | 1 / 3 | Haftalık çalışma günü sınırı. |
| `WorkPlan:EditDeadlineDay` | 5 | Plan düzeltmesi için ayın son günü. |
| `WorkPlan:FullDayHours` | 8 | Tam gün çalışma saati. |
| `Bootstrap:AdminEmail` / `AdminPassword` | boş | Sistemde hiç Fakülte Sekreterliği hesabı yoksa ilk yönetici bununla oluşturulur. |

İzin hakkı ve devamsızlık sınırları dosyada değil veritabanındadır; Fakülte Sekreterliği hesabıyla
`PUT /api/settings/leave` üzerinden değiştirilir. Değişiklik denetim kaydına yazılır.

## Üretim kurulumu

1. Yayınlama:

   ```bash
   dotnet publish src/IskurAuto.API -c Release -o /srv/iskur/app
   ```

2. Gizli değerleri ortam değişkeni ile verin (çift alt çizgi iç içe anahtarı gösterir):

   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   export JwtSettings__SecretKey="<en az 32 karakter, rastgele>"
   export Bootstrap__AdminEmail="admin@ornek.edu.tr"
   export Bootstrap__AdminPassword="<güçlü parola>"
   export ConnectionStrings__IskurAutoDb="Data Source=/srv/iskur/data/iskurauto.db"
   export Storage__DocumentsPath="/srv/iskur/data/documents"
   export Cors__AllowedOrigins__0="https://iskur.ornek.edu.tr"
   export PasswordReset__FrontendBaseUrl="https://iskur.ornek.edu.tr"
   export Smtp__Host="smtp.ornek.edu.tr"
   export Smtp__User="iskur@ornek.edu.tr"
   export Smtp__Password="<posta parolası>"
   export Smtp__From="iskur@ornek.edu.tr"
   ```

   Geliştirme makinesinde aynı değerler `dotnet user-secrets set "JwtSettings:SecretKey" "..." --project src/IskurAuto.API` ile verilir.

3. Uygulamayı çalıştırın (`dotnet IskurAuto.API.dll`). İlk açılışta veritabanı oluşur ve ilk yönetici hesabı yaratılır.
   Giriş yaptıktan sonra `Bootstrap__AdminPassword` değerini ortamdan kaldırın ve parolayı değiştirin.
4. Uygulamayı bir ters vekil (IIS, nginx vb.) arkasında HTTPS ile yayınlayın. Production'da HTTP, HTTPS'e yönlendirilir.
5. Veri klasörünü (`/srv/iskur/data`) uygulama kullanıcısından başkasına açmayın. Veritabanında TC kimlik ve IBAN bulunur.

Demo hesaplar ve Swagger yalnızca Development ortamında etkindir.

## Yeni sürüme geçiş

1. Yedek alın (aşağıya bakın).
2. Uygulamayı durdurun, yeni dosyaları kopyalayın, başlatın. Bekleyen migration'lar açılışta uygulanır.
3. Giriş yapıp kısa bir kontrol yapın.

Model değiştiren geliştirici, `docs/DATABASE.md` içindeki komutla yeni migration üretir ve dosyaları depoya ekler.

## Yedekleme ve geri yükleme

Yedeklenmesi gereken iki şey vardır:

- SQLite veritabanı dosyası (`iskurauto.db`)
- Belgeler klasörü (`Storage:DocumentsPath`)

JWT anahtarı yedeğe girmez. Kaybolursa yenisi üretilir; tek etkisi herkesin yeniden giriş yapmasıdır.
QR oturum anahtarları veritabanındadır, ayrıca saklanmaz.

### Yedek alma

Çalışan bir veritabanı dosyasını düz kopyalamak tutarsız yedek verebilir. Şunlardan biri kullanılır:

- Uygulamayı kısa süre durdurup iki klasörü kopyalamak, ya da
- `sqlite3` aracı varsa tutarlı kopya almak:

  ```bash
  sqlite3 iskurauto.db "VACUUM INTO 'yedek/iskurauto-2026-10-22.db'"
  ```

`scripts/yedekle.ps1` (Windows) veritabanı ve belgeler klasörünü tarihli bir klasöre kopyalar, eski yedekleri temizler:

```powershell
.\scripts\yedekle.ps1 -Veritabani C:\iskur\data\iskurauto.db -Belgeler C:\iskur\data\documents -Hedef D:\yedekler
```

Öneri: günlük yedek, son 30 günü sakla, ayda bir yedeği başka bir diske/sunucuya taşı. Ay sonu dökümleri
alındıktan sonra ayrıca bir yedek almak iyi olur.

### Geri yükleme

1. Uygulamayı durdurun.
2. Mevcut `iskurauto.db` ve belgeler klasörünü başka bir yere taşıyın (silmeyin).
3. Yedekteki `iskurauto.db` dosyasını ve belgeler klasörünü yerine koyun.
4. Uygulamayı başlatın. Yedek eski bir sürümden ise bekleyen migration'lar açılışta uygulanır.
5. Giriş yapın, bir öğrenci kaydı ve son denetim kayıtları ile verinin geldiğini kontrol edin.

Geri yüklemeyi gerçek verinin dışında, boş bir klasörde önceden denemek iyi olur.
