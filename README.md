# İŞKUR Otomasyonu

Teknoloji Fakültesi İŞKUR öğrenci programı için kayıt, aylık çalışma planı, izin ve devam takip sistemi.
.NET 8, EF Core (SQL Server), JWT.

Vize ve final teslim dosyaları (yönerge, araştırma, tasarım, veritabanı, uygulama, test ve dokümantasyon) [teslim](teslim) klasöründedir.

Ayrıntılar:

- [ARCHITECTURE.md](ARCHITECTURE.md) – katmanlar
- [docs/DATABASE.md](docs/DATABASE.md) – tablolar ve bütünlük kuralları
- [docs/API.md](docs/API.md) – uç noktalar ve roller
- [docs/KURULUM.md](docs/KURULUM.md) – kurulum, ayarlar, yedekleme ve geri yükleme
- [docs/KULLANIM.md](docs/KULLANIM.md) – rol bazlı kullanım
- [docs/TEST-SENARYOLARI.md](docs/TEST-SENARYOLARI.md) – testler ve elle kabul senaryoları
- [docs/GEREKSINIMLER.md](docs/GEREKSINIMLER.md) – gereksinim – modül – test eşleşmesi
- [frontend/README.md](frontend/README.md) – arayüz kurulumu ve yapısı
- [docs/NEDEN-BOYLE.md](docs/NEDEN-BOYLE.md) – tasarım kararlarının gerekçeleri

## Kurulum

.NET 8 SDK yeterli.

```bash
dotnet restore
dotnet run --project src/IskurAuto.API
```

Veritabanı (`iskurauto.db`) ilk çalıştırmada oluşur, migration'lar kendiliğinden uygulanır. Development ortamında Swagger ana sayfada açılır.

Üretimde en az bir ayar şart:

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "<en az 32 karakter>" --project src/IskurAuto.API
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@ornek.edu.tr" --project src/IskurAuto.API
dotnet user-secrets set "Bootstrap:AdminPassword" "<güçlü parola>" --project src/IskurAuto.API
```

Frontend adresi `Cors:AllowedOrigins` içine eklenir. Çalışma planı kuralları `WorkPlan` bölümünden değiştirilir
(haftalık gün sınırı, düzeltme son günü, günlük çalışma saati). İzin ve devamsızlık sınırları ise koddan değil, fakülte yönetimi tarafından
`PUT /api/settings/leave` ile değiştirilir. Yüklenen belgeler `Storage:DocumentsPath` klasöründe tutulur.

## Demo hesaplar

Yalnızca Development'ta oluşur, parola `Demo1234!`:

| Rol | E-posta |
|---|---|
| Fakülte Sekreterliği | sekreter@teknoloji.edu.tr |
| Bölüm Başkanı | baskan.bm@teknoloji.edu.tr |
| Danışman | danisman.bm@teknoloji.edu.tr |
| Öğrenci | ogrenci@teknoloji.edu.tr, ogrenci2@teknoloji.edu.tr |
| Onay bekleyen danışman (giriş yapamaz, fakülte aktif edince girer) | bekleyen.danisman@teknoloji.edu.tr |

Başvuru onayı: Fakülte Sekreterliği girişi, Tanımlar ve Hesaplar, Personel hesapları, ilgili satırda "Aktif yap".

## Testler

```bash
dotnet test
```

## Durum

| Aşama | İçerik | Durum |
|---|---|---|
| 1 | Kayıt, giriş, roller, öğrenci profili, katılım/birim/danışman geçmişi, denetim | Bitti |
| 2 | Aylık çalışma planı, takvim, tatil ve sınav dönemi | Bitti |
| 3 | İzin, devamsızlık, izin hakkı takibi ve uyarılar | Bitti |
| 4 | 30 saniyede yenilenen QR ile devam takibi | Bitti |
| 5a | Görev, danışman değerlendirmesi, devamsızlık bildirimi | Bitti |
| 5b | Raporlar, Excel/CSV, aylık döküm, ilişik kesme kararı | Bitti |
| 6 | Test senaryoları, kurulum/kullanım dokümanı, yedekleme | Bitti |
| 7a | Arayüz iskeleti: giriş, öğrenci kaydı, rol bazlı menü, ana sayfalar | Bitti |
| 7b | Öğrenci ekranları: plan takvimi, izin, karekod okutma, görevler, geçmiş, profil | Bitti |
| 7c | Personel ekranları: öğrenci listesi ve detayı, plan ve izin onayı, devamsızlık, karekod oturumu, görev, bildirim | Bitti |
| 7d | Raporlar, ilişik kesme, tanımlar ve hesaplar, denetim kayıtları | Sırada |

## Bilinen sınırlar

- Hesap kilitleme yok.
- Parola sıfırlama isteği için istek sınırı (rate limit) yok; bağlantı tek kullanımlıktır ve 30 dakika geçerlidir.
- E-posta alan adı kısıtı yoktur (gereksinim dokümanında zorunlu değil); kuruma ait adresle sınırlamak istenirse kayıt doğrulamasına eklenir.
- Token süresi dolana kadar eski rol geçerli kalır (varsayılan 120 dk).
- TC ve IBAN veritabanında düz saklanıyor, yalnızca gösterimde maskeleniyor. TC için NVİ doğrulaması yok.
- Öğrenci kaydı onaysız.
- İzin hakkı takvim yılı bazındadır ve izin iki yıla yayılamaz.
- Yüklenen belgeler diskte düz saklanır, şifrelenmez.
- Resmî tatiller ve sınav dönemleri elle girilir, otomatik kaynak yok.
- Haftalık gün sınırı plan bazında denetlenir; ay sınırındaki bir hafta iki plan arasında bölünürse toplam kontrol edilmez.
- QR oturumunun gizli anahtarı veritabanında düz saklanır (imza doğrulaması için gerekli); yanıtlarda hiçbir zaman dönmez.
- Karekod bir öğrenciden diğerine fotoğrafla iletilebilir; jeton yaklaşık 60 saniye geçerli olduğu ve her öğrenci oturum başına tek kayıt oluşturabildiği için etkisi sınırlıdır. Konum doğrulaması yok.
- Planlı ama devamı olmayan gün otomatik devamsızlık sayılmaz; karşılaştırmada "kayıt yok" olarak görünür, devamsızlığı fakülte yönetimi girer.
- Çalışma saati yalnızca onaylı plandaki günlerde, devam kaydı olan günler için hesaplanır; plan dışı güne yazılan kayıt saate eklenmez. Çalışılan her gün tam gündür (günlük 8 saat), yarım gün yoktur.
- Raporlar tek istekte tüm eşleşen öğrenciyi okur; birkaç yüz öğrenci için uygundur, çok büyük hacimde sayfalama gerekir.
- Excel dosyası basit bir tablodur (biçimlendirme, formül yok). CSV dosyası Türkçe Excel için noktalı virgülle ayrılır.
- Arama, SQL Server'ın varsayılan (büyük/küçük harfe duyarsız) harmanlamasıyla yapılır; farklı bir harmanlama seçilirse arama davranışı değişir.
