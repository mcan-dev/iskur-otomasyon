# Kurulum ve işletim

## Gereksinimler

- .NET 8 SDK (geliştirme) veya .NET 8 ASP.NET Core Runtime (yalnızca çalıştırma)
- `dotnet-ef` aracı (yalnızca model değiştirip yeni migration üretirken)
- Microsoft SQL Server (geliştirmede Visual Studio ile gelen LocalDB yeterlidir; üretimde SQL Server 2019 veya üzeri / Express)
- Node.js 18+ (yalnızca arayüzü geliştirirken)

## Geliştirme ortamında çalıştırma

```bash
dotnet restore
dotnet run --project src/IskurAuto.API
```

Veritabanı (`IskurAuto`) ve tablolar ilk açılışta kendiliğinden oluşur; LocalDB'nin çalışıyor olması yeterlidir (`sqllocaldb start MSSQLLocalDB`).
Development ortamında Swagger ana sayfada açılır ve demo hesaplar yüklenir (listesi README'de).

Testler:

```bash
dotnet test
```

## Yapılandırma

Ayarlar `src/IskurAuto.API/appsettings.json` içindedir. Gizli değerler dosyaya yazılmaz.

| Anahtar | Varsayılan | Açıklama |
|---|---|---|
| `ConnectionStrings:IskurAutoDb` | `Server=(localdb)\MSSQLLocalDB;Database=IskurAuto;Trusted_Connection=True;TrustServerCertificate=True` | SQL Server bağlantısı. Üretimde kullanıcı adı/parola ya da Windows kimlik doğrulaması ile sunucu adresi verilir. |
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
| `WorkPlan:DailyHours` | 8 | Günlük çalışma süresi (saat). Çalışılan her gün tam gündür, yarım gün yoktur. |
| `Bootstrap:AdminEmail` / `AdminPassword` | boş | Sistemde hiç Fakülte Sekreterliği hesabı yoksa ilk yönetici bununla oluşturulur. |

Genel izin hakkı ve uyarı eşiği dosyada değil veritabanındadır; Fakülte Sekreterliği hesabıyla
`PUT /api/settings/leave` üzerinden değiştirilir. Değişiklik denetim kaydına yazılır.

## Demo verisini sıfırlama

Şema değiştiğinde ya da demo verisi bozulduğunda veritabanı silinip yeniden kurulabilir (yalnızca Development):

```powershell
.\scripts\sifirla.ps1
# başka bir SQL Server için: .\scripts\sifirla.ps1 -Sunucu "DESKTOP-B3EQC6H"
```

Betik veritabanını siler, API'yi başlatır ve her rolün her sayfasını dolduran demo kayıtlarını yeniden yazar.

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
   export ConnectionStrings__IskurAutoDb="Server=sql.ornek.edu.tr;Database=IskurAuto;User Id=iskur_app;Password=<parola>;Encrypt=True;TrustServerCertificate=False"
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
5. Veri klasörünü (`/srv/iskur/data`) uygulama kullanıcısından başkasına açmayın. Veritabanında TC kimlik numarası bulunur.

Demo hesaplar ve Swagger yalnızca Development ortamında etkindir.

## Yeni sürüme geçiş

1. Yedek alın (aşağıya bakın).
2. Uygulamayı durdurun, yeni dosyaları kopyalayın, başlatın. Migration kullanılıyorsa bekleyen migration'lar açılışta uygulanır.
3. Giriş yapıp kısa bir kontrol yapın.

Model değiştiren geliştirici, `docs/DATABASE.md` içindeki komutla yeni migration üretir ve dosyaları depoya ekler.

## Yedekleme ve geri yükleme

Yedeklenmesi gereken iki şey vardır:

- SQL Server veritabanı (`IskurAuto`)
- Belgeler klasörü (`Storage:DocumentsPath`)

JWT anahtarı yedeğe girmez. Kaybolursa yenisi üretilir; tek etkisi herkesin yeniden giriş yapmasıdır.
QR oturum anahtarları veritabanındadır, ayrıca saklanmaz.

### Yedek alma

Veritabanı, uygulama çalışırken de tutarlı yedeklenebilir:

```sql
BACKUP DATABASE [IskurAuto] TO DISK = N'D:\yedekler\IskurAuto.bak' WITH INIT, CHECKSUM;
```

`scripts/yedekle.ps1` (Windows, `sqlcmd` gerekir) veritabanı yedeğini ve belgeler klasörünü tarihli bir klasöre alır, eski yedekleri temizler:

```powershell
.\scripts\yedekle.ps1 -Sunucu "(localdb)\MSSQLLocalDB" -Veritabani IskurAuto -Belgeler C:\iskur\data\documents -Hedef D:\yedekler
```

Öneri: günlük yedek, son 30 günü sakla, ayda bir yedeği başka bir diske/sunucuya taşı. Ay sonu dökümleri
alındıktan sonra ayrıca bir yedek almak iyi olur.

### Geri yükleme

1. Uygulamayı durdurun.
2. Mevcut belgeler klasörünü başka bir yere taşıyın (silmeyin).
3. Veritabanını yedekten geri yükleyin ve belgeler klasörünü yerine koyun:

   ```sql
   RESTORE DATABASE [IskurAuto] FROM DISK = N'D:\yedekler\IskurAuto.bak' WITH REPLACE;
   ```
4. Uygulamayı başlatın. Yedek eski bir sürümden ise bekleyen migration'lar açılışta uygulanır.
5. Giriş yapın, bir öğrenci kaydı ve son denetim kayıtları ile verinin geldiğini kontrol edin.

Geri yüklemeyi gerçek verinin dışında, boş bir klasörde önceden denemek iyi olur.
