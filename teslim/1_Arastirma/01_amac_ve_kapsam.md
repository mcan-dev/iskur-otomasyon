# Sistemin amacı ve kapsamı

## Amaç

Selçuk Üniversitesi Teknoloji Fakültesi'nde yürütülen İŞKUR öğrenci programının süreçlerini dijital ortamda yönetmek.
Bugün öğrencilerin çalışma günleri, izinleri ve devamları elle takip edilmektedir; toplam izin takibi hiç yapılmamaktadır.
Otomasyon bu işleri tek bir sistemde toplar.

Sistem yalnızca veri girişi yapılan bir uygulama değildir. Veriler ilişkisel veritabanında tutulur, geçmiş yıl ve aylara
ulaşılır, farklı ölçütlerle sorgulanır, rapor alınır ve yetkili kullanıcılar işlemleri sistemden yürütür.

## Kapsam

Kapsama giren süreçler:

1. Öğrenci kaydı ve profil (program katılım dönemleri dahil)
2. Birim ve danışman atamaları (tarihli geçmişiyle)
3. Aylık çalışma planı (takvim, haftalık 1–3 gün kuralı, tatil/ders/sınav kuralları, onay)
4. İzin talebi ve otomatik izin hakkı hesabı
5. Devamsızlık kaydı ve sınır uyarıları
6. Dinamik karekod ile devam takibi
7. Çalışma saati hesabı ve raporlama (ekranda, Excel ve CSV)
8. Görev ve danışman değerlendirmesi, devamsızlık bildirimi
9. İlişik kesme karar kaydı
10. Kullanıcı, bölüm ve birim yönetimi; tatil ve sınav dönemi tanımı
11. Denetim kaydı, yedekleme ve güvenlik

Kapsama girmeyenler:

- İŞKUR'un kendi internet sitesiyle entegrasyon (bu çalışma ayrı bir modül olarak bekletilmektedir)
- Maaş hesabı ve ödeme işlemi
- Mobil uygulama (arayüz telefonda tarayıcıdan kullanılabilir)

## Çalışma kuralları

- Günlük çalışma süresi **8 saattir**. Planlanan her gün tam gündür; yarım gün yoktur.
- Kayıtlar silinmez. Yanlış kayıt gerekçeyle geri alınır; geçmiş korunur.
- Sistem uyarı üretir ama idari kararı kendisi vermez. İzin sınırı, devamsızlık sınırı ve ilişik kesme kararı yetkili kişiler tarafından verilir; sınırlar ayarlardan değiştirilebilir.
- Öğrenci yalnızca kendi kayıtlarını görür; bölüm başkanı yalnızca kendi bölümünü; danışman yalnızca kendi öğrencilerini.

## Kullanılan teknolojiler

| Katman | Teknoloji |
|---|---|
| Veritabanı | Microsoft SQL Server (geliştirmede LocalDB) |
| Arka yüz | .NET 8, ASP.NET Core Web API, EF Core (Code-First), JWT |
| Arayüz | React 18, TypeScript, Vite |
| Test | xUnit (birim ve veritabanı destekli testler) |
