# Arayüz incelemesi: Selçuk Üniversitesi web sayfaları

Arayüzün kurumsal görünüme uyması için Selçuk Üniversitesi ana sayfası (selcuk.edu.tr) ve ona bağlı sayfalar
(Akademik birimler, etkinlik sitesi) incelendi. Renkler ve yazı tipi tarayıcıdan okundu; sayfa düzeni
(üst şerit, menü, haber ve duyuru bölümleri, alt bilgi) gözlemlenerek otomasyona uyarlandı.

## Renk ve yazı tipi

| Öğe | Sitede | Otomasyonda kullanılan |
|---|---|---|
| Üst şerit ve ana renk | Lacivert `#1c458b` | Düğmeler, bağlantılar, başlıklar |
| Koyu lacivert | `#0d2d5e` | Üst başlık, seçili menü, tablo başlığı |
| Vurgu rengi | Sarı `#ffc700` (Hızlı Menü düğmesi, vurgu çizgileri) | Hızlı Menü düğmesi, başlık altı çizgisi, seçili menü şeridi |
| Sayfa zemini | Açık gri-mavi `#f6f8fc`, beyaz kartlar | Aynı |
| Metin | `#1e293b` (başlık), `#475569` (ikincil) | Aynı |
| Kenarlık | `#cbd5e1` civarı | `#dde4ef` |
| Yazı tipi | Montserrat | Montserrat |

## Sitedeki düzen ve otomasyondaki karşılığı

| Sitede gözlenen | Otomasyonda |
|---|---|
| En üstte ince koyu şerit ve bağlantılar | Tarih ve üniversite bağlantıları (Selçuk Üniversitesi, Akademik Birimler, Etkinlikler, İŞKUR) |
| Lacivert başlık, ortada amblem, solda sarı yuvarlak "HIZLI MENÜ" düğmesi | Lacivert yapışkan başlık; sarı "HIZLI MENÜ" düğmesi rolün tüm işlemlerini açıklamalı kartlar olarak açar |
| Arama simgesi | Başlıkta "Sayfa ara" kutusu; menü sayfalarını bulur, Enter ile ilkine gider |
| Sağ kenarda kayan sosyal medya çubuğu | Sağ kenarda kayan çubuk: ana sayfa, hızlı menü, üniversite sitesi, yazdır |
| Sol altta sarı erişilebilirlik düğmesi | Aynı yerde erişilebilirlik düğmesi: yazı boyutu, yüksek karşıtlık, bağlantı altı çizgisi (tercih tarayıcıda saklanır) |
| Sağ altta yukarı çık düğmesi | Aynı |
| "Genel Duyurular", "Öğrenci Duyuruları" bölümleri; mavi başlık, sağda çerçeveli "TÜMÜ →" düğmesi | Ana sayfada "Sizi Bekleyenler", "Bilgilendirme", "Bağlantılar" bölümleri aynı başlık ve düğme biçimiyle |
| Duyuruların solunda mavi tarih kutusu | Ana sayfada bugünün tarih kutusu; bilgilendirme maddelerinde mavi simge kutuları |
| Koyu lacivert alt bilgi, sarı başlıklı iletişim kartları | Aynı yapıda alt bilgi: Rektörlük adresi, kampüs santral, bağlantılar |

Öğrenci panosunda çalıştığı birim, danışman, kalan izin hakkı ve devamsızlık özeti; çalışma planı durumu, açık görev
ve bekleyen izin sayıları gösterilir. Personel panolarında aktif öğrenci, bekleyen izin, onay bekleyen plan
(fakülte) veya okunmamış bildirim (danışman, bölüm başkanı) ve sınıra yaklaşan öğrenciler yer alır.

## Kararlar

- Üniversitenin logosu ve görselleri kopyalanmadı. Amblem yerine yuvarlak çerçeveli "S" harfi kullanılır.
  Resmî logo kullanılacaksa `frontend/public/logo.png` olarak eklenip `frontend/src/layout/SiteChrome.tsx`
  içindeki `emblem` öğelerinin yerine konabilir.
- Sol yan menü korundu: günlük işlerde çok sayıda ekran arasında geçiş gerekir. Üniversite sitesindeki
  "Hızlı Menü" düğmesi bunun yanına eklendi, ikisi aynı menü listesini kullanır.
- Giriş, kayıt ve parola sayfaları da aynı üst başlık ve alt bilgiyi taşır; giriş sayfasında programın ne yaptığını
  anlatan lacivert bir tanıtım alanı vardır.
- Durum renkleri (yeşil onay, sarı bekleyen, kırmızı ret) belgedeki takvim renk önerisiyle uyumludur ve yalnızca renge
  dayanmaz; her durumun yanında yazı vardır.
- Dar ekranlarda yan menü açılır çekmeceye, sayfalar tek sütuna iner.
- Teknoloji Fakültesi'nin kendi sayfası incelemeye açık olmadığı için fakülteye özgü içerik eklenmedi; fakülte adı
  başlıkta ve sayfa metinlerinde yer alır.
