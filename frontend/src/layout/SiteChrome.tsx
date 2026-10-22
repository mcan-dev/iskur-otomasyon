import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { siteLinks } from './links';

const dateText = () => new Date().toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' });

/** Sayfanın en üstündeki ince bağlantı çubuğu. */
export function UtilityBar() {
  return (
    <div className="utility">
      <div className="utility-in">
        <span className="utility-date">{dateText()}</span>
        <nav aria-label="Üniversite bağlantıları">
          {siteLinks.map((l) => (
            <a key={l.href} href={l.href} target="_blank" rel="noreferrer">
              {l.label}
            </a>
          ))}
        </nav>
      </div>
    </div>
  );
}

export function Brand({ to = '/' }: { to?: string }) {
  return (
    <Link to={to} className="brand-link">
      <span className="emblem">S</span>
      <span className="brand-text">
        <strong>SELÇUK ÜNİVERSİTESİ</strong>
        <span>Teknoloji Fakültesi · İŞKUR Otomasyonu</span>
      </span>
    </Link>
  );
}

/** Alt bilgi: iletişim kartları ve bağlantılar. */
export function SiteFooter() {
  return (
    <footer className="site-footer">
      <div className="footer-in">
        <div className="footer-brand">
          <span className="emblem big">S</span>
          <strong>Selçuk Üniversitesi</strong>
          <span>Teknoloji Fakültesi</span>
          <span className="muted-light">İŞKUR öğrenci programı otomasyonu</span>
        </div>
        <div className="footer-card wide">
          <h3>Rektörlük</h3>
          <p>Selçuk Üniversitesi Rektörlüğü, Alaaddin Keykubat Yerleşkesi, Akademi Mah. Yeni İstanbul Cad. No:369 Posta Kodu 42130 Selçuklu / Konya</p>
        </div>
        <div className="footer-card">
          <h3>Kampüs Santral</h3>
          <p>0 (332) 241 00 41</p>
          <p>0 (332) 223 82 65</p>
        </div>
        <div className="footer-card">
          <h3>Bağlantılar</h3>
          <ul>
            {siteLinks.map((l) => (
              <li key={l.href}>
                <a href={l.href} target="_blank" rel="noreferrer">
                  {l.label}
                </a>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <div className="footer-bottom">© {new Date().getFullYear()} Selçuk Üniversitesi Teknoloji Fakültesi · İŞKUR Otomasyonu</div>
    </footer>
  );
}

type Prefs = { size: number; contrast: boolean; underline: boolean };
const defaults: Prefs = { size: 0, contrast: false, underline: false };
const key = 'iskur.erisilebilirlik';

function readPrefs(): Prefs {
  try {
    return { ...defaults, ...JSON.parse(localStorage.getItem(key) ?? '{}') };
  } catch {
    return defaults;
  }
}

function applyPrefs(p: Prefs) {
  const root = document.documentElement;
  root.dataset.size = String(p.size);
  root.dataset.contrast = p.contrast ? 'high' : '';
  root.dataset.underline = p.underline ? 'on' : '';
}

/** Sol alttaki erişilebilirlik düğmesi: yazı boyutu, yüksek karşıtlık, bağlantı altı çizgisi. */
export function Accessibility() {
  const [open, setOpen] = useState(false);
  const [prefs, setPrefs] = useState<Prefs>(readPrefs);

  useEffect(() => {
    applyPrefs(prefs);
    try {
      localStorage.setItem(key, JSON.stringify(prefs));
    } catch {
      /* tarayıcı depolamayı kapatmış olabilir */
    }
  }, [prefs]);

  const set = (patch: Partial<Prefs>) => setPrefs((p) => ({ ...p, ...patch }));
  return (
    <div className="a11y">
      {open && (
        <div className="a11y-panel" role="dialog" aria-label="Erişilebilirlik">
          <h3>Erişilebilirlik</h3>
          <div className="a11y-row">
            <span>Yazı boyutu</span>
            <div className="a11y-btns">
              <button onClick={() => set({ size: Math.max(-1, prefs.size - 1) })} aria-label="Yazıyı küçült">A−</button>
              <button onClick={() => set({ size: 0 })} aria-label="Yazı boyutunu sıfırla">A</button>
              <button onClick={() => set({ size: Math.min(2, prefs.size + 1) })} aria-label="Yazıyı büyüt">A+</button>
            </div>
          </div>
          <label className="a11y-row">
            <span>Yüksek karşıtlık</span>
            <input type="checkbox" checked={prefs.contrast} onChange={(e) => set({ contrast: e.target.checked })} />
          </label>
          <label className="a11y-row">
            <span>Bağlantıların altını çiz</span>
            <input type="checkbox" checked={prefs.underline} onChange={(e) => set({ underline: e.target.checked })} />
          </label>
          <button className="btn small" onClick={() => setPrefs(defaults)}>
            Varsayılana dön
          </button>
        </div>
      )}
      <button className="a11y-btn" onClick={() => setOpen((o) => !o)} aria-label="Erişilebilirlik ayarları" aria-expanded={open}>
        <Icon name="access" size={22} />
      </button>
    </div>
  );
}

/** Sağ alttaki yukarı çık düğmesi. */
export function BackToTop() {
  const [show, setShow] = useState(false);
  useEffect(() => {
    const on = () => setShow(window.scrollY > 300);
    on();
    window.addEventListener('scroll', on, { passive: true });
    return () => window.removeEventListener('scroll', on);
  }, []);
  if (!show) return null;
  return (
    <button className="to-top" onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })} aria-label="Yukarı çık">
      <Icon name="up" size={22} />
    </button>
  );
}

/** Sağ kenardaki kayan hızlı erişim çubuğu. */
export function FloatingBar({ children }: { children: ReactNode }) {
  return <div className="float-bar">{children}</div>;
}
