import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import type { Absence, Leave, LeaveAlert } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Card, Empty, Field } from '../../components/ui';
import { monthName } from '../../format';
import { useLoad } from '../../useLoad';
import { AbsenceTable, Filters, LeaveTable } from './parts';

type Tab = 'leaves' | 'absences' | 'alerts';

export default function LeavesStaffPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<Tab>('leaves');
  const [year, setYear] = useState(new Date().getFullYear());

  return (
    <div className="stack">
      <div className="row between">
        <div className="tabs">
          <button className={`tab ${tab === 'leaves' ? 'active' : ''}`} onClick={() => setTab('leaves')}>İzinler</button>
          <button className={`tab ${tab === 'absences' ? 'active' : ''}`} onClick={() => setTab('absences')}>Devamsızlıklar</button>
          <button className={`tab ${tab === 'alerts' ? 'active' : ''}`} onClick={() => setTab('alerts')}>Sınıra yaklaşanlar</button>
        </div>
        <label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label>
      </div>
      {tab === 'leaves' && <Leaves year={year} canCancel={user?.role === 'FacultyAdmin'} />}
      {tab === 'absences' && <Absences year={year} canRevoke={user?.role === 'FacultyAdmin'} />}
      {tab === 'alerts' && <Alerts year={year} />}
    </div>
  );
}

function Leaves({ year, canCancel }: { year: number; canCancel: boolean }) {
  const [status, setStatus] = useState('Approved');
  const [month, setMonth] = useState('');
  const query = new URLSearchParams({ year: String(year) });
  if (status) query.set('status', status);
  if (month) query.set('month', month);
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves?${query}`), [query.toString()]);
  return (
    <Card
      title="Öğrenci izinleri"
      subtitle="İzinler öğrenci tarafından çalışma günlerinden seçilir ve onay beklemeden geçerli olur. Fakülte sekreterliği gerekirse iptal eder."
    >
      <Filters>
        <Field label="Durum">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Tümü</option>
            <option value="Approved">Geçerli</option>
            <option value="Cancelled">İptal edilen</option>
          </select>
        </Field>
        <Field label="Ay">
          <select value={month} onChange={(e) => setMonth(e.target.value)}>
            <option value="">Tüm yıl</option>
            {Array.from({ length: 12 }, (_, i) => (
              <option key={i + 1} value={i + 1}>{monthName(i + 1)}</option>
            ))}
          </select>
        </Field>
      </Filters>
      {leaves.error && <Alert>{leaves.error}</Alert>}
      {leaves.data && <LeaveTable leaves={leaves.data} canCancel={canCancel} onChanged={leaves.reload} />}
    </Card>
  );
}

function Absences({ year, canRevoke }: { year: number; canRevoke: boolean }) {
  const absences = useLoad(() => api.get<Absence[]>(`/api/absences?year=${year}`), [year]);
  return (
    <Card title="Devamsızlık kayıtları">
      {absences.error && <Alert>{absences.error}</Alert>}
      {absences.data && <AbsenceTable absences={absences.data} canRevoke={canRevoke} onChanged={absences.reload} />}
    </Card>
  );
}

function Alerts({ year }: { year: number }) {
  const navigate = useNavigate();
  const alerts = useLoad(() => api.get<LeaveAlert[]>(`/api/leaves/alerts?year=${year}`), [year]);
  return (
    <Card title="İzin hakkına yaklaşan / hakkı aşan öğrenciler">
      {alerts.error && <Alert>{alerts.error}</Alert>}
      {alerts.data?.length === 0 && <Empty>Sınıra yaklaşan öğrenci yok.</Empty>}
      {alerts.data && alerts.data.length > 0 && (
        <table className="table">
          <thead><tr><th>Öğrenci</th><th>Bölüm</th><th>Kalan izin</th><th>Kullanılan</th><th>Uyarı</th></tr></thead>
          <tbody>
            {alerts.data.map((a) => (
              <tr key={a.studentId} className="link-row" onClick={() => navigate(`/ogrenciler/${a.studentId}`)}>
                <td>{a.studentName}</td>
                <td>{a.departmentName}</td>
                <td>{a.balance.remaining} gün</td>
                <td>{a.balance.used} / {a.balance.limit} gün</td>
                <td>{a.balance.warnings.map((w) => w.message).join(' ')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  );
}
