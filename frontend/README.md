# Arayüz

React 18, TypeScript ve Vite ile yazılmıştır. Arka yüzdeki API'yi kullanır.

## Çalıştırma

Önce arka yüzü başlatın (`dotnet run --project src/IskurAuto.API`, varsayılan adres `http://localhost:5231`), sonra:

```bash
cd frontend
npm install
npm run dev
```

Arayüz `http://localhost:5173` adresinde açılır. Geliştirmede `/api` istekleri `vite.config.ts` içindeki yönlendirme ile
arka yüze gider, bu yüzden CORS ayarı gerekmez. Arka yüz farklı bir portta çalışıyorsa hedef adresi orada değiştirin.

Development ortamındaki demo hesaplarla (README'deki liste) giriş yapabilirsiniz.

## Üretim derlemesi

```bash
npm run build
```

`dist` klasörü çıkar. Arayüz ve API aynı alan adı altında yayınlanacaksa `/api` yolu ters vekil ile API'ye yönlendirilir.
Farklı bir adreste yayınlanacaksa o adres arka yüzde `Cors:AllowedOrigins` içine eklenir.

## Yapı

| Klasör | İçerik |
|---|---|
| `src/api` | `client.ts` (istek ve hata yönetimi), `types.ts` (API tipleri) |
| `src/auth` | Oturum bilgisi; jeton `sessionStorage` içinde tutulur, sekme kapanınca silinir |
| `src/layout` | Yan menü, üst çubuk, rol bazlı menü listeleri (`menu.ts`) |
| `src/pages` | Ekranlar |
| `src/components` | Ortak küçük bileşenler (kart, alan, uyarı) |

Menüler gereksinim dokümanının 2. bölümündeki yetkilere göre rol başına ayrı tanımlanır. Bir rolün menüsünde olmayan adres ana sayfaya yönlendirilir.
Asıl yetki denetimi arka yüzdedir; menü yalnızca kullanıcıya uygun ekranları gösterir.
