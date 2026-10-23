import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { AbsenceNotice, AttendanceComparison, Leave, LeaveAlert, LeaveBalance, Paged, PlanCalendar, StudentProfileFull, StudentSummary, StudentTask, WorkPlan } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Alert, Badge } from '../components/ui';
import { Icon } from '../components/Icon';
import { fmtDate, planStatusLabels } from '../format';
import { menus, roleLabels } from '../layout/menu';
import { useLoad } from '../useLoad';

type Tone = 'blue' | 'amber' | 'green' | 'red';

function Tile({ icon, label, value, hint, tone = 'blue' }: { icon: string; label: string; value: React.ReactNode; hint?: string; tone?: Tone }) {
  return (
    <div className={`tile-card tone-bg-${tone}`}>
      <span className="tile-icon">
        <Icon name={icon} size={24} />
      </span>
      <div>
        <span className="tile-label">{label}</span>
        <strong className="tile-value">{value}</strong>
        {hint && <span className="tile-hint">{hint}</span>}
      </div>
    </div>
  );
}

/** Başlık ve sağ tarafta çerçeveli "tümü" düğmesi olan bölüm. */
function Section({ title, to, toLabel, children }: { title: string; to?: string; toLabel?: string; children: React.ReactNode }) {
  return (
    <section className="panel">
      <div className="panel-head">
        <h2>{title}</h2>
        {to && (
          <Link to={to} className="outline-btn">
            {toLabel ?? 'TÜMÜ'} <Icon name="arrow" size={16} />
          </Link>
        )}
      </div>
      {children}
    </section>
  );
}

export default function Dashboard() {
  const { user } = useAuth();
  if (!user) return null;
  const items = menus[user.role].filter((m) => m.path !== '/');
  return (
    <>
      <div className="hero">
        <span className="tag">{roleLabels[user.role]}</span>
        <h2>Merhaba, {user.fullName}</h2>
        {user.departmentName && <p>{user.departmentName}</p>}
      </div>

      {user.role === 'Student' ? <StudentSummary /> : <StaffSummary role={user.role} />}

      <Section title="Hızlı Erişim">
        <div className="quick">
          {items.map((m) => (
            <Link key={m.path} to={m.path} className="quick-item">
              <span className="tile">
                <Icon name={m.icon} size={22} />
              </span>
              <strong>{m.label}</strong>
            </Link>
          ))}
        </div>
      </Section>
    </>
  );
}

function StudentSummary() {
  const now = new Date();
  const [year, month] = [now.getFullYear(), now.getMonth() + 1];
  const profile = useLoad(() => api.get<StudentProfileFull>('/api/students/me'), []);
  const balance = useLoad(() => api.get<LeaveBalance>('/api/leaves/me/balance'), []);
  const calendar = useLoad(() => api.get<PlanCalendar>(`/api/work-plans/me/calendar?year=${year}&month=${month}`), []);
  const tasks = useLoad(() => api.get<StudentTask[]>('/api/tasks/me?status=Open'), []);
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves/me?year=${year}`), []);
  const worked = useLoad(() => api.get<AttendanceComparison>(`/api/attendance/me/comparison?year=${year}&month=${month}`), []);

  const p = profile.data;
  const b = balance.data;
  const pendingLeaves = leaves.data?.filter((l) => l.status === 'Pending').length;
  const planStatus = calendar.data?.planStatus;

  return (
    <>
      {(profile.error || balance.error) && <Alert kind="warn">Bazı bilgiler yüklenemedi.</Alert>}
      {p && !p.iban && <Alert kind="info">IBAN bilginiz kayıtlı değil. Profilim sayfasından ekleyebilirsiniz.</Alert>}
      {b?.warnings.map((w) => (
        <Alert key={w.code} kind="warn">
          {w.message}
        </Alert>
      ))}
      <div className="tiles">
        <Tile icon="clock" label="Bu ay çalışılan saat" value={worked.data?.hasApprovedPlan ? `${worked.data.workedHours} saat` : '–'} hint={worked.data?.hasApprovedPlan ? `${worked.data.attendedDays} / ${worked.data.plannedDays} gün` : 'Onaylı plan yok'} tone="green" />
        <Tile icon="building" label="Çalıştığı birim" value={p?.currentUnit?.workUnitName ?? 'Atanmadı'} />
        <Tile icon="user" label="Danışman" value={p?.currentAdvisor?.advisorName ?? 'Atanmadı'} />
        <Tile icon="mail" label="Kalan izin hakkı" value={b ? `${b.remaining} gün` : '–'} hint={b ? `${b.year} yılı, ${b.limit} gün üzerinden` : undefined} tone="green" />
      </div>
      <Section title="Sizi Bekleyenler">
        <ul className="todo">
          <li>
            <Icon name="calendar" size={20} />
            <span>
              <strong>Bu ayın çalışma planı</strong>
              {calendar.data && calendar.data.canEdit && <small>Son düzenleme tarihi {fmtDate(calendar.data.editDeadline)}</small>}
            </span>
            {planStatus ? <Badge tone={planStatus === 'Approved' ? 'green' : planStatus === 'Rejected' ? 'red' : 'amber'}>{planStatusLabels[planStatus]}</Badge> : <Badge>Gönderilmedi</Badge>}
            <Link to="/plan" className="outline-btn small">
              AÇ
            </Link>
          </li>
          <li>
            <Icon name="tasks" size={20} />
            <span>
              <strong>Açık görevler</strong>
            </span>
            <Badge tone={tasks.data?.length ? 'amber' : 'gray'}>{tasks.data ? tasks.data.length : '–'}</Badge>
            <Link to="/gorevlerim" className="outline-btn small">
              AÇ
            </Link>
          </li>
          <li>
            <Icon name="mail" size={20} />
            <span>
              <strong>Onay bekleyen izin talepleri</strong>
            </span>
            <Badge tone={pendingLeaves ? 'amber' : 'gray'}>{pendingLeaves ?? '–'}</Badge>
            <Link to="/izinlerim" className="outline-btn small">
              AÇ
            </Link>
          </li>
        </ul>
      </Section>
    </>
  );
}

function StaffSummary({ role }: { role: string }) {
  const now = new Date();
  const [year, month] = [now.getFullYear(), now.getMonth() + 1];
  const students = useLoad(() => api.get<Paged<StudentSummary>>('/api/students?page=1&pageSize=1&isActive=true'), []);
  const alerts = useLoad(() => api.get<LeaveAlert[]>(`/api/leaves/alerts?year=${year}`), []);
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves?year=${year}&status=Pending`), []);
  const plans = useLoad(() => (role === 'FacultyAdmin' ? api.get<WorkPlan[]>(`/api/work-plans?year=${year}&month=${month}&status=Pending`) : Promise.resolve([] as WorkPlan[])), [role]);
  const notices = useLoad(() => (role === 'FacultyAdmin' ? Promise.resolve([] as AbsenceNotice[]) : api.get<AbsenceNotice[]>('/api/absence-notices?unreadOnly=true')), [role]);

  const a = alerts.data;
  return (
    <>
      {(students.error || alerts.error) && <Alert kind="warn">Bazı bilgiler yüklenemedi.</Alert>}
      <div className="tiles">
        <Tile icon="users" label="Aktif öğrenci" value={students.data?.totalCount ?? '–'} hint="Yetki alanınızdaki kayıtlar" />
        <Tile icon="mail" label="Bekleyen izin talebi" value={leaves.data ? leaves.data.length : '–'} hint={`${year} yılı`} tone="amber" />
        {role === 'FacultyAdmin' ? (
          <Tile icon="check" label="Onay bekleyen plan" value={plans.data ? plans.data.length : '–'} hint="İçinde bulunulan ay" tone="amber" />
        ) : (
          <Tile icon="bell" label="Okunmamış bildirim" value={notices.data ? notices.data.length : '–'} hint="Devamsızlık bildirimleri" tone="amber" />
        )}
        <Tile icon="alert" label="Sınıra yaklaşan / aşan" value={a ? a.length : '–'} hint="İzin veya devamsızlık sınırı" tone="red" />
      </div>
      {a && a.length > 0 && (
        <Section title="Sınıra Yaklaşan Öğrenciler" to="/izinler" toLabel="TÜM UYARILAR">
          <div className="scroll-x">
            <table className="table">
              <thead>
                <tr>
                  <th>Öğrenci</th>
                  <th>Bölüm</th>
                  <th>Kalan izin</th>
                  <th>Kullanılan</th>
                </tr>
              </thead>
              <tbody>
                {a.slice(0, 8).map((x) => (
                  <tr key={x.studentId}>
                    <td>{x.studentName}</td>
                    <td>{x.departmentName}</td>
                    <td>{x.balance.remaining} gün</td>
                    <td>
                      {x.balance.used} / {x.balance.limit}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Section>
      )}
    </>
  );
}
