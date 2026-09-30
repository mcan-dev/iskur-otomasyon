# İŞKUR Otomasyon Sistemi ve Kısmi Zamanlı Öğrenci Puantaj Uygulaması Mimari Tasarımı

## 1. Temel Prensipler ve İş Akışı

*   **Mimari Yaklaşım:** Projede .NET Web API kullanılarak N-Tier (N-Katmanlı) Mimari uygulanacaktır.
*   **Standartlar:** Tüm geliştirme süreçlerinde SOLID prensiplerine ve Clean Code standartlarına titizlikle uyulacaktır.
*   **Veritabanı Yönetimi:** Veri erişimi ve şema yönetimi için Entity Framework Core (Code-First) yaklaşımı kullanılacaktır.
*   **İş Akışı ve Yetkilendirme:** 
    *   Sistem **Çoklu Kiracı (Multi-Tenant)** yapısında kurgulanmış olup, her fakülte kendi kiracı (tenant) izolasyonuna sahip olacaktır.
    *   Süreçlerde **Maker-Checker (Giren-Onaylayan)** mekanizması benimsenecektir.
    *   **Maker (Fakülte Sekreteri):** Sisteme öğrenci çalışma saatlerini (puantajları) girer.
    *   **Checker (SKS Admin):** Girilen verileri kontrol eder, onaylar ve İŞKUR sistemine giriş yapacak olan otomasyon sürecini tetikler.

## 2. Katman Yapısı

Proje, bağımlılıkları yönetilebilir kılmak ve sorumlulukları ayırmak amacıyla aşağıdaki katmanlardan oluşacaktır:

*   **IskurAuto.Core:** 
    *   Referanssız en alt katmandır.
    *   Sistemin merkezini oluşturan veritabanı varlıklarını (Entity'ler) ve diğer katmanlar tarafından uygulanacak sözleşmeleri (Interface'ler) içerir.
*   **IskurAuto.Data:** 
    *   Veritabanı işlemleriyle ilgilenen katmandır.
    *   `DbContext`, Code-First Migration'lar ve Repository kalıbı (pattern) uygulamaları bu katmanda yer alır.
*   **IskurAuto.Service:** 
    *   İş mantığının (Business Logic) koşturulduğu ana katmandır.
    *   Maker-Checker onay süreçleri, yetki ve validasyon kontrolleri bu katmanda gerçekleştirilir.
*   **IskurAuto.Automation:** 
    *   Sadece web otomasyon işlemleri için ayrılmış, izole katmandır.
    *   Sadece **Playwright for .NET** bağımlılığını içerir.
    *   SKS Admin onay verdiğinde tetiklenir ve İŞKUR sistemine öğrenci puantajlarını (çalışma saatlerini) **otonom** olarak girer.
*   **IskurAuto.API:** 
    *   Sunum (Presentation) katmanıdır.
    *   Swagger destekli RESTful uç noktalarını (endpoints) barındırır. İstemci (Client) isteklerini karşılar ve servis katmanına yönlendirir.

## 3. Veritabanı Varlıkları (Entities)

Sistemin temel veri yapısı aşağıdaki entity'lerden oluşacaktır:

*   **Faculty:** 
    *   Fakülte tanımlarının tutulduğu tablodur. (Örn: Mühendislik Fakültesi, Eğitim Fakültesi). Multi-Tenant yapısının temelidir.
*   **Role & User:** 
    *   Kullanıcıları ve yetkilerini yönetir.
    *   Fakülte Sekreteri (Maker) ve SKS Admin (Checker) yetkilendirmesi için temel teşkil eder.
*   **StudentWorker:** 
    *   Kısmi zamanlı çalışan öğrencilerin bilgilerini içerir.
    *   Alanlar: TC Kimlik No, Ad, Soyad, `FacultyId` (Kiracı ilişkisi).
*   **MonthlyTimesheet:** 
    *   Aylık puantaj kayıtlarının (çalışma saatleri) tutulduğu tablodur.
    *   Alanlar: Öğrenci ID, Ay/Yıl, Çalışılan Toplam Saat, `Status`.
    *   **Statüler (Status):**
        *   `PendingSksApproval`: SKS onayı bekliyor.
        *   `ApprovedBySks`: SKS tarafından onaylandı, otomasyon sırasını bekliyor.
        *   `ProcessedToIskur`: Otomasyon tarafından İŞKUR sistemine başarıyla girildi.
        *   `Error`: Otomasyon veya diğer süreçlerde hata oluştu.
*   **TaskLog:** 
    *   Playwright botunun İŞKUR'a veri girerken oluşturduğu denetim ve hata izlerini (loglarını) tutar.
    *   Alanlar: İlgili Puantaj ID, İşlem Zamanı (Timestamp), Başarı Durumu, Hata Detayı.
