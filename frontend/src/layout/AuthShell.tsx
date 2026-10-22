import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { Accessibility, Brand, SiteFooter, UtilityBar } from './SiteChrome';

const features = [
  { icon: 'calendar', text: 'Aylık çalışma planı ve takvim' },
  { icon: 'qr', text: 'Karekod ile günlük devam kaydı' },
  { icon: 'mail', text: 'İzin ve devamsızlık takibi' },
  { icon: 'tasks', text: 'Görev verme ve değerlendirme' },
];

/** Giriş, kayıt ve parola sayfalarının ortak çerçevesi: üst başlık, içerik, alt bilgi. */
export default function AuthShell({ children, side = true, wide = false }: { children: ReactNode; side?: boolean; wide?: boolean }) {
  return (
    <div className="public">
      <UtilityBar />
      <header className="site-header">
        <div className="header-in">
          <Brand to="/giris" />
          <div className="header-actions">
            <Link to="/giris" className="quick-btn">
              <Icon name="user" size={18} />
              GİRİŞ TÜRLERİ
            </Link>
          </div>
        </div>
      </header>
      <main className={`public-main ${side ? 'with-side' : ''}`}>
        {side && (
          <section className="public-side">
            <span className="tag">Teknoloji Fakültesi</span>
            <h2>İŞKUR Öğrenci Programı Otomasyonu</h2>
            <p>Programa katılan öğrencilerin çalışma planı, devam, izin ve görev süreçleri tek yerden yürütülür.</p>
            <ul>
              {features.map((f) => (
                <li key={f.text}>
                  <span className="tile">
                    <Icon name={f.icon} size={20} />
                  </span>
                  {f.text}
                </li>
              ))}
            </ul>
          </section>
        )}
        <div className={`public-form ${wide ? 'wide' : ''}`}>{children}</div>
      </main>
      <SiteFooter />
      <Accessibility />
    </div>
  );
}
