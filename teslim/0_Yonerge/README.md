# 0 – Yönerge

Projenin dayandığı iki belge bu klasördedir:

- `iskur.docx` – Yazılım Geliştirme Gereksinimleri ve İşleyiş Dokümanı (asıl gereksinim belgesi)
- `iskur_vize.docx` – Vizeye kadar tamamlanması gereken çalışmalar ve teslim esasları

## Vize belgesinin istedikleri ve karşılıkları

| Vize belgesindeki istek | Karşılığı |
|---|---|
| Sistemin amacı ve kapsamı | [1_Arastirma/01_amac_ve_kapsam.md](../1_Arastirma/01_amac_ve_kapsam.md) |
| Kullanıcı rolleri ve işlemleri | [1_Arastirma/02_roller_ve_islemler.md](../1_Arastirma/02_roller_ve_islemler.md) |
| İş akışları (kayıt, plan, danışman ataması, izin, devam, rapor) | [1_Arastirma/03_is_akislari.md](../1_Arastirma/03_is_akislari.md) |
| Temel ekranlar ve ekran taslakları | [1_Arastirma/04_ekranlar.md](../1_Arastirma/04_ekranlar.md), [2_Tasarimlar](../2_Tasarimlar) |
| Vize sonrası geliştirilecekler | [1_Arastirma/06_vize_sonrasi.md](../1_Arastirma/06_vize_sonrasi.md) |
| Tablolar, alanlar, ilişkiler, anahtarlar | [3_Veritabani/01_er_diyagrami.md](../3_Veritabani/01_er_diyagrami.md), [3_Veritabani/04_tablo_sozlugu.md](../3_Veritabani/04_tablo_sozlugu.md) |
| ER diyagramı | [3_Veritabani/01_er_diyagrami.md](../3_Veritabani/01_er_diyagrami.md) |
| Örnek test verisi | [3_Veritabani/03_ornek_veri.sql](../3_Veritabani/03_ornek_veri.sql) |
| Giriş ekranı ve rol bazlı erişim | Çalışıyor – [4_Uygulama](../4_Uygulama) |
| Öğrenci profili, aylık plan, kayıt ekleme/görüntüleme/güncelleme | Çalışıyor |
| Öğrenci listesi, bölüm ve danışman filtreleri | Çalışıyor |
| İzin, karekod, görev, rapor, denetim, yedekleme | Vizeden öteye, çalışır durumda (aşağıdaki tabloya bakın) |

## Vize "Özellik / Vizeye kadar" tablosunun durumu

| Özellik | Vizede beklenen | Durum |
|---|---|---|
| Veritabanı ve ilişkiler | Tamamlanmalı | Tamamlandı (SQL Server, 23 tablo) |
| Öğrenci profili ve çalışma planı | Temel sürüm | Tamamlandı |
| Rol bazlı yetkilendirme | Temel sürüm | Tamamlandı (4 rol, kapsam denetimi) |
| Ay/yıl ve bölüm filtreleri | Temel filtreler | Tamamlandı |
| İzin talebi ve izin hakkı hesabı | Tasarım | Tamamlandı, çalışıyor |
| Dinamik karekodla devam | Tasarım ve iş akışı | Tamamlandı, çalışıyor (30 sn'de yenilenen imzalı jeton) |
| Danışman görev ve değerlendirme | Ekran taslağı / veri modeli | Tamamlandı, çalışıyor |
| Aylık son döküm ve dışa aktarma | Örnek rapor taslağı | Tamamlandı (Excel ve CSV) |
| Denetim kayıtları, yedekleme, güvenlik | Temel tasarım | Denetim kaydı çalışıyor; yedekleme betiği ve güvenlik notları hazır |

## Bu projedeki tek sapma

Belgedeki "yarım gün çalışma" seçeneği uygulanmamıştır. Günlük çalışma süresi 8 saattir; planlanan her gün tam gün
sayılır. Bunun dışında belgeden ayrılan bir yer yoktur.
