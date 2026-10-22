# Vize gösterim senaryosu

Asgari ölçüt: veriyi arayüzden eklemek, görüntülemek, bir kaydı güncellemek ve ay/yıl gibi bir ölçütle filtrelemek.
Aşağıdaki adımlar bunu sırayla gösterir. Hazırlık: API ve arayüz çalışıyor, `03_ornek_veri.sql` yüklü olabilir (zorunlu değil).

## 1. Öğrenci kaydı (ekleme)

1. `/giris` sayfasında "Kayıt olun" bağlantısına tıklayın.
2. Formu doldurun: ad, geçerli T.C. kimlik numarası, IBAN, bölüm, öğrenci numarası, e-posta, telefon, kayıt yılı, parola.
3. Kayıttan sonra giriş yapın. Profil ekranında bilgiler görünür; birim ve danışman "atanmadı" yazar.
4. **Veritabanında gösterim:** `SELECT * FROM Users WHERE Email = '...'` ve `SELECT * FROM StudentProfiles`, `ProgramParticipations`.

## 2. Aylık çalışma planı (ekleme, tekrar görüntüleme)

1. Öğrenci hesabıyla "Çalışma Planım" ekranını açın.
2. Takvimde bir hafta için 1–3 gün seçin; tatil ve hafta sonu günlerinin seçilemediğini gösterin.
3. Haftada 4 gün seçip kaydetmeyi deneyin: uyarı verir.
4. Geçerli seçimle "Planı Kaydet". Sayfayı yenileyin veya çıkış-giriş yapın: plan aynı günlerle geri gelir.
5. **Veritabanında gösterim:** `MonthlyWorkPlans` (öğrenci, yıl, ay) ve `WorkPlanDays` tabloları.
6. "Geçmiş Kayıtlarım" ekranında önceki ayların planı ayrı satır olarak görünür.

## 3. Yetkili ekranda listeleme ve filtreleme

1. `sekreter@teknoloji.edu.tr` ile giriş yapın, "Öğrenciler" ekranını açın.
2. Bölüm, danışman, kayıt yılı ve çalışma durumu filtrelerini sırayla deneyin.
3. `baskan.bm@teknoloji.edu.tr` ile girin: yalnızca Bilgisayar Mühendisliği öğrencileri görünür (rol kapsamı).
4. `danisman.bm@teknoloji.edu.tr` ile girin: yalnızca kendi öğrencileri görünür.

## 4. Güncelleme

- Fakülte hesabıyla bir öğrenciyi açın, "Birim ata" ve "Danışman ata" işlemlerini yapın. Eski atamanın bitiş tarihi yazılır,
  yeni satır açılır (`StudentUnitAssignments`, `AdvisorAssignments`).
- Öğrenci hesabıyla profilde telefon veya IBAN'ı değiştirip kaydedin.
- "Çalışma Planı Onayları" ekranında bekleyen planı onaylayın veya gerekçeyle reddedin.

## 5. Ay/yıl filtresi ve rapor

1. "Raporlar" ekranında "Aylık çalışma" raporunu seçin, ay ve yılı değiştirin; tablo değişir.
2. "Excel olarak indir" ile aynı filtreli dosyayı alın.
3. "Ay sonu dökümü" raporunda T.C., IBAN, planlanan ve gerçekleşen gün ile toplam saati gösterin.

## 6. Tasarımı yapılan modüller

- **İzin:** Öğrenci hesabında izin talebi açın; kalan hak formda görünür. Fakülte hesabında onaylayın; kalan hak azalır.
- **Karekod:** Fakülte hesabında "Karekod Devam" → oturum aç → karekodun 30 saniyede yenilendiğini gösterin; öğrenci hesabıyla okutun.
- **Görev:** Danışman hesabıyla görev verin ve "Tamamlandı" işaretleyin; değerlendirme tarihiyle listelenir.
- **Denetim:** "Denetim Kayıtları" ekranında yapılan işlemlerin kim, ne zaman bilgisiyle listelendiğini gösterin.

## Tamamlananlar ve kalanlar

Tamamlanan ve henüz yapılmayan özelliklerin listesi: [0_Yonerge/README.md](../0_Yonerge/README.md) ve
[1_Arastirma/06_vize_sonrasi.md](../1_Arastirma/06_vize_sonrasi.md).
