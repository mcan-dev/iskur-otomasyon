# Kullanım kılavuzu

Sistemde dört rol vardır. Her kullanıcı yalnızca kendi rolünün işlemlerini yapabilir; kapsam dışı kayıtlar hiç görünmez.
Uç noktaların listesi için [API.md](API.md) dosyasına bakın.

## Ortak işlemler

- Dört rolün her birinin kendi giriş sayfası vardır (`/giris` sayfasından seçilir): Öğrenci, Danışman Öğretim Elemanı, Bölüm Başkanı, Fakülte Sekreterliği / Dekanlık. Hesap, yalnızca kendi rolünün giriş sayfasından girebilir.
- Giriş e-posta ve parola iledir. Parola, giriş yapıldıktan sonra değiştirilebilir; unutulursa e-postayla sıfırlanır.
- Hiçbir kayıt silinmez. Yanlış girilen kayıt (devamsızlık, devam, karar vb.) gerekçeyle geri alınır veya iptal edilir; geçmiş korunur.
- Önemli işlemler denetim kaydına yazılır. Kayıtlarda kişisel veri tutulmaz.

## Öğrenci

1. **Kayıt:** İlk kayıt formu herkese açıktır. Bölüm seçilir, TC, öğrenci numarası ve iletişim bilgileri girilir. IBAN kayıtta istenmez; öğrenci profil ekranından kendisi girer (ay sonu dökümü için gereklidir).
   Personel (danışman, bölüm başkanı, fakülte sekreterliği) de giriş sayfasındaki "Kayıt Ol" düğmesiyle başvurur. Başvuru pasif hesap olarak açılır; Fakülte Sekreterliği "Tanımlar ve Hesaplar" ekranından aktif edince giriş yapılabilir. Fakülte Sekreterliği hesabını doğrudan da açabilir. İlk fakülte hesabı kurulumda tanımlanır (`Bootstrap` ayarları, bkz. KURULUM.md).
   Hesap ve ilk program katılım dönemi birlikte oluşur.
2. **Profil:** Telefon ve IBAN bilgisi kendi ekranından güncellenir.
3. **Ders günleri:** Haftalık ders günleri girilir; takvimde bu günler ayrıca işaretlenir.
4. **Aylık plan:**
   - Her hafta için 1 ile 3 gün seçilir. Hafta, perşembe gününün düştüğü aya sayılır.
   - Takvimde tatiller, sınav dönemleri ve ders günleri renkle işaretlidir.
   - Plan düzeltme için son gün ayarlardan belirlenir (varsayılan 5). Kaydedilen plan Fakülte Sekreterliği tarafından onaylanır veya gerekçeyle reddedilir.
   - Plan durumu ve geçmiş planlar listeden izlenir.
5. **İzin:** Talep tarih aralığı veya saat ile açılır. Kalan izin hakkı (yılda 10 gün) ve uyarılar talebin yanıtında görünür.
   Rapor gibi belgeler PDF, JPG veya PNG (en çok 5 MB) olarak eklenir. Onaylanmadan önce talep iptal edilebilir.
6. **Devam:** Çalışma günü başında, fakülte ekranındaki karekod telefonla okutulur. Karekod her 30 saniyede yenilenir;
   eski görüntü çalışmaz. Okutma reddedilirse sebep ekranda yazar (oturum kapalı, bugün planda yok, izinli vb.).
   Okutamayan öğrenci Fakülte Sekreterliğine başvurur, kayıt gerekçeyle elle girilir.
7. **Karşılaştırma ve saatler:** Planlanan ve gerçekleşen günler, toplam çalışma saati ve kalan izin hakkı ekranda görünür. Geçmiş Kayıtlarım sayfasındaki "Çalışılan günler" sekmesi seçilen ayda kaç saat ve hangi günlerde çalışıldığını gösterir.
8. **Görevler:** Danışmanın verdiği görevler, notlar ve tamamlanma durumu izlenir. Süresi geçip açık kalan görev "gecikmiş" görünür.

## Danışman

- Kendi öğrencilerinin planlarını, devamlarını, izinlerini ve çalışma saatlerini görür.
- **Görev:** Öğrenciye başlık, açıklama ve bitiş tarihiyle görev verir. Sonradan başlık ve tarihler değişmez; durum ve notlar değerlendirme olarak eklenir.
  Görev "Tamamlandı" veya "Tamamlanmadı" olarak işaretlenir. Yalnızca öğrencinin güncel danışmanı değerlendirme yapabilir.
- **Devamsızlık bildirimi:** Öğrencinin gelmediği veya devamda sorun olan gün için bölüm başkanına bildirim gönderir.
- Geçmiş görev ve değerlendirmelere ulaşır. Rapor almaz; raporlar Fakülte Sekreterliği ve Bölüm Başkanına aittir.

## Bölüm başkanı

- Bölümündeki tüm öğrencileri, planları, devamları, izinleri ve devamsızlıkları görür.
- **Danışman atama:** Öğrencinin danışmanını değiştirir; önceki danışman geçmişte kalır.
- **Devam oturumu:** Bölümü için devam oturumu açıp kapatır ve karekodu ekranda gösterir.
- **Görev:** Bölümündeki öğrencilere görev verebilir; danışman notlarını ve görev durumlarını izler.
- **Bildirimler:** Danışmanlardan gelen devamsızlık bildirimlerini inceler ve okundu olarak işaretler.
- Bölüm, danışman, ay ve yıl filtreleriyle rapor alır, CSV veya Excel olarak indirir.

## Fakülte Sekreterliği / Dekanlık

Fakültedeki tüm kayıtları görür ve yönetir.

- **Hesaplar ve tanımlar:** Personel hesapları, bölümler ve çalışma birimleri. Hesap silinmez, pasif yapılır.
- **Öğrenci yönetimi:** Öğrenciyi çalışma birimine atar, danışman atar, programa dahil eder veya katılımı sonlandırır.
  Her değişiklik tarihli geçmiş olarak saklanır.
- **Plan onayı:** Öğrenci planlarını onaylar veya reddeder; gerekirse öğrenci adına plan girer.
- **İzin:** Talepleri onaylar veya reddeder, izin hakkı ve devamsızlık sınırına yaklaşanları uyarılardan izler.
  İzin hakkı ve sınırlar ayarlardan değiştirilir.
- **Devamsızlık:** Devamsızlık yalnızca bu rol tarafından girilir. Hatalı kayıt gerekçeyle geri alınır.
- **Devam oturumu:** Fakülte genelinde veya bir bölüm için oturum açar, karekodu gösterir, elle devam girer veya iptal eder.
- **Raporlar:** Aylık ve yıllık çalışma, izin ve devamsızlık, danışman, öğrenci listesi, sınıra yaklaşanlar ve
  ilişik kesme incelemesi raporları ekranda görüntülenir; CSV ve Excel olarak indirilir.
- **Aylık döküm:** Öğrenci bazında TC, ad soyad, IBAN, birim, planlanan ve gerçekleşen gün, toplam saat ve izinli gün sayısını içerir.
  Yalnızca bu rol görür; her görüntüleme ve indirme denetim kaydına yazılır.
- **İlişik kesme:** İnceleme raporundan sonra karar (dayanak, tarih, not) kaydedilir. Öğrencinin katılımı aynı işlemde sonlanır.
  Karar sonradan değiştirilemez; kaydın o anki izin ve devamsızlık sayıları karara eklenir.
- **Denetim kayıtları:** Kim, ne zaman, hangi işlemi yaptı bilgisi listelenir.

## Sık karşılaşılan durumlar

| Durum | Ne yapılır |
|---|---|
| Öğrenci planı kaydedemiyor | Haftalık 1-3 gün kuralına ve düzeltme son gününe bakın; hata mesajı ihlal edilen kuralı açıklar. |
| Karekod reddedildi | Mesajdaki sebebe bakın. Okutma sırasında ekrandaki kod güncel olmalı; öğrenci o gün için onaylı planda olmalı. |
| Yanlış devamsızlık girildi | Fakülte Sekreterliği kaydı gerekçeyle geri alır. |
| Öğrenci danışman değiştirdi | Bölüm başkanı veya Fakülte Sekreterliği yeni danışmanı atar; eski görevler eski danışmanın kaydı olarak kalır. |
| Parola unutuldu | Giriş ekranındaki "Parolamı unuttum" bağlantısıyla e-posta adresi girilir; gelen bağlantıyla 30 dakika içinde yeni parola belirlenir. |
