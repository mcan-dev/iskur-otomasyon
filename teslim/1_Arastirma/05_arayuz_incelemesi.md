# Arayüz incelemesi: Selçuk Üniversitesi web sayfaları

Arayüzün kurumsal görünüme uyması için Selçuk Üniversitesi ana sayfası (selcuk.edu.tr) ve bağlı sayfaları
incelendi. Renkler ve yazı tipi tarayıcıdan okundu; sayfa düzeninden yalnızca işe yarayan öğeler alındı.
İlk sürümde fazla süslü bulunduğu için arayüz sadeleştirildi.

## Renk ve yazı tipi

| Öğe | Sitede | Otomasyonda kullanılan |
|---|---|---|
| Ana renk | Lacivert `#1c458b` | Düğmeler, bağlantılar, başlıklar |
| Koyu lacivert | `#0d2d5e` | Üst başlık, seçili menü, tablo başlığı |
| Vurgu rengi | Sarı `#ffc700` | Başlık altı çizgisi, seçili menü şeridi, avatar |
| Sayfa zemini | Açık gri-mavi `#f6f8fc`, beyaz kartlar | Aynı |
| Yazı tipi | Montserrat | Montserrat |

## Düzen

- **Logo:** Üniversite logosu üst başlıkta ve giriş kapağında kullanılır (`frontend/public/logo.png`).
- **Üst başlık:** Lacivert, logo ve fakülte adı solda, kullanıcı menüsü (profil, çıkış) sağda.
- **Sol menü:** Rolün işlemleri simgeli liste olarak durur; daraltılabilir, dar ekranda çekmeceye dönüşür.
- **Ana sayfa:** Karşılama bandı, simgeli özet kartları, "Sizi Bekleyenler" ve "Hızlı Erişim". Başka bölüm yoktur.
- **Giriş, kayıt, parola sayfaları:** Solda Selçuk Üniversitesi kapağı (logo ve ad), sağda form. Her giriş sayfasında
  "Kayıt Ol" düğmesi bulunur.
- **Alt bilgi:** Tek satır.
- **Erişilebilirlik:** Sol altta yazı boyutu, yüksek karşıtlık ve bağlantı altı çizgisi ayarı (tercih tarayıcıda saklanır).

## Kararlar

- Sitedeki yan çubuk, arama, hızlı menü paneli ve üst bağlantı şeridi kullanılmadı; menü zaten solda olduğu için
  tekrar olurdu ve ekranı kalabalıklaştırırdı.
- Cep telefonu alanları yalnızca rakam kabul eder ve 11 haneyle sınırlıdır.
- Durum renkleri (yeşil onay, sarı bekleyen, kırmızı ret) belgedeki takvim renk önerisiyle uyumludur ve yalnızca renge
  dayanmaz; her durumun yanında yazı vardır.
- Teknoloji Fakültesi'nin kendi sayfası incelemeye açık olmadığı için fakülteye özgü içerik eklenmedi.
