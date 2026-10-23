import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Icon } from '../components/Icon';
import { menus, roleLabels } from './menu';
import { Accessibility, BackToTop, Brand, SiteFooter } from './SiteChrome';

export default function AppLayout() {
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [drawer, setDrawer] = useState(false);
  const [userMenu, setUserMenu] = useState(false);
  const location = useLocation();
  const menuRef = useRef<HTMLDivElement>(null);

  // Sayfa değişince açık panelleri kapat.
  useEffect(() => {
    setDrawer(false);
    setUserMenu(false);
  }, [location.pathname]);

  // Kullanıcı menüsü dışına tıklanınca veya Esc ile kapanır.
  useEffect(() => {
    const click = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setUserMenu(false);
    };
    const key = (e: KeyboardEvent) => e.key === 'Escape' && setUserMenu(false);
    document.addEventListener('mousedown', click);
    document.addEventListener('keydown', key);
    return () => {
      document.removeEventListener('mousedown', click);
      document.removeEventListener('keydown', key);
    };
  }, []);

  if (!user) return null;

  const items = menus[user.role];
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
      <header className="site-header">
        <div className="header-in">
          <button className="icon-btn menu-toggle" onClick={() => setDrawer((d) => !d)} aria-label="Menüyü aç veya kapat">
            <Icon name="menu" size={22} />
          </button>
          <Brand />
          <div className="header-actions">
            <div className="user-menu" ref={menuRef}>
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
            <h1>{title}</h1>
          </div>
          <main className="content">
            <Outlet />
          </main>
        </div>
      </div>

      <SiteFooter />
      <Accessibility />
      <BackToTop />
    </div>
  );
}
