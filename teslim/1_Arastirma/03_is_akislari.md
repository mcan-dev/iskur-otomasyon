# İş akışları

Akışlar Mermaid ile yazılmıştır; GitHub ve Visual Studio Code (Markdown önizleme) üzerinde çizim olarak görünür.

## 1. Öğrenci kaydı

```mermaid
flowchart TD
    A[Öğrenci kayıt formunu açar] --> B[Ad, TC, bölüm, öğrenci no,<br/>e-posta, telefon, kayıt yılı girer]
    B --> C{Doğrulama:<br/>TC geçerli mi, e-posta ve<br/>öğrenci no daha önce var mı?}
    C -- Hayır --> D[Hata mesajı, form düzeltilir]
    D --> B
    C -- Evet --> E[Kullanıcı hesabı + öğrenci profili +<br/>ilk program katılım dönemi oluşur]
    E --> F[Fakülte sekreterliği öğrenciyi<br/>çalışma birimine atar]
    F --> G[Bölüm başkanı veya sekreterlik<br/>danışman atar]
    G --> H[Profilde birim ve danışman görünür]
```

İlk kayıtta birim ve danışman girilmez; belgede istendiği gibi atamalar yapıldıktan sonra profilde görünür.
Her atama tarihli bir satırdır; değiştirilince eski satır kapanır, geçmiş korunur.

## 2. Aylık çalışma planı

```mermaid
flowchart TD
    A[Öğrenci ay ve yılı seçer] --> B[Takvim açılır:<br/>yeşil uygun, kırmızı tatil, sarı ders/sınav, gri kapalı]
    B --> C[Çalışacağı günleri seçer<br/>her gün 8 saat]
    C --> D{Kurallar}
    D -- "Haftada 1-3 gün değil<br/>(sınav haftası hariç)" --> X[Uyarı, kaydedilmez]
    D -- Tatil / hafta sonu seçili --> X
    D -- "Dersli gün seçili,<br/>açıklama yok" --> X
    D -- Uygun --> E[Plan kaydedilir: durum Bekliyor]
    X --> C
    E --> F[Düzeltme son gününe kadar<br/>öğrenci planı değiştirebilir]
    F --> G{Fakülte sekreterliği}
    G -- Onaylar --> H[Plan Onaylandı, kilitlenir]
    G -- Gerekçeyle reddeder --> I[Öğrenci yeni plan girer]
    I --> C
```

Hafta, perşembe gününün düştüğü aya sayılır. Önceki ayların planları ayrı kayıt olarak korunur
(öğrenci, yıl, ay üçlüsü benzersizdir).

## 3. Danışman ataması

```mermaid
flowchart LR
    A[Bölüm başkanı veya sekreterlik<br/>öğrenciyi seçer] --> B[Danışmanı seçer<br/>aynı bölümden olmalı]
    B --> C{Öğrencinin açık<br/>danışmanı var mı?}
    C -- Evet --> D[Eski atamanın bitiş tarihi yazılır]
    C -- Hayır --> E
    D --> E[Yeni atama başlar]
    E --> F[Denetim kaydı yazılır]
```

## 4. İzin

```mermaid
flowchart TD
    A[Öğrenci izin formunu açar] --> B[Tarih aralığı veya saat, tür,<br/>gerekçe, varsa belge]
    B --> C[Sistem gün sayısını hesaplar:<br/>planlı günler, tatiller düşülür]
    C --> D[Kalan izin hakkı ve uyarılar formda gösterilir]
    D --> E[Talep Bekliyor durumunda kaydedilir]
    E --> F{Fakülte sekreterliği}
    F -- Onaylar --> G[Kullanılan izin artar, kalan azalır]
    F -- Reddeder --> H[Hak değişmez]
    E --> I[Öğrenci onaydan önce talebi iptal edebilir]
    G --> J{Sınıra yaklaştı mı?}
    J -- Evet --> K[Uyarı: öğrenci, sekreterlik ekranlarında]
```

Toplam izin hakkı (varsayılan 10 gün), uyarı eşikleri ve sağlık izninin haktan düşüp düşmeyeceği ayarlardan
değiştirilir; kod değişmez.

## 5. Karekodla devam

```mermaid
sequenceDiagram
    participant Y as Yetkili (sekreterlik / bölüm başkanı)
    participant S as Sistem
    participant Ö as Öğrenci
    Y->>S: Gün ve saat aralığı ile oturum açar
    loop her 30 saniye
        S-->>Y: Yeni karekod (oturum no + pencere + imza)
    end
    Ö->>S: Giriş yapar, karekodu okutur
    S->>S: Jeton imzası ve süresi, oturum açık mı,<br/>öğrenci kimliği, onaylı planda bugün var mı,<br/>izinli mi, daha önce kayıt var mı?
    alt hepsi uygun
        S-->>Ö: Devam kaydedildi
    else bir kontrol başarısız
        S-->>Ö: Reddedilme sebebi yazılır
    end
    Y->>S: Gerekirse elle kayıt veya geri alma (gerekçe zorunlu)
```

Karekod fotoğrafı bir sonraki pencerede geçersizdir. Aynı öğrenci aynı oturumda ikinci kez kayıt yapamaz.
Planlı ama devamı olmayan gün kendiliğinden devamsızlık sayılmaz; karşılaştırmada "kayıt yok" görünür ve devamsızlığı
fakülte yönetimi girer.

## 6. Raporlama

```mermaid
flowchart TD
    A[Yetkili Raporlar ekranını açar] --> B[Rapor türünü seçer]
    B --> C[Filtreler: yıl, ay, bölüm, danışman, çalışma durumu]
    C --> D[Tablo ekranda görünür]
    D --> E{Dışa aktar}
    E -- Excel --> F[.xlsx]
    E -- CSV --> G[.csv]
    D --> H[Ay sonu dökümü<br/>yalnızca fakülte sekreterliği:<br/>TC, ad, IBAN, bölüm, birim, ay-yıl,<br/>planlanan ve gerçekleşen gün, saat, izin]
```

Çalışma saati = onaylı plandaki günlerden devam kaydı olanların sayısı × 8 saat.

## 7. Görev ve değerlendirme

```mermaid
flowchart LR
    A[Danışman / bölüm başkanı / sekreterlik<br/>öğrenciye görev verir] --> B[Başlık, açıklama, hedef tarih]
    B --> C[Öğrenci görevi ekranında görür]
    C --> D[Danışman not ekler ve durumu<br/>Tamamlandı / Tamamlanmadı yapar]
    D --> E[Her değerlendirme tarihiyle ayrı satır olarak saklanır]
    E --> F[Bölüm başkanı görev ve notları görüntüler]
```

## 8. Devamsızlık bildirimi ve ilişik kesme

```mermaid
flowchart TD
    A[Danışman gelmeyen günü bildirir] --> B[Bölüm başkanı bildirimi inceler,<br/>okundu işaretler]
    B --> C[Gerekirse dekanlığa bildirir]
    D[Sistem sınıra yaklaşan veya aşan<br/>öğrencileri listeler] --> E[Fakülte sekreterliği inceler]
    C --> E
    E --> F{Karar}
    F -- İlişik kesilecek --> G[Dayanak ve gerekçeyle karar kaydı girilir]
    G --> H[Öğrencinin katılımı sona erer,<br/>öğrenci pasif olur, geçmiş kayıtlar durur]
    F -- Kesilmeyecek --> I[İzlemeye devam]
```

Sistem yalnızca inceleme listesi üretir; karar yetkili kişiye aittir.
