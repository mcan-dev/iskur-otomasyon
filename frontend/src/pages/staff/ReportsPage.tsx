import { useState } from 'react';
import { api, ApiError } from '../../api/client';
import type { Department, ReportTable, StudentSummary, User } from '../../api/types';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Card, Empty, Field } from '../../components/ui';
import { monthName, reportKinds } from '../../format';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

export default function ReportsPage() {
  // Raporlar yalnızca fakülte sekreterliği / dekanlık hesabında açılır.
  const kinds = reportKinds;

  const now = new Date();
  const [kind, setKind] = useState(kinds[0].id);
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [departmentId, setDepartmentId] = useState('');
  const [advisorId, setAdvisorId] = useState('');
  const [active, setActive] = useState('');
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [error, setError] = useState('');

  const current = kinds.find((k) => k.id === kind) ?? kinds[0];
  const needsYear = kind !== 'StudentList';

  const departments = useLoad(() => api.get<Department[]>('/api/departments'), []);
  const advisors = useLoad(() => api.get<User[]>('/api/users?role=Advisor'), []);

  const query = new URLSearchParams();
  if (needsYear) query.set('year', String(year));
  if (current.needsMonth) query.set('month', String(month));
  if (departmentId) query.set('departmentId', departmentId);
  if (advisorId) query.set('advisorUserId', advisorId);
  if (active) query.set('isActive', active);
  if (student) query.set('studentId', String(student.id));
  const qs = query.toString();

  const report = useLoad(() => api.get<ReportTable>(`/api/reports/${kind}?${qs}`), [kind, qs]);

  async function save(format: 'Xlsx' | 'Csv') {
    setError('');
    try {
      await api.download(`/api/reports/${kind}/export?${qs}&format=${format}`, `rapor.${format === 'Xlsx' ? 'xlsx' : 'csv'}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Dosya indirilemedi.');
    }
  }

  return (
    <Card title="Raporlar" subtitle="Ekrandaki tablo ile indirilen dosya aynı filtrelerle üretilir.">
      <Filters>
        <Field label="Rapor">
          <select value={kind} onChange={(e) => setKind(e.target.value)}>
            {kinds.map((k) => <option key={k.id} value={k.id}>{k.label}</option>)}
          </select>
        </Field>
        {needsYear && (
          <Field label="Yıl"><input type="number" value={year} min={2000} max={2100} onChange={(e) => setYear(Number(e.target.value))} /></Field>
        )}
        {current.needsMonth && (
          <Field label="Ay">
            <select value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {Array.from({ length: 12 }, (_, i) => <option key={i} value={i + 1}>{monthName(i + 1)}</option>)}
            </select>
          </Field>
        )}
        {(
          <Field label="Bölüm">
            <select value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
              <option value="">Tümü</option>
              {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </Field>
        )}
        {(
          <Field label="Danışman">
            <select value={advisorId} onChange={(e) => setAdvisorId(e.target.value)}>
              <option value="">Tümü</option>
              {advisors.data?.map((a) => <option key={a.id} value={a.id}>{a.fullName}</option>)}
            </select>
          </Field>
        )}
        <Field label="Çalışma durumu">
          <select value={active} onChange={(e) => setActive(e.target.value)}>
            <option value="">Hepsi</option>
            <option value="true">Aktif</option>
            <option value="false">Pasif</option>
          </select>
        </Field>
      </Filters>

      <div style={{ margin: '4px 0 12px' }}>
        <span className="muted small">Öğrenci bazında rapor için öğrenci seçin (boşsa tüm öğrenciler):</span>
        <StudentPicker value={student} onChange={setStudent} />
      </div>

      <div className="row" style={{ marginBottom: 12 }}>
        <button className="btn" onClick={() => save('Xlsx')} disabled={!report.data}>Excel olarak indir</button>
        <button className="btn" onClick={() => save('Csv')} disabled={!report.data}>CSV olarak indir</button>
      </div>

      {(error || report.error) && <Alert>{error || report.error}</Alert>}
      {report.loading && <p className="muted">Yükleniyor…</p>}
      {report.data && (
        <>
          <h3 style={{ margin: '4px 0 8px' }}>{report.data.title}</h3>
          {report.data.rows.length === 0 ? (
            <Empty>Bu filtrelerle gösterilecek kayıt yok.</Empty>
          ) : (
            <div className="scroll-x">
              <table className="table">
                <thead><tr>{report.data.columns.map((c) => <th key={c}>{c}</th>)}</tr></thead>
                <tbody>
                  {report.data.rows.map((row, i) => (
                    <tr key={i}>{row.map((cell, j) => <td key={j}>{cell === null || cell === '' ? '—' : String(cell)}</td>)}</tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="muted small" style={{ marginTop: 8 }}>{report.data.rows.length} satır</p>
        </>
      )}
    </Card>
  );
}
