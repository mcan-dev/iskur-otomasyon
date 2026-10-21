# İŞKUR Otomasyonu – Mimari

Teknoloji Fakültesi kısmi zamanlı öğrenci (İŞKUR) takip sistemi. Gereksinimler resmi belgeye
(`1bb104e4-iskur.docx`) göre alınmıştır; bu dosya **kodun nasıl katmanlandığını ve neden** anlatır.

## 1. Katmanlar ve bağımlılık yönü

```
        ┌──────────────┐
        │     API      │  Controller, JWT, middleware, DI (composition root)
        └──┬────────┬──┘
           │        │
   ┌───────▼──┐  ┌──▼───────┐
   │ Service  │  │   Data   │  EF Core + SQLite, repository'ler, DbContext
   └───────┬──┘  └──┬───────┘
           │        │
        ┌──▼────────▼──┐
        │     Core     │  Entity, enum, exception, arayüzler (hiçbir şeye bağlı değil)
        └──────────────┘
```

| Katman | Sorumluluk | Neye bağlı |
|---|---|---|
| **Core** | Varlıklar (entity), enum'lar, domain exception'ları, repository/UoW/IClock **arayüzleri** | Hiçbir şeye |
| **Data** | `DbContext`, EF yapılandırmaları, repository **uygulamaları** | Core |
| **Service** | İş kuralları, yetki kapsamı, doğrulama, DTO, parola özeti | **Yalnızca Core** |
| **API** | HTTP, kimlik doğrulama (JWT), hata → HTTP eşlemesi, bileşenleri birbirine bağlama | Service + Data |
| **Tests** | Birim ve veritabanı destekli testler | Hepsi |

**Kritik kural:** Service katmanı EF Core'u hiç görmez. Veriye `IStudentRepository` gibi Core'da tanımlı
arayüzlerle erişir. Bu sayede (1) iş kuralları veritabanından bağımsız test edilir, (2) veritabanı
değişirse yalnızca Data katmanı etkilenir.

## 2. Bir isteğin yolu (örnek: `PUT /api/students/{id}/advisor`)

1. **Controller** (API): yalnızca `[Authorize(Roles=...)]`, DTO'yu alır, servisi çağırır. İş kuralı yok.
2. **Service**: rolü doğrular → öğrenciyi kapsam içinde yükler (başka bölümse *404*) → kuralları kontrol eder
   (danışman gerçekten Danışman mı, aynı bölümde mi, zaten atanmış mı) → eski atamayı kapatır, yenisini ekler →
   denetim kaydı yazar → hepsini **tek transaction**'da kaydeder.
3. **Repository** (Data): sorguyu EF ile çalıştırır.
4. **ExceptionHandlingMiddleware**: servisin fırlattığı tipli hatayı (`ConflictException` → 409 vb.) tek yerde HTTP yanıtına çevirir.

## 3. Roller ve yetki kapsamı (belge madde 2)

| Rol | Kapsam |
|---|---|
| `FacultyAdmin` (Dekanlık / Fakülte Sekreterliği) | Tüm öğrenciler, kullanıcı/bölüm/birim yönetimi, denetim kayıtları, ham kişisel veri (görüntüleme **loglanır**) |
| `DepartmentHead` (Bölüm Başkanı) | Yalnızca kendi bölümü; kişisel veriler maskeli; danışman atar |
| `Advisor` (Danışman) | Yalnızca kendisine atanmış öğrenciler; maskeli |
| `Student` (Öğrenci) | Yalnızca kendi kaydı |

Yetki iki kez kontrol edilir: Controller'da `[Authorize(Roles)]` (kaba), Service'te `RequireRole` + kapsam filtresi (ince).
Kapsam dışı kayıt için **403 değil 404** döner; böylece kaydın var olup olmadığı sızmaz.

## 4. Geçmişin korunması (belge madde 9)

* Hiçbir ilişkide cascade delete yok (`DeleteBehavior.Restrict`).
* Katılım / birim / danışman ataması **silinmez, `EndDate` ile kapatılır**.
* Öğrenci başına tek açık kayıt kuralı **veritabanında** filtreli benzersiz indeksle (`WHERE EndDate IS NULL`) zorlanır;
  uygulama kodu hata yapsa bile mükerrer açık kayıt oluşamaz.
* Denetim günlüğü (`AuditLog`) yalnızca eklenir; içinde kişisel veri tutulmaz.

## 5. Güvenlik özeti (belge madde 11)

* Parola: PBKDF2-HMAC-SHA256, 600.000 yineleme, rastgele salt, sabit-zamanlı karşılaştırma.
* Giriş: bilinmeyen e-posta ile yanlış parola aynı mesaj ve benzer süre (kullanıcı numaralandırma engeli).
* JWT: anahtar ≥ 32 karakter değilse veya yer tutucuysa uygulama **başlamaz**.
* CORS: yalnızca yapılandırmadaki origin'ler. HTTPS yönlendirme Development dışında.
* TC kimlik ve IBAN listelerde ve yetkisiz rollerde maskelenir (`123******50`).
* Doğrulama: TC kimlik algoritması, TR IBAN mod-97, telefon, parola politikası.

## 6. Aşamalar

| Aşama | İçerik | Durum |
|---|---|---|
| 1 | Veri modeli, kimlik doğrulama, roller, öğrenci kaydı/profili, geçmiş, denetim | **Tamamlandı** |
| 2 | Aylık çalışma planı (takvim, 1–3 gün/hafta, tatil/ders kuralları, yarım gün) | Sırada |
| 3 | İzin / devamsızlık (parametrik 10 gün limiti, uyarı) | |
| 4 | 30 sn dinamik QR ile devam | |
| 5 | Görev ve danışman değerlendirme, raporlar (Excel/CSV) | |
| 6 | Test senaryoları, kurulum ve teknik dokümantasyon | |
| Sonra | İŞKUR botu (Playwright, Maker-Checker) – `parked/` klasöründe bekliyor | |
