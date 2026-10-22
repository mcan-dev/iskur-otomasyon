# Vize sonrası geliştirilecekler

Vizeye kadar istenenlerin tamamı, ayrıca izin, karekod, görev, rapor, denetim ve ilişik kesme modülleri çalışır durumdadır.
Aşağıdakiler henüz yapılmamıştır ve final dönemi planındadır.

## Final planı

| Sıra | İş | Açıklama |
|---|---|---|
| 1 | Öğrenci ekranlarının gözden geçirilmesi | Kullanıcı denemelerinden gelen düzeltmeler |
| 2 | E-posta bildirimleri | Plan onayı/reddi, izin sonucu, sınıra yaklaşma uyarısı; parola sıfırlama e-postası şimdiden çalışır |
| 3 | PDF çıktıları | Ay sonu dökümü ve ilişik kesme kararı için imzaya uygun PDF |
| 4 | Ders programından otomatik ders günü | Şimdilik öğrenci beyanına dayanır (belgede kabul edilmiştir) |
| 5 | Toplu işlemler | Öğrencileri Excel'den içe aktarma, toplu birim/danışman ataması |
| 6 | Kurumsal oturum açma | Selçuk Üniversitesi hesabıyla tek oturum açma (SSO), kurum e-postası zorunluluğu |
| 7 | Yönetim paneli grafikleri | Aylık çalışma saati, izin kullanımı, devam oranı |
| 8 | Büyük raporlar için sayfalama | Şimdiki rapor birkaç yüz öğrenci için uygundur |
| 9 | Otomatik yedekleme zamanlaması | Betik hazırdır; Windows Görev Zamanlayıcı veya SQL Server Agent ile zamanlanacak |
| 10 | İŞKUR sitesiyle entegrasyon | Ayrı modül olarak bekletilmektedir (`parked/` klasörü) |
| 11 | Erişilebilirlik ve mobil düzen denetimi | Ekran okuyucu ve küçük ekran denemeleri |
| 12 | Yük ve güvenlik testi | Eşzamanlı karekod okutma, yetki denemeleri |

## Bilinen sınırlar

- Karekod fotoğrafla başka birine iletilebilir; jeton 30 saniyelik pencerelerle sınırlı olduğu ve oturum başına tek kayıt
  verildiği için etkisi küçüktür. Konum doğrulaması yoktur.
- Devamsızlık otomatik üretilmez; fakülte yönetimi girer.
- Excel çıktısı düz tablodur (biçimlendirme ve formül yok).
