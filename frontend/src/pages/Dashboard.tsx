import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { LeaveAlert, LeaveBalance, Paged, StudentDetail, StudentSummary } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Alert, Card, Stat } from '../components/ui';
import { menus, roleLabels } from '../layout/menu';

export default function Dashboard() {
  const { user } = useAuth();
  if (!user) return null;
  return (
    <>
      <div className="hero">
        <span className="tag">{roleLabels[user.role]}</span>
        <h2>Merhaba, {user.fullName}</h2>
        <p>Teknoloji Fakültesi İŞKUR öğrenci programı: plan, devam, izin, görev ve raporlar tek yerde.</p>
      </div>
      {user.role === 'Student' ? <StudentSummary /> : <StaffSummary />}
      <Card title="Hızlı erişim">
        <div className="quick">
          {menus[user.role]
            .filter((m) => m.path !== '/')
            .map((m) => (
              <Link key={m.path} to={m.path} className="quick-item">
                <span className="nav-icon">{m.icon}</span>
                {m.label}
              </Link>
            ))}
        </div>
      </Card>
    </>
  );
}

function StudentSummary() {
  const [profile, setProfile] = useState<StudentDetail | null>(null);
  const [balance, setBalance] = useState<LeaveBalance | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    api.get<StudentDetail>('/api/students/me').then(setProfile).catch(() => setFailed(true));
    api.get<LeaveBalance>('/api/leaves/me/balance').then(setBalance).catch(() => setFailed(true));
  }, []);

  return (
    <>
      {failed && <Alert kind="warn">Bazı bilgiler yüklenemedi.</Alert>}
      <div className="stats">
        <Stat label="Çalıştığı birim" value={profile?.currentUnit?.workUnitName ?? 'Atanmadı'} tone="blue" />
        <Stat label="Danışman" value={profile?.currentAdvisor?.advisorName ?? 'Atanmadı'} tone="blue" />
        <Stat label="Kalan izin hakkı" value={balance ? `${balance.remaining} gün` : '–'} hint={balance ? `${balance.year} yılı, ${balance.limit} gün üzerinden` : undefined} tone="green" />
        <Stat label="Mazeretsiz devamsızlık" value={balance ? `${balance.unexcusedAbsenceDays} gün` : '–'} hint={balance ? `Sınır ${balance.absenceLimit} gün` : undefined} tone="amber" />
      </div>
      {balance?.warnings.map((w) => (
        <Alert key={w.code} kind="warn">
          {w.message}
        </Alert>
      ))}
    </>
  );
}

function StaffSummary() {
  const [total, setTotal] = useState<number | null>(null);
  const [alerts, setAlerts] = useState<LeaveAlert[] | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    api.get<Paged<StudentSummary>>('/api/students?page=1&pageSize=1&isActive=true').then((r) => setTotal(r.totalCount)).catch(() => setFailed(true));
    api.get<LeaveAlert[]>(`/api/leaves/alerts?year=${new Date().getFullYear()}`).then(setAlerts).catch(() => setFailed(true));
  }, []);

  return (
    <>
      {failed && <Alert kind="warn">Bazı bilgiler yüklenemedi.</Alert>}
      <div className="stats">
        <Stat label="Aktif öğrenci" value={total ?? '–'} hint="Yetki alanınızdaki kayıtlar" tone="blue" />
        <Stat label="Sınıra yaklaşan / aşan" value={alerts ? alerts.length : '–'} hint="İzin veya devamsızlık sınırı" tone="amber" />
      </div>
      {alerts && alerts.length > 0 && (
        <Card title="Sınıra yaklaşan öğrenciler" subtitle="İzin veya devamsızlık uyarısı olanlar">
          <table className="table">
            <thead>
              <tr>
                <th>Öğrenci</th>
                <th>Bölüm</th>
                <th>Kalan izin</th>
                <th>Mazeretsiz devamsızlık</th>
              </tr>
            </thead>
            <tbody>
              {alerts.slice(0, 8).map((a) => (
                <tr key={a.studentId}>
                  <td>{a.studentName}</td>
                  <td>{a.departmentName}</td>
                  <td>{a.balance.remaining} gün</td>
                  <td>
                    {a.balance.unexcusedAbsenceDays} / {a.balance.absenceLimit}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </>
  );
}
