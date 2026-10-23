import type { ReactNode } from 'react';
import { Accessibility, Logo } from './SiteChrome';

/**
 * Giriş, kayıt ve parola sayfalarının ortak çerçevesi.
 * Solda Selçuk Üniversitesi kapağı, sağda işlem formu bulunur.
 */
export default function AuthShell({ children, wide = false }: { children: ReactNode; wide?: boolean }) {
  return (
    <div className="public">
      <aside className="cover">
        <Logo size={150} />
        <h2>Selçuk Üniversitesi</h2>
        <p>Teknoloji Fakültesi</p>
        <span className="cover-sub">İŞKUR Otomasyonu</span>
      </aside>
      <main className="public-main">
        <div className={`public-form ${wide ? 'wide' : ''}`}>{children}</div>
      </main>
      <Accessibility />
    </div>
  );
}
