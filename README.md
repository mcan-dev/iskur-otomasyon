# İŞKUR Otomasyonu – Teknoloji Fakültesi

Kısmi zamanlı öğrenci (İŞKUR) kayıt, plan, izin ve devam takip sistemi. .NET 8, N-katmanlı mimari, EF Core + SQLite, JWT.

Mimari için: [ARCHITECTURE.md](ARCHITECTURE.md) · Veritabanı: [docs/DATABASE.md](docs/DATABASE.md) · Tasarım kararları ("neden böyle?"): [docs/NEDEN-BOYLE.md](docs/NEDEN-BOYLE.md)

## Gereksinimler
.NET 8 SDK. (Başka bir şey gerekmez; SQLite bir dosyadır.)

## Çalıştırma

```bash
dotnet restore
dotnet tool install --global dotnet-ef          # bir kez
dotnet ef migrations add InitialCreate --project src/IskurAuto.Data --startup-project src/IskurAuto.API
dotnet run --project src/IskurAuto.API
```

Swagger: `https://localhost:<port>/swagger` (yalnızca Development).

### Ayarlar
* **JWT anahtarı** (üretimde zorunlu, ≥ 32 karakter):
  `dotnet user-secrets set "JwtSettings:SecretKey" "<uzun-rastgele-değer>" --project src/IskurAuto.API`
* **İlk yönetici** (hiç FacultyAdmin yoksa oluşturulur): `Bootstrap:AdminEmail` ve `Bootstrap:AdminPassword` (aynı yöntemle).
* **CORS**: frontend adresini `Cors:AllowedOrigins` içine ekleyin.

### Demo hesaplar (yalnızca Development, parola `Demo1234!`)
`sekreter@` (FacultyAdmin), `baskan.bm@` (DepartmentHead), `danisman.bm@` (Advisor), `ogrenci@` ve `ogrenci2@` (Student) — hepsi `@teknoloji.edu.tr`.

## Testler
```bash
dotnet test
```
`Unit/` saf mantık testleri, `Services/` her teste ayrı bellek içi SQLite kurup gerçek repository + servisleri çalıştırır.

## Aşama 1'de olanlar
Giriş / öğrenci kaydı / parola değiştirme, 4 rol ve kapsamlı yetki, öğrenci profili (maskeleme), katılımı sonlandırma/yeniden başlatma,
birim ve danışman atama (geçmiş korunur), kullanıcı-bölüm-birim yönetimi, denetim günlüğü.

## Bilinen sınırlar (savunmada açıkça söylenmeli)
* Migration dosyası henüz üretilmedi (yukarıdaki komutla üretilir).
* Hesap kilitleme / brute-force koruması yok.
* Token içindeki rol, süre dolana kadar (varsayılan 120 dk) geçerlidir; pasifleştirilen kullanıcı girişi anında engellenmez, yalnızca yeni girişi.
* TC ve IBAN veritabanında şifreli değil (maskeleme yalnızca gösterimde); TC için NVİ doğrulaması yok, yalnızca algoritma kontrolü.
* Öğrenci kaydı anonim; onay akışı yok.
* Bölüm başına tek Bölüm Başkanı; danışman öğrencisiyle aynı bölümde olmalı.
* SQLite'ta Türkçe karakterli arama/sıralama büyük-küçük harf duyarsız değildir (ASCII/ikili karşılaştırma).
* Bu aşamada veritabanı destekli testler yazar tarafından çalıştırılmadı, yalnızca derleme ve saf mantık testleri doğrulandı; ilk `dotnet test` sonucunu paylaşın.
