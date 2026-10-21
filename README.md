# İŞKUR Otomasyonu

Teknoloji Fakültesi İŞKUR öğrenci programı için kayıt, aylık çalışma planı, izin ve devam takip sistemi.
.NET 8, EF Core (SQLite), JWT.

Ayrıntılar: [ARCHITECTURE.md](ARCHITECTURE.md), [docs/DATABASE.md](docs/DATABASE.md), [docs/NEDEN-BOYLE.md](docs/NEDEN-BOYLE.md)

## Kurulum

.NET 8 SDK yeterli.

```bash
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef migrations add AddLeaves --project src/IskurAuto.Data --startup-project src/IskurAuto.API
dotnet run --project src/IskurAuto.API
```

Veritabanı (`iskurauto.db`) ilk çalıştırmada oluşur. Development ortamında Swagger ana sayfada açılır.

Üretimde en az bir ayar şart:

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "<en az 32 karakter>" --project src/IskurAuto.API
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@ornek.edu.tr" --project src/IskurAuto.API
dotnet user-secrets set "Bootstrap:AdminPassword" "<güçlü parola>" --project src/IskurAuto.API
```

Frontend adresi `Cors:AllowedOrigins` içine eklenir. Çalışma planı kuralları `WorkPlan` bölümünden değiştirilir
(haftalık gün sınırı, düzeltme son günü, tam gün saati). İzin ve devamsızlık sınırları ise koddan değil, fakülte yönetimi tarafından
`PUT /api/settings/leave` ile değiştirilir. Yüklenen belgeler `Storage:DocumentsPath` klasöründe tutulur.

## Demo hesaplar

Yalnızca Development'ta oluşur, parola `Demo1234!`:

| Rol | E-posta |
|---|---|
| Fakülte Sekreterliği | sekreter@teknoloji.edu.tr |
| Bölüm Başkanı | baskan.bm@teknoloji.edu.tr |
| Danışman | danisman.bm@teknoloji.edu.tr |
| Öğrenci | ogrenci@teknoloji.edu.tr, ogrenci2@teknoloji.edu.tr |

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
| 4 | 30 saniyede yenilenen QR ile devam takibi | Bitti (migration üretilecek) |
| 5 | Görev, değerlendirme, raporlar | |
| 6 | Test senaryoları, teknik dokümantasyon | |

## Bilinen sınırlar

- Hesap kilitleme yok.
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
- SQLite'ta Türkçe karakterli aramalar büyük/küçük harfe duyarlıdır.
