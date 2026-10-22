import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import AppLayout from './layout/AppLayout';
import { menus } from './layout/menu';
import Dashboard from './pages/Dashboard';
import ForgotPassword from './pages/ForgotPassword';
import Login from './pages/Login';
import AbsencesPage from './pages/staff/AbsencesPage';
import AuditPage from './pages/staff/AuditPage';
import AttendancePage from './pages/staff/AttendancePage';
import DefinitionsPage from './pages/staff/DefinitionsPage';
import LeavesStaffPage from './pages/staff/LeavesStaffPage';
import NoticesPage from './pages/staff/NoticesPage';
import PlansReviewPage from './pages/staff/PlansReviewPage';
import ReportsPage from './pages/staff/ReportsPage';
import SessionsPage from './pages/staff/SessionsPage';
import StudentDetailPage from './pages/staff/StudentDetailPage';
import StudentsPage from './pages/staff/StudentsPage';
import TerminationsPage from './pages/staff/TerminationsPage';
import TasksStaffPage from './pages/staff/TasksStaffPage';
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

  // Menüdeki her adresin ekranı burada tanımlıdır.
  const screens: Record<string, JSX.Element> = {
    '/plan': <PlanPage />,
    '/izinlerim': <LeavesPage />,
    '/devam-kaydi': <CheckInPage />,
    '/gorevlerim': <TasksPage />,
    '/gecmis': <HistoryPage />,
    '/profil': <ProfilePage />,
    '/ogrenciler': <StudentsPage />,
    '/plan-onaylari': <PlansReviewPage />,
    '/izinler': <LeavesStaffPage />,
    '/devamsizlik': <AbsencesPage />,
    '/devam-oturumlari': <SessionsPage />,
    '/devam': <AttendancePage />,
    '/gorevler': <TasksStaffPage />,
    '/bildirimler': <NoticesPage />,
    '/raporlar': <ReportsPage />,
    '/iliski-kesme': <TerminationsPage />,
    '/tanimlar': <DefinitionsPage />,
    '/denetim': <AuditPage />,
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
          <Route key={p} path={p} element={screens[p] ?? <Navigate to="/" replace />} />
        ))}
        {user && user.role !== 'Student' && <Route path="/ogrenciler/:id" element={<StudentDetailPage />} />}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
