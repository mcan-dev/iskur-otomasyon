# Kullanıcı rolleri ve işlemleri

Gereksinim belgesi dört rol tanımlar. Vize belgesinde "fakülte sekreterliği ve dekanlık" ayrı sayılmıştır; asıl belgede
bu ikisi tek rol olarak (2.1) ele alınmıştır ve aynı yetkileri taşır. Sistemde bu rol **Fakülte Sekreterliği / Dekanlık**
adıyla tek hesap türüdür. Dekanlıktan ayrı bir izleme hesabı istenirse aynı rolde ikinci bir hesap açılır.

| Rol | Kapsam |
|---|---|
| Öğrenci | Yalnızca kendi kayıtları |
| Danışman Öğretim Elemanı | Danışmanı olduğu öğrenciler |
| Bölüm Başkanı | Kendi bölümündeki öğrenciler |
| Fakülte Sekreterliği / Dekanlık | Fakültenin tamamı |

## Giriş

Her rolün kendi giriş sayfası vardır: `/giris/ogrenci`, `/giris/danisman`, `/giris/bolum-baskani`, `/giris/fakulte`.
Hesap yalnızca kendi rolünün sayfasından girebilir; parola doğru olsa bile başka rolün sayfasından giriş reddedilir.
Kontrol sunucuda yapılır, yalnızca arayüzde değildir.

Her giriş sayfasında "Kayıt Ol" düğmesi vardır. Öğrenci kaydı hemen aktif olur. Danışman, bölüm başkanı ve fakülte
sekreterliği başvuruları pasif hesap olarak açılır; fakülte sekreterliği "Tanımlar ve Hesaplar" ekranından hesabı aktif
edince giriş yapılabilir. Böylece yetkili bir hesap kendi kendine açılamaz.

## İşlem matrisi

(● yapabilir, ○ yalnızca görüntüler, – yetkisi yok)

| İşlem | Öğrenci | Danışman | Bölüm Başkanı | Fakülte Sek./Dekanlık |
|---|---|---|---|---|
| İlk kayıt (kendi bilgileri) | ● | – | – | – |
| Profil görüntüleme | ○ (kendi) | ○ | ○ | ○ |
| Telefon ve IBAN güncelleme | ● (kendi) | – | – | – |
| Ders günlerini girme | ● | – | – | – |
| Aylık çalışma planı oluşturma / düzeltme | ● | – | – | ● (öğrenci adına) |
| Plan onayı / reddi | – | – | – | ● |
| İzin talebi oluşturma, iptal | ● | – | – | – |
| İzin onayı / reddi | – | – | – | ● |
| İzin hakkı ve devamsızlık sınırını belirleme | – | – | – | ● |
| Devamsızlık kaydı girme / geri alma | – | – | – | ● |
| Karekod oturumu açma / kapatma | – | – | ● (bölümü için) | ● |
| Karekodu okutma | ● | – | – | – |
| Elle devam kaydı / devam kaydını geri alma | – | – | – | ● |
| Devam ve çalışma saati görüntüleme | ○ (kendi) | ○ | ○ | ○ |
| Birim ataması | – | – | – | ● |
| Danışman ataması / değişikliği | – | – | ● | ● |
| Program katılımını sonlandırma | – | – | – | ● |
| Görev verme | – | ● | ● | ● |
| Görev değerlendirme, "Tamamlandı / Tamamlanmadı" | – | ● | – | – |
| Devamsızlık bildirimi gönderme | – | ● | – | – |
| Devamsızlık bildirimlerini inceleme | – | ○ (kendi gönderdikleri) | ● | ○ |
| Rapor alma (ekran, Excel, CSV) | – | ● (kendi öğrencileri) | ● (bölümü) | ● |
| Ay sonu dökümü (TC ve IBAN içerir) | – | – | – | ● |
| İlişik kesme incelemesini görme | – | – | ● | ● |
| İlişik kesme kararı girme | – | – | – | ● |
| Personel hesabı, bölüm, birim tanımı | – | – | – | ● |
| Resmî tatil ve sınav dönemi tanımı | – | – | – | ● |
| Denetim kayıtlarını görme | – | – | – | ● |

## Kişisel veri

T.C. kimlik numarası ve IBAN, yetkisi olmayan kullanıcıya maskeli gösterilir (örneğin `123****50`). Tam değer yalnızca
öğrencinin kendisine, fakülte sekreterliğine ve ay sonu dökümünde gösterilir; bu görüntülemeler denetim kaydına yazılır.
