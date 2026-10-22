# 4 – Uygulama

Kaynak kod bu klasörde değil, depo kökündeki klasörlerdedir (çözüm ve proje yolları değişmesin diye):

| Klasör | İçerik |
|---|---|
| `src/IskurAuto.Core` | Varlıklar, enum'lar, arayüzler, kural modelleri (hiçbir katmana bağlı değil) |
| `src/IskurAuto.Data` | EF Core, SQL Server, repository'ler, tablo ayarları |
| `src/IskurAuto.Service` | İş kuralları: plan, izin, devam, rapor, görev, kimlik doğrulama |
| `src/IskurAuto.API` | Controller'lar, JWT, hata yönetimi, başlangıç verisi |
| `frontend` | React arayüzü |
| `tests` | Otomatik testler |
| `scripts` | Yedekleme betiği |

Katman yapısı ve nedenleri: [ARCHITECTURE.md](../../ARCHITECTURE.md), [docs/NEDEN-BOYLE.md](../../docs/NEDEN-BOYLE.md).

## Gereksinimler

- .NET 8 SDK
- Node.js 18 veya üzeri
- SQL Server (Visual Studio ile gelen LocalDB yeterlidir)

## Çalıştırma

Arka yüz (ilk açılışta veritabanını kurar, Development ortamında demo hesapları yükler):

```powershell
dotnet restore
dotnet run --project src/IskurAuto.API
```

API `http://localhost:5231` adresinde açılır; Swagger ana sayfadadır.

Arayüz (ayrı bir terminalde):

```powershell
cd frontend
npm install
npm run dev
```

Arayüz `http://localhost:5173` adresinde açılır ve `/api` isteklerini API'ye yönlendirir.

Visual Studio'dan çalıştırırken başlangıç projesi olarak `IskurAuto.API` seçilmelidir.

## Demo hesaplar

Parola hepsi için `Demo1234!` (yalnızca Development ortamı, boş veritabanında):

| E-posta | Rol |
|---|---|
| sekreter@teknoloji.edu.tr | Fakülte Sekreterliği / Dekanlık |
| baskan.bm@teknoloji.edu.tr | Bölüm Başkanı |
| danisman.bm@teknoloji.edu.tr | Danışman |
| ogrenci@teknoloji.edu.tr | Öğrenci |
| ogrenci2@teknoloji.edu.tr | Öğrenci (Yazılım Müh.) |

Örnek veri (çok aylı planlar, izin, devam, görev, ilişik kesme) uygulama ilk açıldığında kendiliğinden yüklenir; ayrıntı [3_Veritabani/README.md](../3_Veritabani/README.md).
Daha fazla demo hesap: burak.sahin@, selin.celik@ (öğrenci), danisman.ym@, danisman2.bm@ (danışman) – hepsi `@teknoloji.edu.tr`.
