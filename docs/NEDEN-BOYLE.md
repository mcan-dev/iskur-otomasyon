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

### İzin sınırı neden veritabanındaki ayarda?
Belge 10 günlük sınırın değişebileceğini söylüyor. Sınır kodda sabit olsaydı her değişiklik yeni sürüm demekti; ayar tablosunda fakülte yönetimi kendisi günceller ve değişiklik denetim kaydına düşer.

### Sınırı aşan talep neden engellenmiyor?
Belge "uyarı üretilsin, nihai idari karar kendiliğinden verilmesin" diyor. Sistem uyarır ve ekranlarda belirgin gösterir, kararı fakülte yönetimi verir.

### İzin günleri neden plana göre sayılıyor?
Öğrenci haftada en fazla 3 gün çalıştığı için gelemeyeceği gün yalnızca planlı günlerdir. Her hafta içi günü saymak izin hakkını olduğundan hızlı tüketirdi. Plan henüz yoksa hafta içi günler sayılır.

### Devamsızlık kaydı neden silinmiyor?
Yanlış girilen kayıt gerekçe ve işlemi yapan kullanıcıyla geri alınır, böylece geçmiş izlenebilir. Aynı gün için geri alınan kaydın ardından yenisi girilebilir (filtreli unique index).

### Sağlık raporu belgesi neden denetimli?
Belge sağlık verisi içerebilir. Öğrencinin kendisi dışında biri açtığında denetim kaydı düşer; dosya adı rastgele üretilir, yol gezintisi engellenir, yalnızca PDF/JPG/PNG ve en fazla 5 MB kabul edilir.

### Karekod neden veritabanında saklanmıyor?
Her 30 saniyede yeni bir kayıt üretmek hem gereksiz yazma hem de temizlik işi olurdu. Jeton, oturum numarası ve zaman penceresinden gizli anahtarla imzalanarak hesaplanır; sunucu aynı hesabı yapıp karşılaştırır. Zaman ilerledikçe eski jeton kendiliğinden geçersizleşir.

### Neden bir önceki pencere de kabul ediliyor?
Öğrenci karekodu okutup göndermeye çalışırken pencere değişebilir. Tolerans olmasa geçerli bir okutma haksız yere reddedilirdi. Bedeli, jetonun en fazla yaklaşık 60 saniye geçerli olmasıdır.

### Karekodun paylaşılması nasıl engelleniyor?
Tamamen engellenemez: fotoğrafı başkasına iletilebilir. Önlemler: jeton hemen eskir, öğrenci kendi hesabıyla giriş yapmış olmalıdır, oturum başına tek kayıt vardır ve o gün için onaylı planı bulunmayan öğrenci kayıt oluşturamaz. Reddedilen denemeler denetim kaydına düşer.

### Okutma neden onaylı plana bağlı?
Belge, devamın çalışma planı ve izinle ilişkilendirilmesini ve planlanan ile gerçekleşenin karşılaştırılmasını istiyor. Planı olmayan ya da o gün izinli bir öğrencinin kaydı, karşılaştırmayı anlamsız kılardı.

### Oturumu kim açıyor?
Belge "yetkili kullanıcı" diyor. Fakülte yönetimi tüm fakülte için, bölüm başkanı yalnızca kendi bölümü için açabilir. Danışman ve öğrenci açamaz.

### Neden otomatik devamsızlık yazılmıyor?
Devamsızlık idari bir karardır ve izin hakkını etkiler. Planlı günde kayıt yoksa sistem bunu karşılaştırmada "kayıt yok" olarak gösterir; devamsızlık kaydını fakülte yönetimi girer.

### Hatalı kayıt neden silinmiyor?
Belge düzeltme nedeni ve işlemi yapanın saklanmasını istiyor. Kayıt iptal edilir (`VoidedAt`, kim, neden), satır kalır. İptalden sonra aynı oturum için yeni kayıt oluşabilir.

### Değerlendirmeler neden ayrı bir tabloda?
Belge önceki değerlendirmelerin ve değişikliklerin korunmasını istiyor. Görev satırına tek bir "not" alanı koysaydık her güncelleme öncekini ezerdi. Her not ve durum değişikliği ayrı, eklenen bir satırdır; mevcut durum görevde, geçmiş değerlendirmelerde durur.

### Görev başlığı ve tarihleri neden değişmiyor?
Değerlendirme geçmişi o görevin belirli bir tanımına bağlı. Tanım sonradan değişirse eski notlar yanlış bağlama kalırdı. Yanlış atanan görev için yeni görev açılır.

### Kim değerlendirebiliyor?
Belge danışman öğretim elemanını söylüyor. Bu yüzden yalnızca öğrencinin şu anki danışmanı not ekler ve durum değiştirir. Bölüm başkanı ve fakülte yönetimi görüntüler. Danışman değişirse eski notlar kalır.

### Öğrenci danışman notlarını görüyor mu?
Evet. Görevin ne durumda olduğunu ve nedenini bilmesi gerekir. Notları öğrenciden gizli tutmak istenirse tek bir alanla ayrılabilir; belge bunu belirtmiyor.

### Devamsızlık bildirimi neden devamsızlık kaydından ayrı?
Danışmanın bildirimi bir ihbar, devamsızlık kaydı ise izin hakkını etkileyen idari karardır. Bildirim bölüm başkanını bilgilendirir; kaydı fakülte yönetimi kendi incelemesiyle girer.

### Raporlar neden hep aynı tablo biçiminde?
Belge her raporun hem ekranda hem Excel/CSV olarak, aynı filtrelerle alınmasını istiyor. Her rapora ayrı sınıf yazmak yerine hepsi başlık, sütun ve satırdan oluşan tek bir biçimde döner; ekran bunu çizer, dosya yazıcısı aynı tablodan dosya üretir. İki çıktı birbirinden ayrılamaz.

### Raporlar neden veritabanında saklanmıyor?
Plan, devam, izin ve devamsızlık kayıtları zaten saklı ve silinmiyor. Rapor bunlardan her seferinde hesaplandığı için geçmiş bir ay sorgulandığında da o günkü kayıtlarla doğru sonuç verir ve ayrı bir tabloyla senkron tutma derdi olmaz.

### Çalışma saati nasıl hesaplanıyor?
Öğrencinin onaylı planındaki günlerden devam kaydı olanlar sayılır, yarım gün 0,5. Sonuç tam gün saatiyle çarpılır (ayarda, varsayılan 8). Plan dışı bir güne elle yazılan kayıt saate eklenmez: belge planlanan ile gerçekleşenin karşılaştırılmasını istiyor ve plansız çalışma bu karşılaştırmayı bozardı.

### Excel dosyası neden kütüphanesiz yazılıyor?
Yalnızca başlıklı bir tablo gerekiyor. Dış kütüphane eklemek bağımlılık, lisans ve sürüm yükü getirirdi. .xlsx zaten bir zip içindeki küçük XML dosyalarıdır; yazıcı kısa ve testle doğrulanıyor. Biçimlendirme veya formül gerekirse o noktada kütüphaneye geçilebilir.

### CSV neden noktalı virgülle ayrılıyor?
Türkçe bölgesel ayarlı Excel, virgülü ondalık ayırıcı kabul eder ve alanları noktalı virgülden ayırır. Dosya BOM'lu UTF-8 olduğu için Türkçe karakterler bozulmaz.

### Dışa aktarılan dosyada formül enjeksiyonu
Bir ad "=..." ile başlarsa Excel bunu formül sayabilir. `= + - @` ile başlayan metinlerin başına `'` konur; negatif sayılar bundan etkilenmez.

### Aylık döküm neden yalnızca fakülte yönetimine açık?
Belge dökümde T.C. kimlik numarası ve IBAN istiyor. Bu iki bilgi maskesiz yalnızca fakülte yönetimine verilir. Dökümün görüntülenmesi ve her dışa aktarma denetim kaydına yazılır.

### İlişik kesme listesi kararı vermiyor
Belge nihai kararın otomatik verilmemesini istiyor. Liste, izin ya da devamsızlık sınırını aşan aktif öğrencileri inceleme için gösterir. Kararı fakülte yönetimi gerekçesiyle kaydeder; kayıt o günkü izin ve devamsızlık sayılarını içerir ve silinmez.

### Karar kaydı neden öğrenciyi de pasif yapıyor?
Karar ile durumun ayrı ayrı güncellenmesi tutarsızlık yaratırdı (karar var ama öğrenci aktif görünür). Kayıt ve katılımın sonlandırılması tek işlemde yapılır; biri başarısız olursa ikisi de geri alınır. Öğrencinin geçmişi silinmez, yanlış karar için öğrenci mevcut "programa yeniden al" işlemiyle geri alınır.
