import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import AppLayout from './layout/AppLayout';
import { menus } from './layout/menu';
import Dashboard from './pages/Dashboard';
import ForgotPassword from './pages/ForgotPassword';
import Login from './pages/Login';
import Placeholder from './pages/Placeholder';
import CheckInPage from './pages/student/CheckInPage';
import HistoryPage from './pages/student/HistoryPage';
import LeavesPage from './pages/student/LeavesPage';
import PlanPage from './pages/student/PlanPage';
import ProfilePage from './pages/student/ProfilePage';
import TasksPage from './pages/student/TasksPage';
import Register from './pages/Register';
import ResetPassword from './pages/ResetPassword';

export default function App() {
  const { user, loading } = useAuth();
  if (loading) return <div className="splash">Yükleniyor…</div>;

  // Gerçek ekranı yazılmış adresler; geri kalanlar yer tutucu sayfaya gider.
  const screens: Record<string, JSX.Element> = {
    '/plan': <PlanPage />,
    '/izinlerim': <LeavesPage />,
    '/devam-kaydi': <CheckInPage />,
    '/gorevlerim': <TasksPage />,
    '/gecmis': <HistoryPage />,
    '/profil': <ProfilePage />,
  };

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
          <Route key={p} path={p} element={(user?.role === 'Student' && screens[p]) || <Placeholder />} />
        ))}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
