# 3 – Veritabani

Veriler **Microsoft SQL Server** üzerinde ilişkisel olarak tutulur. Şema elle SQL yazılarak değil, uygulamadaki
EF Core modelinden (Code-First) oluşur; model sınıfları `src/IskurAuto.Core/Entities`, tablo ayarları
`src/IskurAuto.Data/Configurations` klasöründedir.

| Dosya | İçerik |
|---|---|
| [01_er_diyagrami.md](01_er_diyagrami.md) | ER diyagramı, ilişki özeti, benzersizlik ve bütünlük kuralları |
| [04_tablo_sozlugu.md](04_tablo_sozlugu.md) | 23 tablonun alanları, türleri, anahtarları ve indeksleri |

## Veritabanını kurma

Visual Studio ile gelen LocalDB yeterlidir. API'yi çalıştırmak (`dotnet run --project src/IskurAuto.API`) `IskurAuto`
veritabanını ve tüm tabloları oluşturur. Bağlantı cümlesi `src/IskurAuto.API/appsettings.json` içindedir:

```
Server=(localdb)\MSSQLLocalDB;Database=IskurAuto;Trusted_Connection=True;TrustServerCertificate=True
```

İsteğe bağlı olarak EF Core migration üretilebilir (komutlar [docs/DATABASE.md](../../docs/DATABASE.md) içindedir).

## Örnek test verisi

Development ortamında, veritabanı boşken uygulama ilk açılışta örnek veriyi kendisi yükler
(`src/IskurAuto.API/Infrastructure/DatabaseSeeder.cs`). Tüm hesapların parolası `Demo1234!` değeridir
(yalnızca deneme içindir).

| E-posta (`@teknoloji.edu.tr`) | Rol |
|---|---|
| sekreter@ | Fakülte Sekreterliği / Dekanlık |
| baskan.bm@ | Bölüm Başkanı (Bilgisayar Müh.) |
| danisman.bm@, danisman2.bm@ | Danışman (Bilgisayar Müh.) |
| danisman.ym@ | Danışman (Yazılım Müh.) |
| ogrenci@, ogrenci2@, burak.sahin@, selin.celik@ | Öğrenci |

Örnek veri şunları gösterecek biçimdedir:

- **Aynı öğrencinin farklı ay ve yılları:** Mehmet Öztürk için Eylül 2025, Ekim 2025, Eylül 2026 ve Ekim 2026 planları ayrı satırlardır.
- **Geçmişin korunması:** Mehmet'in birimi Kütüphane'den Bilgisayar Laboratuvarı'na, danışmanı Hakan Polat'tan Zeynep Arslan'a geçmiştir; eski satırların bitiş tarihi yazılıdır.
- **Plan durumları:** Onaylanmış, onay bekleyen ve gerekçeyle reddedilmiş plan birer örnekle vardır.
- **İzin:** Günlük, saatlik (3,75 saat = 0,5 gün hak düşer; saat, günlük süreye bölünür) ve onay bekleyen sağlık izni.
- **Devam, görev, bildirim:** Karekodla devam kayıtları, değerlendirmesi olan görev, açık görev, devamsızlık bildirimi.
- **Programdan ayrılan öğrenci:** Kerem Aydın'ın katılımı sona ermiş, ilişik kesme kararı kayıtlıdır; geçmiş kayıtları durur.
- **Filtre denemesi:** Üç bölümde öğrenci vardır; bölüm başkanı yalnızca kendi bölümünü, danışman yalnızca kendi öğrencilerini görür.

## Yedekleme

Veritabanı yedeği `BACKUP DATABASE` ile alınır; ayrıntı ve geri yükleme adımları
[docs/KURULUM.md](../../docs/KURULUM.md) dosyasındadır. `scripts/yedekle.ps1` yedeği ve belgeler klasörünü
tarihli klasöre alır.
