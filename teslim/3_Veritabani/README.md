# 3 – Veritabani

Veriler **Microsoft SQL Server** üzerinde ilişkisel olarak tutulur. Şema, uygulamadaki EF Core modelinden (Code-First)
üretilir; aşağıdaki betikler aynı şemanın elle çalıştırılabilir halidir.

| Dosya | İçerik |
|---|---|
| [01_er_diyagrami.md](01_er_diyagrami.md) | ER diyagramı, ilişki özeti, benzersizlik ve bütünlük kuralları |
| [02_sema.sql](02_sema.sql) | Tüm tabloları, birincil/yabancı anahtarları ve indeksleri oluşturan T-SQL betiği (EF Core modelinden üretildi) |
| [03_ornek_veri.sql](03_ornek_veri.sql) | Örnek test verisi (bölüm, birim, kullanıcı, öğrenci, plan, izin, devam, görev, karar) |
| [04_tablo_sozlugu.md](04_tablo_sozlugu.md) | Her tablonun alanları, türleri, anahtarları ve indeksleri |

## Veritabanını kurma

### Yol 1 – Uygulama kendisi kurar (önerilen)

Visual Studio ile gelen LocalDB yeterlidir. API'yi çalıştırmak (`dotnet run --project src/IskurAuto.API`) `IskurAuto`
veritabanını ve tabloları oluşturur; Development ortamında demo hesaplar da yüklenir.

### Yol 2 – Betiklerle

1. SQL Server Management Studio (veya Azure Data Studio) ile sunucuya bağlanın. LocalDB için sunucu adı `(localdb)\MSSQLLocalDB`.
2. Yeni sorgu penceresinde: `CREATE DATABASE IskurAuto;` çalıştırın, ardından `USE IskurAuto;` yapın.
3. `02_sema.sql` dosyasını çalıştırın (tablolar oluşur).
4. `03_ornek_veri.sql` dosyasını çalıştırın (örnek veri yüklenir).
5. API'yi çalıştırın. Tablolar zaten varsa uygulama onlara dokunmaz.

Örnek verideki tüm hesapların parolası `Demo1234!` değeridir (yalnızca deneme içindir).

| E-posta | Rol |
|---|---|
| sekreter@teknoloji.edu.tr | Fakülte Sekreterliği / Dekanlık |
| baskan.bm@teknoloji.edu.tr | Bölüm Başkanı (Bilgisayar Müh.) |
| danisman.bm@teknoloji.edu.tr, danisman2.bm@… | Danışman (Bilgisayar Müh.) |
| danisman.ym@teknoloji.edu.tr | Danışman (Yazılım Müh.) |
| ogrenci@, ogrenci2@, burak.sahin@, selin.celik@teknoloji.edu.tr | Öğrenci |

## Örnek verinin gösterdikleri

- **Aynı öğrencinin farklı ay ve yılları:** Mehmet Öztürk için Eylül 2025, Ekim 2025, Eylül 2026 ve Ekim 2026 planları ayrı satırlardır.
- **Geçmiş korunması:** Mehmet'in birimi Kütüphane'den Bilgisayar Laboratuvarı'na, danışmanı Zeynep Arslan'dan Hakan Polat'a geçmiştir; eski satırların bitiş tarihi yazılıdır.
- **Plan durumları:** Onaylanmış, onay bekleyen ve gerekçeyle reddedilmiş plan birer örnekle vardır.
- **İzin:** Günlük, saatlik (4 saat = 0,5 gün hak düşer) ve onay bekleyen sağlık izni.
- **Programdan ayrılan öğrenci:** Kerem Aydın'ın katılımı sona ermiş, ilişik kesme kararı kayıtlıdır; geçmiş kayıtları durur.
- **Bölüm filtresi denemesi:** Üç bölümde öğrenci vardır; bölüm başkanı yalnızca kendi bölümünü görür.

## Yedekleme

Veritabanı yedeği `BACKUP DATABASE` ile alınır; ayrıntı ve geri yükleme adımları
[5_Test_ve_Dokumantasyon/KURULUM.md](../5_Test_ve_Dokumantasyon/KURULUM.md) dosyasındadır.
`scripts/yedekle.ps1` yedeği ve belgeler klasörünü tarihli klasöre alır.
