import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import type { Department, Paged, StudentSummary, User } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Badge, Card, Empty, Field, Pager } from '../../components/ui';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

export default function StudentsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const isFaculty = user?.role === 'FacultyAdmin';
  const canPickAdvisor = isFaculty || user?.role === 'DepartmentHead';

  const [text, setText] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [advisorId, setAdvisorId] = useState('');
  const [year, setYear] = useState('');
  const [active, setActive] = useState('true');
  const [page, setPage] = useState(1);

  const departments = useLoad(() => (isFaculty ? api.get<Department[]>('/api/departments') : Promise.resolve([])), []);
  const advisors = useLoad(() => (canPickAdvisor ? api.get<User[]>('/api/users?role=Advisor&isActive=true') : Promise.resolve([])), []);

  const query = new URLSearchParams({ page: String(page), pageSize: '20' });
  if (text.trim()) query.set('text', text.trim());
  if (departmentId) query.set('departmentId', departmentId);
  if (advisorId) query.set('advisorUserId', advisorId);
  if (year) query.set('registrationYear', year);
  if (active) query.set('isActive', active);

  const students = useLoad(() => api.get<Paged<StudentSummary>>(`/api/students?${query}`), [query.toString()]);
  const reset = (set: (v: string) => void) => (v: string) => { set(v); setPage(1); };

  return (
    <Card title={user?.role === 'Advisor' ? 'Öğrencilerim' : 'Öğrenciler'} subtitle="Satıra tıklayarak öğrencinin plan, izin, devam ve görev kayıtlarını görebilirsiniz.">
      <Filters>
        <Field label="Ara"><input value={text} onChange={(e) => reset(setText)(e.target.value)} placeholder="Ad veya öğrenci no" /></Field>
        {isFaculty && (
          <Field label="Bölüm">
            <select value={departmentId} onChange={(e) => reset(setDepartmentId)(e.target.value)}>
              <option value="">Tümü</option>
              {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </Field>
        )}
        {canPickAdvisor && (
          <Field label="Danışman">
            <select value={advisorId} onChange={(e) => reset(setAdvisorId)(e.target.value)}>
              <option value="">Tümü</option>
              {advisors.data?.map((a) => <option key={a.id} value={a.id}>{a.fullName}</option>)}
            </select>
          </Field>
        )}
        <Field label="Kayıt yılı"><input inputMode="numeric" value={year} onChange={(e) => reset(setYear)(e.target.value.replace(/\D/g, '').slice(0, 4))} /></Field>
        <Field label="Çalışma durumu">
          <select value={active} onChange={(e) => reset(setActive)(e.target.value)}>
            <option value="">Tümü</option>
            <option value="true">Aktif</option>
            <option value="false">Pasif</option>
          </select>
        </Field>
      </Filters>

      {students.error && <Alert>{students.error}</Alert>}
      {students.data && students.data.items.length === 0 && <Empty>Kriterlere uyan öğrenci yok.</Empty>}
      {students.data && students.data.items.length > 0 && (
        <>
          <table className="table">
            <thead>
              <tr><th>Ad soyad</th><th>Öğrenci no</th><th>T.C.</th><th>Bölüm</th><th>Birim</th><th>Danışman</th><th>Yıl</th><th>Durum</th></tr>
            </thead>
            <tbody>
              {students.data.items.map((s) => (
                <tr key={s.id} className="link-row" onClick={() => navigate(`/ogrenciler/${s.id}`)}>
                  <td>{s.fullName}</td>
                  <td>{s.studentNumber}</td>
                  <td>{s.maskedNationalId}</td>
                  <td>{s.departmentName}</td>
                  <td>{s.currentUnitName ?? '–'}</td>
                  <td>{s.currentAdvisorName ?? '–'}</td>
                  <td>{s.registrationYear}</td>
                  <td><Badge tone={s.isActive ? 'green' : 'gray'}>{s.isActive ? 'Aktif' : 'Pasif'}</Badge></td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="muted small" style={{ marginTop: 8 }}>Toplam {students.data.totalCount} öğrenci</p>
          <Pager page={students.data.page} totalPages={students.data.totalPages} onChange={setPage} />
        </>
      )}
    </Card>
  );
}
