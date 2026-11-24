# Kullanım kılavuzu

Sistemde dört rol vardır: Öğrenci, Sorumlu personel (eski adıyla danışman), Birim yetkilisi (eski adıyla bölüm başkanı) ve Fakülte Sekreterliği / Dekanlık. Rol adları değişti; yetkiler aynı kaldı. Her kullanıcı yalnızca kendi rolünün işlemlerini yapabilir; kapsam dışı kayıtlar hiç görünmez.
Uç noktaların listesi için [API.md](API.md) dosyasına bakın.

## Ortak işlemler

- Her rolün kendi giriş sayfası vardır (`/giris` sayfasından seçilir). Hesap yalnızca kendi rolünün giriş sayfasından girebilir.
- **Kayıt olma yoktur.** Bilgiler, fakültede İŞKUR kapsamında çalışan öğrenciler için bilgi işlemden alınır:
  - Öğrenci, **öğrenci e-postası ve OBİS şifresiyle** giriş yapar. Öğrenci hesabını Fakülte Sekreterliği "Öğrenciler > Öğrenci ekle" ile açar. OBİS şifresi bu sistemde değiştirilmez; OBİS'te değişirse sekreterlik öğrencinin sayfasından yeni şifreyi girer.
  - Personel **sicil numarası ve şifresiyle** giriş yapar. Personel hesabını Fakülte Sekreterliği "Tanımlar ve Hesaplar > Personel hesapları" ile açar. Personel şifresini kendisi değiştirir; unutursa "Şifremi unuttum" ile (sicil no veya e-posta) sıfırlama bağlantısı alır.
- Hiçbir kayıt silinmez. Yanlış girilen kayıt gerekçeyle geri alınır veya iptal edilir; geçmiş korunur.
- Önemli işlemler denetim kaydına yazılır. Kayıtlarda kişisel veri tutulmaz.

## Öğrenci

1. **Profil ve IBAN:** Telefon ve IBAN kendi ekranından güncellenir. IBAN öğrencinin kendisine ait olmalıdır: hesap sahibi adı soyadı kayıtlı adla aynı yazılır ve "Bu IBAN bana aittir" onaylanır. Yalnızca şubesi olan bankaların hesapları kabul edilir; Enpara, Colendi, Hayat Finans gibi şubesiz dijital bankalar ve Papara gibi elektronik para hesapları reddedilir.
2. **Ders günleri:** Dersli günler işaretlenir ve her gün için öğleden önce, öğleden sonra ya da tam gün seçilir. IBAN kaydedilmeden ders günü ve plan kaydedilemez.
3. **Aylık plan:**
   - Her hafta 1–3 gün, ayda en fazla 14 gün seçilir. Hafta, perşembe gününün düştüğü aya sayılır; sınav dönemi haftalarında en az gün şartı aranmaz.
   - **Tam gün dersi olan gün çalışma günü olarak seçilemez.**
   - Renkler: yarım gün dersli günü seçerseniz **sarı**, boş (dersi olmayan) günü seçerseniz **yeşil** görünür. Aynı hafta içinde tamamen boş bir gün varken yarım gün dersli günü seçtiyseniz, seçmediğiniz boş gün **kırmızı** yanar. Hafta sonu, tatil ve tam gün dersli günler de kırmızıdır ve seçilemez.
   - Üniversite hayatı boyunca en fazla **140 iş günü** çalışılabilir. Çalışılan ve kalan gün plan ekranında ve profilde görünür; sınırı aşan plan kaydedilmez.
   - Plan, ayın 5'ine kadar gönderilir; Fakülte Sekreterliği onaylar veya gerekçeyle reddeder.
4. **İzin:**
   - İzin sayfasında o ayın çalışma takvimi açılır. Yalnızca **onaylı plandaki çalışma günleri** seçilebilir; çalışma günü olmayan ve geçmiş günler seçilemez.
   - İzin türü ve gerekçe yoktur; açıklama isteğe bağlıdır.
   - İzin **onay beklemez**: "İzin Al" denince hemen geçerli olur, Fakülte Sekreterliği bilgilendirilir.
   - Ayda en az bir gün çalışmak zorunludur; ayın bütün çalışma günleri için izin alınamaz.
   - Tek bir izin hakkı vardır (varsayılan yılda 10 gün); izinler ve devamsızlıklar aynı haktan düşer. Gelecekteki bir izin öğrenci tarafından iptal edilebilir.
5. **Devam:** Fakültedeki monitörde sabah mesai başlangıcında ve akşam mesai bitişinde karekod gösterilir; telefonla okutulur. Karekod 30 saniyede bir değişir. **Yalnızca çalışma günü olan günlerde** okutulabilir; plan dışı günde, izinliyken veya saat dışında okutma reddedilir ve sebebi yazılır.
6. **Karşılaştırma ve saatler:** Planlanan ve gerçekleşen günler, çalışma saati ve kalan izin hakkı görünür.
7. **Görevler:** Sorumlu personelin verdiği görevler ve notlar izlenir.

## Sorumlu personel

- Kendi öğrencilerinin planlarını, devamlarını, izinlerini ve çalışma saatlerini görür; izinleri görür ama onaylamaz (izin onay gerektirmez).
- **Kim ne zaman çalışıyor:** Öğrencilerinin hangi gün çalıştığını günlere göre görür.
- Görev verir, değerlendirme yazar, birim yetkilisine devamsızlık bildirimi gönderir.

## Birim yetkilisi

- Bölümündeki tüm öğrencileri, planları, devamları, izinleri ve devamsızlıkları görür.
- **Sorumlu personel atama:** Öğrenci listesinde birden çok öğrenciyi işaretleyip tek işlemde, sorumlu personelin **sicil numarasıyla** atar. Önceki atama geçmişte kalır.
- **Kim ne zaman çalışıyor:** Günlere göre çalışanları ve **geçmiş yıllarda** (ör. 2025) çalışanları görür.
- Çalışılan ve kalan iş günü (140 gün) bilgisini öğrenci sayfasında görür.
- Karekod ekranı ve oturumları yalnızca fakülte sekreterliği tarafından yönetilir.

## Fakülte Sekreterliği / Dekanlık

Fakültedeki tüm kayıtları görür ve düzenler (süper yetkili).

- **Öğrenci hesapları:** "Öğrenciler > Öğrenci ekle" ile bilgi işlemden gelen öğrenci e-postası ve OBİS şifresiyle hesap açar. Öğrencinin sayfasında "Öğrenci bilgilerini düzenle" ile ad, e-posta, TC, numara, bölüm, kayıt yılı, IBAN ve OBİS şifresini değiştirir.
- **Personel hesapları:** Sicil numarasıyla personel açar, düzenler, pasif yapar.
- **Toplu atama:** Öğrenciler ekranında bölüm seçince o bölümün öğrencileri listelenir; birden çok öğrenci işaretlenip tek işlemde çalışma birimine ve/veya sorumlu personele (sicil no ile) atanır.
- **Plan onayı:** Öğrenci planlarını onaylar veya reddeder; gerekirse öğrenci adına plan girer.
- **İzin:** İzinleri onaylamaz; tüm izinleri ve ana sayfadaki "İzin bildirimleri"ni izler, gerektiğinde bir izni gerekçesiz ya da notla **iptal eder**. Genel izin hakkını ayarlardan, öğrenciye özel toplam izin gün sayısını öğrencinin İzin sekmesinden belirler.
- **Mesai karekod ekranı:** Sabah ve akşam karekod saatlerini belirler. Monitörde bu ekran açık bırakılır; saat aralığı içinde karekod kendiliğinden görünür ve 30 saniyede bir değişir.
- **Kim ne zaman çalışıyor:** Günlere ve yıllara göre çalışan öğrenciler.
- **Devamsızlık, raporlar, ay sonu dökümü, ilişik kesme, denetim kayıtları:** Önceki gibi.

## Sık karşılaşılan durumlar

| Durum | Ne yapılır |
|---|---|
| Öğrenci giriş yapamıyor | Öğrenci e-postası ve OBİS şifresiyle girmeli. OBİS şifresi değiştiyse sekreterlik yeni şifreyi öğrencinin sayfasından girer. |
| Personel giriş yapamıyor | Sicil numarasıyla girmeli; şifreyi unuttuysa "Şifremi unuttum". "Hesabınız aktif değil" yazıyorsa sekreterlik hesabı aktif yapar. |
| Öğrenci bir günü seçemiyor | Tam gün dersli gün, hafta sonu veya tatil seçilemez. 140 iş günü sınırı dolmuş olabilir. |
| İzin alınamıyor | O ay için onaylı plan olmalı; yalnızca çalışma günleri seçilir; ayın tüm çalışma günleri izinle kapatılamaz; izin hakkı yetmiyor olabilir. |
| Karekod reddedildi | Bugün onaylı planda çalışma günü olmalı, öğrenci izinli olmamalı ve karekod saatleri içinde okutulmalı. |
| IBAN kabul edilmiyor | Hesap sahibi adı öğrencinin adıyla aynı olmalı; dijital banka ve Papara gibi hesaplar kabul edilmez. |
