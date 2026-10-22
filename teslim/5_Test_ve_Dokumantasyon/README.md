# 5 – Test ve Dokümantasyon

| Belge | İçerik |
|---|---|
| [01_vize_gosterim_senaryosu.md](01_vize_gosterim_senaryosu.md) | Vize sunumunda arayüzden başlayıp veritabanına kaydı ve tekrar görüntülemeyi gösteren adımlar |
| [../../docs/TEST-SENARYOLARI.md](../../docs/TEST-SENARYOLARI.md) | Otomatik test senaryoları, sınıf ve sayı listesi |
| [../../docs/KURULUM.md](../../docs/KURULUM.md) | Kurulum, yapılandırma, yayına alma, yedekleme ve geri yükleme |
| [../../docs/KULLANIM.md](../../docs/KULLANIM.md) | Rol bazlı kullanım kılavuzu |
| [../../docs/API.md](../../docs/API.md) | Uç noktalar |
| [../../docs/DATABASE.md](../../docs/DATABASE.md) | Veritabanı notları |
| [../../docs/GEREKSINIMLER.md](../../docs/GEREKSINIMLER.md) | Gereksinim belgesindeki her maddenin karşılığı |
| [../../docs/NEDEN-BOYLE.md](../../docs/NEDEN-BOYLE.md) | Tasarım kararlarının nedenleri |
| [../../ARCHITECTURE.md](../../ARCHITECTURE.md) | Katmanlı mimari |

## Testleri çalıştırma

```powershell
dotnet test
```

Testler xUnit ile yazılmıştır. Veritabanı kullanan testler her test için ayrı, bellek içi bir veritabanı kurar
(sahte nesne kullanılmaz); saat sabittir, bu yüzden sonuçlar günden bağımsızdır.

## Güvenlik ve yedekleme özeti

- Parolalar PBKDF2 ile özetlenir; oturum JWT iledir.
- Yetki iki yerde denetlenir: controller'da rol, serviste kapsam (bölüm, danışman, öğrencinin kendisi).
- T.C. kimlik ve IBAN yetkisiz kullanıcıya maskeli gösterilir.
- Önemli işlemler denetim kaydına yazılır; kayıtlar silinmez.
- Karekod jetonları 30 saniyelik pencerelerle imzalanır.
- Yedek: `BACKUP DATABASE` ile veritabanı, belgeler klasörü ayrıca; ayrıntı KURULUM.md'de.
