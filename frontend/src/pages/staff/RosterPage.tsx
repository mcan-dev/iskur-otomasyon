import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import type { Department, RosterStatus, WorkRoster, YearWorkers } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Badge, Card, Empty, Field, MonthPicker } from '../../components/ui';
import { fmtDate } from '../../format';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

const statusLabels: Record<RosterStatus, string> = { Planned: 'Planlı', Attended: 'Çalıştı', OnLeave: 'İzinli', Missing: 'Kayıt yok' };
const statusTone: Record<RosterStatus, 'blue' | 'green' | 'gray' | 'amber'> = { Planned: 'blue', Attended: 'green', OnLeave: 'gray', Missing: 'amber' };
const dayNames = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];

/** Hangi gün kimin çalıştığı (sorumlu personel, birim yetkilisi, sekreterlik) ve geçmiş yıllarda çalışanlar. */
export default function RosterPage() {
  const { user } = useAuth();
  const canSeeYears = user?.role === 'FacultyAdmin' || user?.role === 'DepartmentHead';
  const [tab, setTab] = useState<'days' | 'years'>('days');
  return (
    <div className="stack">
      {canSeeYears && (
        <div className="tabs">
          <button className={`tab ${tab === 'days' ? 'active' : ''}`} onClick={() => setTab('days')}>Günlere göre</button>
          <button className={`tab ${tab === 'years' ? 'active' : ''}`} onClick={() => setTab('years')}>Yıllara göre</button>
        </div>
      )}
      {tab === 'days' ? <Days isFaculty={user?.role === 'FacultyAdmin'} /> : <Years isFaculty={user?.role === 'FacultyAdmin'} />}
    </div>
  );
}

function DepartmentFilter({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const departments = useLoad(() => api.get<Department[]>('/api/departments'), []);
  return (
    <Field label="Bölüm">
      <select value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">Tümü</option>
        {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
      </select>
    </Field>
  );
}

function Days({ isFaculty }: { isFaculty: boolean }) {
  const navigate = useNavigate();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [departmentId, setDepartmentId] = useState('');
  const roster = useLoad(
    () => api.get<WorkRoster>(`/api/work-roster?year=${year}&month=${month}${departmentId ? `&departmentId=${departmentId}` : ''}`),
    [year, month, departmentId],
  );

  return (
    <Card title="Kim ne zaman çalışıyor" subtitle="Onaylı çalışma planlarına göre her gün çalışan öğrenciler; devam ve izin durumuyla.">
      <Filters>
        <Field label="Ay"><MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} /></Field>
        {isFaculty && <DepartmentFilter value={departmentId} onChange={setDepartmentId} />}
      </Filters>
      {roster.error && <Alert>{roster.error}</Alert>}
      {roster.data?.days.length === 0 && <Empty>Bu ay için onaylı çalışma planı yok.</Empty>}
      <div className="stack">
        {roster.data?.days.map((d) => (
          <div key={d.date} className="roster-day">
            <h4>{fmtDate(d.date)} {dayNames[new Date(d.date + 'T00:00:00').getDay()]} · {d.students.length} öğrenci</h4>
            <table className="table">
              <tbody>
                {d.students.map((s) => (
                  <tr key={s.studentId} className="link-row" onClick={() => navigate(`/ogrenciler/${s.studentId}`)}>
                    <td>{s.studentName}</td>
                    <td className="muted">{s.studentNumber}</td>
                    <td>{s.departmentName}</td>
                    <td><Badge tone={statusTone[s.status]}>{statusLabels[s.status]}</Badge></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
      </div>
    </Card>
  );
}

function Years({ isFaculty }: { isFaculty: boolean }) {
  const navigate = useNavigate();
  const [year, setYear] = useState(new Date().getFullYear());
  const [departmentId, setDepartmentId] = useState('');
  const workers = useLoad(
    () => api.get<YearWorkers>(`/api/work-roster/year?year=${year}${departmentId ? `&departmentId=${departmentId}` : ''}`),
    [year, departmentId],
  );

  return (
    <Card title="Geçmiş yıllarda çalışanlar" subtitle="Seçilen yılda çalışma planı onaylanmış veya devam kaydı olan öğrenciler.">
      <Filters>
        <Field label="Yıl">
          <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 100 }} />
        </Field>
        {isFaculty && <DepartmentFilter value={departmentId} onChange={setDepartmentId} />}
      </Filters>
      {workers.error && <Alert>{workers.error}</Alert>}
      {workers.data?.students.length === 0 && <Empty>{year} yılında çalışan öğrenci kaydı yok.</Empty>}
      {workers.data && workers.data.students.length > 0 && (
        <>
          <table className="table">
            <thead><tr><th>Öğrenci</th><th>Öğrenci no</th><th>Bölüm</th><th>Planlanan gün</th><th>Çalışılan gün</th><th>İzinli gün</th></tr></thead>
            <tbody>
              {workers.data.students.map((s) => (
                <tr key={s.studentId} className="link-row" onClick={() => navigate(`/ogrenciler/${s.studentId}`)}>
                  <td>{s.studentName}</td>
                  <td>{s.studentNumber}</td>
                  <td>{s.departmentName}</td>
                  <td>{s.plannedDays}</td>
                  <td>{s.workedDays}</td>
                  <td>{s.leaveDays}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="muted small" style={{ marginTop: 8 }}>{workers.data.students.length} öğrenci</p>
        </>
      )}
    </Card>
  );
}
