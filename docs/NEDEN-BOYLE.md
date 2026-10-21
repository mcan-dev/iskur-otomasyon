# Neden böyle yaptık? (Savunma hazırlığı)

Her başlık: **soru → cevap**. Jüri "neden?" diye sorduğunda cümleyi buradan alabilirsiniz.

### Neden N-katmanlı mimari?
Sorumlulukları ayırır: HTTP bilgisi Controller'da, kurallar Service'te, SQL Data'da. Bir katmanı değiştirmek (örn. SQLite → SQL Server)
diğerlerini bozmaz. Belgenin "katmanlı mimari" şartını da doğrudan karşılar.

### Service neden EF Core'u bilmiyor?
Bağımlılık tersine çevirme (SOLID-D). Service, Core'daki `IStudentRepository` arayüzüne bağımlı; somut sınıf Data'da. Böylece iş kuralları veritabansız da
anlaşılır ve test edilir.

### Neden Repository + UnitOfWork?
Repository sorguyu saklar, UnitOfWork "birden çok değişikliği tek seferde kaydet / hepsi olmazsa hiçbiri olmasın" garantisi verir
(örn. eski danışmanı kapat + yenisini ekle + denetim yaz = tek transaction).

### Neden DTO? Entity'yi neden doğrudan dönmüyoruz?
`PasswordHash` gibi alanlar yanlışlıkla dışarı çıkmasın; tablo değişse bile frontend'in gördüğü sözleşme sabit kalsın.

### Neden kayıtlar silinmiyor, `EndDate` ile kapatılıyor?
Belge madde 9: geçmiş kaybolmamalı. "Mehmet 2025'te hangi birimde, hangi danışmanla çalıştı?" sorusu yalnızca geçmiş tutulursa cevaplanır.

### Neden "tek açık kayıt" kuralı veritabanında da var?
Uygulama kodu bir gün hata yapsa bile veritabanı mükerrer açık kaydı reddeder (filtreli unique index). Kural iki yerde: kullanıcıya güzel mesaj için kodda,
bütünlük için veritabanında.

### Neden kapsam dışı kayıtta 403 değil 404?
403 "bu kayıt var ama sana yasak" demektir; 404 varlığını da gizler. Başka öğrencinin Id'sini deneyerek keşif yapılamaz.

### Neden PBKDF2 ve 600.000 yineleme?
Parola düz saklanmaz; yavaş, tuzlu özet saldırganın tahmin hızını düşürür. 600.000 OWASP'ın PBKDF2-SHA256 önerisidir. Biçim `v1.iter.salt.hash`
olduğundan sayı ileride artırılsa eski parolalar çalışmaya devam eder.

### Neden yanlış e-posta ile yanlış parolada aynı mesaj?
Saldırgan "bu e-posta kayıtlı mı?" bilgisini alamasın. Süre farkı da sahte özet doğrulamasıyla kapatıldı.

### Neden IClock?
`DateTime.Now` doğrudan kullanılırsa testler tarihe bağımlı olur. `IClock` ile testte tarih sabitlenir, "10 gün limiti" gibi kurallar her gün aynı sonucu verir.

### Neden hatalar exception ile, tek middleware'de?
Servis `ConflictException` fırlatır, HTTP bilmez. Middleware bunu 409'a çevirir. Böylece her controller'da try/catch tekrarı olmaz (DRY) ve tüm hatalar aynı biçimde döner.

### Neden SQLite?
Kurulumsuz, tek dosya; bitirme projesi ve jüri demosu için ideal. EF Core sayesinde üretimde SQL Server/PostgreSQL'e geçiş yalnızca Data katmanında yapılır.

### Neden İŞKUR botu ertelendi?
Belgenin zorunlu modülleri (kayıt, plan, izin, QR, rapor) botta yok. Önce zorunlular, sonra bot; kod `parked/` içinde saklı.

### Plan kuralları neden ayrı bir sınıfta?
`WorkPlanRules` yalnızca tarih listesi ve takvim bilgisiyle çalışır, veritabanını bilmez. Böylece kuralların her biri küçük bir testle denenir ve belge değişirse tek yerde güncellenir.

### Hafta neden perşembesinin düştüğü aya sayılıyor?
Ay sınırındaki yarım hafta (örneğin yalnızca 30 Kasım Pazartesi) iki ayın planında birden "en az 1 gün" şartı doğurmasın diye. ISO hafta mantığıyla aynıdır.

### Plan düzenleme süresi neden ayarda?
Belge "belirlenen süre içinde düzeltme" diyor ama süreyi vermiyor. `WorkPlan:EditDeadlineDay` ile kurum kendi süresini seçer.

### Plan onaylanınca neden öğrenci değiştiremiyor, sekreter değiştirebiliyor?
Onay, kararın bağlayıcı olması demek. Belge fakülte yönetimine değiştirme yetkisi veriyor; değişen plan yeniden "bekliyor" durumuna düşer ve değişiklik denetim kaydına yazılır.

### Tatiller ve sınav dönemleri neden elle giriliyor?
Dini bayram tarihleri her yıl değişir ve güvenilir bir otomatik kaynak yok. Fakülte yönetimi yıllık bir kez girer.
