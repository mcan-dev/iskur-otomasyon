import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { menus, roleLabels } from './menu';

export default function AppLayout() {
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  if (!user) return null;

  const items = menus[user.role];
  const current = items.find((m) => (m.path === '/' ? location.pathname === '/' : location.pathname.startsWith(m.path)));
  const initials = user.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0].toLocaleUpperCase('tr'))
    .join('');

  return (
    <div className={`shell ${collapsed ? 'collapsed' : ''}`}>
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">İ</div>
          <div className="brand-text">
            <strong>SELÇUK ÜNİVERSİTESİ</strong>
            <span>Teknoloji Fakültesi · İŞKUR</span>
          </div>
        </div>
        <nav>
          {items.map((m) => (
            <NavLink key={m.path} to={m.path} end={m.path === '/'} className={({ isActive }) => (isActive ? 'nav-item active' : 'nav-item')}>
              <span className="nav-icon">{m.icon}</span>
              <span className="nav-label">{m.label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-foot">
          <div className="user-chip">
            <div className="avatar">{initials}</div>
            <div className="user-text">
              <strong>{user.fullName}</strong>
              <span>{roleLabels[user.role]}</span>
            </div>
          </div>
          <button className="link-btn" onClick={() => setCollapsed((c) => !c)}>
            {collapsed ? '»' : '« Menüyü Daralt'}
          </button>
        </div>
      </aside>

      <div className="main">
        <header className="topbar">
          <div>
            <h1>{current?.label ?? 'İŞKUR Otomasyonu'}</h1>
            <p>{user.departmentName ?? 'Teknoloji Fakültesi'}</p>
          </div>
          <div className="topbar-right">
            <span className="role-pill">{roleLabels[user.role]}</span>
            <button className="btn ghost" onClick={logout}>
              Çıkış
            </button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
