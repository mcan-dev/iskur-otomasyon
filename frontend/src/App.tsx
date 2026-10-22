import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import AppLayout from './layout/AppLayout';
import { menus } from './layout/menu';
import Dashboard from './pages/Dashboard';
import ForgotPassword from './pages/ForgotPassword';
import Login from './pages/Login';
import Placeholder from './pages/Placeholder';
import Register from './pages/Register';
import ResetPassword from './pages/ResetPassword';

export default function App() {
  const { user, loading } = useAuth();
  if (loading) return <div className="splash">Yükleniyor…</div>;

  // Menüdeki yollar yalnızca ilgili rol için tanımlanır; başka rolün adresi ana sayfaya döner.
  const paths = user ? menus[user.role].filter((m) => m.path !== '/').map((m) => m.path) : [];

  return (
    <Routes>
      <Route path="/giris" element={<Login />} />
      <Route path="/kayit" element={<Register />} />
      <Route path="/parolami-unuttum" element={<ForgotPassword />} />
      <Route path="/sifre-sifirla" element={<ResetPassword />} />
      <Route element={user ? <AppLayout /> : <Navigate to="/giris" replace />}>
        <Route index element={<Dashboard />} />
        {paths.map((p) => (
          <Route key={p} path={p} element={<Placeholder />} />
        ))}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
