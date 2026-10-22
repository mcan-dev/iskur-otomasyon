import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Icon } from '../components/Icon';
import { menus, roleLabels } from './menu';
import { Accessibility, BackToTop, Brand, FloatingBar, SiteFooter, UtilityBar } from './SiteChrome';

export default function AppLayout() {
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [drawer, setDrawer] = useState(false);
  const [quick, setQuick] = useState(false);
  const [userMenu, setUserMenu] = useState(false);
  const [query, setQuery] = useState('');
  const location = useLocation();
  const navigate = useNavigate();
  const headerRef = useRef<HTMLElement>(null);

  // Sayfa değişince açık panelleri kapat.
  useEffect(() => {
    setQuick(false);
    setDrawer(false);
    setUserMenu(false);
    setQuery('');
  }, [location.pathname]);

  // Panelin dışına tıklanınca veya Esc ile kapat.
  useEffect(() => {
    const click = (e: MouseEvent) => {
      if (headerRef.current && !headerRef.current.contains(e.target as Node)) {
        setQuick(false);
        setUserMenu(false);
        setQuery('');
      }
    };
    const key = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setQuick(false);
        setUserMenu(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', click);
    document.addEventListener('keydown', key);
    return () => {
      document.removeEventListener('mousedown', click);
      document.removeEventListener('keydown', key);
    };
  }, []);

  const items = user ? menus[user.role] : [];
  const results = useMemo(() => {
    const q = query.trim().toLocaleLowerCase('tr');
    if (!q) return [];
    return items.filter((m) => (m.label + ' ' + m.desc).toLocaleLowerCase('tr').includes(q));
  }, [items, query]);

  if (!user) return null;

  const current = items.find((m) => (m.path === '/' ? location.pathname === '/' : location.pathname.startsWith(m.path)));
  const title = current?.label ?? (location.pathname.startsWith('/ogrenciler/') ? 'Öğrenci Bilgileri' : 'İŞKUR Otomasyonu');
  const initials = user.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0].toLocaleUpperCase('tr'))
    .join('');

  return (
    <div className={`shell ${collapsed ? 'collapsed' : ''} ${drawer ? 'drawer-open' : ''}`}>
      <UtilityBar />
      <header className="site-header" ref={headerRef}>
        <div className="header-in">
          <button className="icon-btn menu-toggle" onClick={() => setDrawer((d) => !d)} aria-label="Menüyü aç veya kapat">
            <Icon name="menu" size={22} />
          </button>
          <Brand />
          <div className="header-actions">
            <div className="search">
              <Icon name="search" size={18} />
              <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Sayfa ara…" aria-label="Menüde ara" onKeyDown={(e) => e.key === 'Enter' && results[0] && navigate(results[0].path)} />
              {query.trim() && (
                <ul className="search-list">
                  {results.length === 0 && <li className="muted small">Sonuç bulunamadı</li>}
                  {results.map((m) => (
                    <li key={m.path}>
                      <Link to={m.path}>
                        <Icon name={m.icon} size={18} />
                        {m.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <button className="quick-btn" onClick={() => setQuick((q) => !q)} aria-expanded={quick}>
              <Icon name="menu" size={18} />
              HIZLI MENÜ
            </button>
            <div className="user-menu">
              <button className="user-btn" onClick={() => setUserMenu((u) => !u)} aria-expanded={userMenu}>
                <span className="avatar">{initials}</span>
                <span className="user-text">
                  <strong>{user.fullName}</strong>
                  <span>{roleLabels[user.role]}</span>
                </span>
                <Icon name="chevron" size={16} />
              </button>
              {userMenu && (
                <div className="user-drop">
                  <div className="user-drop-head">
                    <strong>{user.fullName}</strong>
                    <span>{user.email}</span>
                    <span>{user.departmentName ?? 'Teknoloji Fakültesi'}</span>
                  </div>
                  {user.role === 'Student' && (
                    <Link to="/profil">
                      <Icon name="user" size={18} /> Profilim
                    </Link>
                  )}
                  <button onClick={logout}>
                    <Icon name="logout" size={18} /> Çıkış yap
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
        {quick && (
          <div className="quick-panel">
            <div className="quick-panel-in">
              <h2>{roleLabels[user.role]} işlemleri</h2>
              <div className="quick-grid">
                {items.map((m) => (
                  <Link key={m.path} to={m.path} className="quick-card">
                    <span className="tile">
                      <Icon name={m.icon} size={22} />
                    </span>
                    <span>
                      <strong>{m.label}</strong>
                      <small>{m.desc}</small>
                    </span>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )}
      </header>

      <div className="body">
        <aside className="sidebar">
          <nav>
            {items.map((m) => (
              <NavLink key={m.path} to={m.path} end={m.path === '/'} className={({ isActive }) => (isActive ? 'nav-item active' : 'nav-item')} title={m.label}>
                <span className="nav-icon">
                  <Icon name={m.icon} size={19} />
                </span>
                <span className="nav-label">{m.label}</span>
              </NavLink>
            ))}
          </nav>
          <button className="link-btn collapse-btn" onClick={() => setCollapsed((c) => !c)}>
            {collapsed ? '»' : '« Menüyü daralt'}
          </button>
        </aside>
        {drawer && <div className="drawer-back" onClick={() => setDrawer(false)} />}

        <div className="main">
          <div className="page-head">
            {location.pathname !== '/' && (
              <div className="crumbs">
                <Link to="/">Ana Sayfa</Link>
                <span>›</span>
                <span>{title}</span>
              </div>
            )}
            <h1>{title}</h1>
          </div>
          <main className="content">
            <Outlet />
          </main>
        </div>
      </div>

      <SiteFooter />
      <FloatingBar>
        <Link to="/" aria-label="Ana sayfa" title="Ana sayfa">
          <Icon name="home" size={20} />
        </Link>
        <button onClick={() => setQuick(true)} aria-label="Hızlı menü" title="Hızlı menü">
          <Icon name="menu" size={20} />
        </button>
        <a href="https://selcuk.edu.tr/" target="_blank" rel="noreferrer" aria-label="Selçuk Üniversitesi sitesi" title="Selçuk Üniversitesi">
          <Icon name="globe" size={20} />
        </a>
        <button onClick={() => window.print()} aria-label="Yazdır" title="Yazdır">
          <Icon name="file" size={20} />
        </button>
      </FloatingBar>
      <Accessibility />
      <BackToTop />
    </div>
  );
}
