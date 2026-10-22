# Arayüz incelemesi: Selçuk Üniversitesi web sayfaları

Arayüzün kurumsal görünüme uyması için Selçuk Üniversitesi ana sayfası (selcuk.edu.tr) incelendi. Sayfanın
renkleri ve yazı tipi tarayıcıdan okunarak aşağıdaki değerler elde edildi.

| Öğe | Sitede | Otomasyonda kullanılan |
|---|---|---|
| Üst şerit ve ana renk | Lacivert `#1c458b` | Düğmeler, seçili menü, bağlantılar |
| Koyu lacivert | `#0d2d5e` | Yan menü, giriş sayfası arka planı |
| Vurgu rengi | Sarı `#ffc700` (Hızlı Menü düğmesi, vurgu çizgileri) | Seçili menü şeridi, avatar, giriş kartı üst çizgisi |
| Sayfa zemini | Açık gri-mavi `#f6f8fc`, beyaz kartlar | Aynı |
| Metin | `#1e293b` (başlık), `#475569` (ikincil) | Aynı |
| Kenarlık | `#cbd5e1` civarı | `#dde4ef` |
| Yazı tipi | Montserrat | Montserrat |
| Menü yapısı | Üstte logo ve "Hızlı Menü"; Üniversitemiz, Akademik, Personel, Öğrenci… | Yan menü (rol menüsü) |

## Kararlar

- Üniversitenin logosu ve görselleri kopyalanmadı. Yan menü ve giriş ekranında yer tutucu bir simge kullanılır.
  Resmî logo kullanılacaksa `frontend/public/logo.png` olarak eklenip `AppLayout.tsx` içindeki simgenin yerine konabilir.
- Yan menü, günlük işlerde çok sayıda ekran arasında geçiş gerektiği için üst menü yerine seçildi; renk ve yazı
  tipi üniversite sitesinden alındı.
- Durum renkleri (yeşil onay, sarı bekleyen, kırmızı ret) belgedeki takvim renk önerisiyle uyumludur ve yalnızca renge
  dayanmaz; her durumun yanında yazı vardır.
- Arayüz dar ekranlarda tek sütuna iner.
