# Temel ekranlar

Her ekranın gerçek görüntüsü [2_Tasarimlar/ekranlar](../2_Tasarimlar/ekranlar) klasöründedir. Menüler role göre değişir.

## Herkese açık

| Ekran | Adres | Görüntü |
|---|---|---|
| Giriş | `/giris` | 00-giris.png |
| Öğrenci kaydı | `/kayit` | 01-ogrenci-kayit.png |
| Parolamı unuttum, parola sıfırlama | `/parolami-unuttum`, `/sifre-sifirla` | 02-parola-sifirlama.png |

## Öğrenci

| Menü | Adres | İçerik | Görüntü |
|---|---|---|---|
| Ana Sayfa | `/` | Plan durumu, kalan izin, çalışma saati özeti | 10 |
| Çalışma Planım | `/plan` | Renkli aylık takvim, gün seçimi, not, ders günleri | 11 |
| İzin Taleplerim | `/izinlerim` | Talep formu, kalan hak, talep listesi | 12 |
| Karekod ile Devam | `/devam-kaydi` | Karekodu okutma, planlanan/gerçekleşen karşılaştırma | 13 |
| Görevlerim | `/gorevlerim` | Verilen görevler, notlar, durum | – |
| Geçmiş Kayıtlarım | `/gecmis` | Önceki ay ve yıllara ait plan, devam, izin | – |
| Profilim | `/profil` | Kişisel bilgiler, birim, danışman, program dönemleri | 14 |

## Fakülte Sekreterliği / Dekanlık

| Menü | Adres | İçerik | Görüntü |
|---|---|---|---|
| Ana Sayfa | `/` | Özet kartları, sınıra yaklaşanlar | 20 |
| Öğrenciler | `/ogrenciler` | Bölüm, danışman, yıl, durum filtreli liste | 21 |
| Öğrenci detayı | `/ogrenciler/:id` | Genel bilgi, planlar, izinler, devam, görev sekmeleri; birim ve danışman atama | 22 |
| Çalışma Planı Onayları | `/plan-onaylari` | Bekleyen planlar, onay / ret | 23 |
| İzin Talepleri | `/izinler` | Talep listesi, onay / ret, uyarılar | – |
| Devamsızlık | `/devamsizlik` | Kayıt girme, geri alma | – |
| Karekod Devam | `/devam-oturumlari` | Oturum açma, 30 sn'de yenilenen karekod, elle kayıt | 24 |
| Görevler | `/gorevler` | Görev verme, takip | – |
| Raporlar | `/raporlar` | Rapor türü ve filtreler, Excel/CSV indirme | 25 |
| İlişik Kesme | `/iliski-kesme` | Karar listesi ve karar girişi | 26 |
| Tanımlar ve Hesaplar | `/tanimlar` | Bölüm, birim, personel, tatil, sınav dönemi, izin sınırları | 27 |
| Denetim Kayıtları | `/denetim` | Kim, ne zaman, ne yaptı | 28 |

## Bölüm Başkanı

Bölüm Öğrencileri (31), Karekod Devam, İzin ve Devamsızlık, Görevler ve Notlar, Devamsızlık Bildirimleri, Raporlar. Danışman atama
öğrenci detay ekranından yapılır. Görüntü: 30, 31.

## Danışman

Öğrencilerim (41), Devam Durumu, İzin Durumu, Görev ve Değerlendirme, Devamsızlık Bildirimi, Raporlar. Görüntü: 40, 41.

## Ekran taslağı

Genel düzen tüm rollerde aynıdır:

```
+----------------+---------------------------------------------------+
| SELÇUK ÜNİV.   |  Sayfa başlığı                  [Rol]   [Çıkış]   |
| Teknoloji Fak. +---------------------------------------------------+
|                |                                                   |
| Ana Sayfa      |   Kart: filtreler                                 |
| Menü öğesi     |   Kart: tablo / takvim / form                     |
| Menü öğesi     |                                                   |
| ...            |                                                   |
|                |                                                   |
| [Kullanıcı]    |                                                   |
+----------------+---------------------------------------------------+
```
