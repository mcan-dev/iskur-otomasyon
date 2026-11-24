import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import type { BulkAssignResult, Department, Paged, StudentSummary, User, WorkUnit } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Badge, Card, Empty, Field, Modal, Pager, PhoneInput } from '../../components/ui';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

const msg = (err: unknown, fallback: string) => {
  if (!(err instanceof ApiError)) return fallback;
  const details = Object.values(err.fieldErrors).flat();
  return details.length ? details.join(' ') : err.message;
};

export default function StudentsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const isFaculty = user?.role === 'FacultyAdmin';
  const canAssign = isFaculty || user?.role === 'DepartmentHead';

  const [text, setText] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [advisorId, setAdvisorId] = useState('');
  const [year, setYear] = useState('');
  const [active, setActive] = useState('true');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'info' | 'warn' | 'error'; text: string } | null>(null);

  const departments = useLoad(() => (isFaculty ? api.get<Department[]>('/api/departments') : Promise.resolve([])), []);
  const advisors = useLoad(
    () => (canAssign ? api.get<User[]>(`/api/users?role=Advisor&isActive=true${departmentId ? `&departmentId=${departmentId}` : ''}`) : Promise.resolve([])),
    [departmentId],
  );

  const query = new URLSearchParams({ page: String(page), pageSize: departmentId ? '100' : '20' });
  if (text.trim()) query.set('text', text.trim());
  if (departmentId) query.set('departmentId', departmentId);
  if (advisorId) query.set('advisorUserId', advisorId);
  if (year) query.set('registrationYear', year);
  if (active) query.set('isActive', active);

  const students = useLoad(() => api.get<Paged<StudentSummary>>(`/api/students?${query}`), [query.toString()]);
  const reset = (set: (v: string) => void) => (v: string) => { set(v); setPage(1); };

  // Filtre değişince seçim temizlenir: seçilenler her zaman ekrandaki listeden olur.
  useEffect(() => setSelected(new Set()), [query.toString()]);

  const items = students.data?.items ?? [];
  const allChosen = items.length > 0 && items.every((s) => selected.has(s.id));
  function toggleAll() {
    setSelected(allChosen ? new Set() : new Set(items.filter((s) => s.isActive).map((s) => s.id)));
  }
  function toggle(id: number) {
    setSelected((cur) => {
      const copy = new Set(cur);
      if (copy.has(id)) copy.delete(id);
      else copy.add(id);
      return copy;
    });
  }

  return (
    <Card
      title={user?.role === 'Advisor' ? 'Öğrencilerim' : 'Öğrenciler'}
      subtitle={canAssign
        ? 'Bölüm seçince o bölümün öğrencileri listelenir. Birden çok öğrenciyi işaretleyip tek işlemde birime veya sorumlu personele atayabilirsiniz. Satıra tıklayınca öğrencinin ayrıntısı açılır.'
        : 'Satıra tıklayarak öğrencinin plan, izin, devam ve görev kayıtlarını görebilirsiniz.'}
    >
      {isFaculty && (
        <div className="row" style={{ marginBottom: 12 }}>
          <button className="btn primary" onClick={() => setCreating(true)}>Öğrenci ekle</button>
          <span className="muted small">Öğrenci hesapları bilgi işlemden gelen öğrenci e-postası ve OBİS şifresiyle açılır; öğrenciler kayıt olmaz.</span>
        </div>
      )}
      <Filters>
        <Field label="Ara"><input value={text} onChange={(e) => reset(setText)(e.target.value)} placeholder="Ad veya öğrenci no" /></Field>
        {isFaculty && (
          <Field label="Bölüm">
            <select value={departmentId} onChange={(e) => { reset(setDepartmentId)(e.target.value); setAdvisorId(''); }}>
              <option value="">Tümü</option>
              {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </Field>
        )}
        {canAssign && (
          <Field label="Sorumlu personel">
            <select value={advisorId} onChange={(e) => reset(setAdvisorId)(e.target.value)}>
              <option value="">Tümü</option>
              {advisors.data?.map((a) => <option key={a.id} value={a.id}>{a.registryNumber} · {a.fullName}</option>)}
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

      {notice && <Alert kind={notice.kind}>{notice.text}</Alert>}
      {canAssign && selected.size > 0 && (
        <BulkAssign
          studentIds={[...selected]}
          isFaculty={isFaculty}
          advisors={advisors.data ?? []}
          onDone={(result) => {
            setSelected(new Set());
            students.reload();
            const failed = result.errors.map((e) => `${e.studentName || e.studentId}: ${e.message}`).join(' · ');
            setNotice({
              kind: result.errors.length ? 'warn' : 'info',
              text: `${result.assigned} öğrenci atandı.${failed ? ` Atanamayanlar: ${failed}` : ''}`,
            });
          }}
          onError={(text) => setNotice({ kind: 'error', text })}
        />
      )}

      {students.error && <Alert>{students.error}</Alert>}
      {students.data && items.length === 0 && <Empty>Kriterlere uyan öğrenci yok.</Empty>}
      {students.data && items.length > 0 && (
        <>
          <table className="table">
            <thead>
              <tr>
                {canAssign && <th><input type="checkbox" checked={allChosen} onChange={toggleAll} aria-label="Tümünü seç" /></th>}
                <th>Ad soyad</th><th>Öğrenci no</th><th>T.C.</th><th>Bölüm</th><th>Birim</th><th>Sorumlu personel</th><th>Yıl</th><th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {items.map((s) => (
                <tr key={s.id} className="link-row" onClick={() => navigate(`/ogrenciler/${s.id}`)}>
                  {canAssign && (
                    <td onClick={(e) => e.stopPropagation()}>
                      <input type="checkbox" checked={selected.has(s.id)} disabled={!s.isActive} onChange={() => toggle(s.id)} />
                    </td>
                  )}
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
          <p className="muted small" style={{ marginTop: 8 }}>Toplam {students.data.totalCount} öğrenci{selected.size > 0 ? ` · ${selected.size} seçili` : ''}</p>
          <Pager page={students.data.page} totalPages={students.data.totalPages} onChange={setPage} />
        </>
      )}
      {creating && (
        <CreateStudent
          departments={departments.data ?? []}
          onClose={() => setCreating(false)}
          onDone={() => { setCreating(false); students.reload(); setNotice({ kind: 'info', text: 'Öğrenci hesabı açıldı.' }); }}
        />
      )}
    </Card>
  );
}

/** Seçilen öğrencileri tek işlemde birime ve/veya sorumlu personele (sicil no ile) atar. */
function BulkAssign({ studentIds, isFaculty, advisors, onDone, onError }: {
  studentIds: number[];
  isFaculty: boolean;
  advisors: User[];
  onDone: (r: BulkAssignResult) => void;
  onError: (text: string) => void;
}) {
  const units = useLoad(() => (isFaculty ? api.get<WorkUnit[]>('/api/work-units') : Promise.resolve([])), []);
  const [unitId, setUnitId] = useState('');
  const [registry, setRegistry] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!unitId && !registry.trim()) return onError('Bir çalışma birimi seçin veya sorumlu personelin sicil numarasını girin.');
    setBusy(true);
    try {
      const result = await api.post<BulkAssignResult>('/api/students/bulk-assign', {
        studentIds,
        workUnitId: unitId ? Number(unitId) : null,
        advisorRegistryNumber: registry.trim() || null,
      });
      setUnitId('');
      setRegistry('');
      onDone(result);
    } catch (err) {
      onError(msg(err, 'Atama yapılamadı.'));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card bulk-bar" onSubmit={submit}>
      <strong>{studentIds.length} öğrenci seçildi</strong>
      {isFaculty && (
        <Field label="Çalışma birimi">
          <select value={unitId} onChange={(e) => setUnitId(e.target.value)}>
            <option value="">Değiştirme</option>
            {units.data?.filter((u) => u.isActive).map((u) => <option key={u.id} value={u.id}>{u.name}</option>)}
          </select>
        </Field>
      )}
      <Field label="Sorumlu personel sicil no">
        <input value={registry} onChange={(e) => setRegistry(e.target.value)} list="bulk-advisors" placeholder="Değiştirme" inputMode="numeric" />
        <datalist id="bulk-advisors">
          {advisors.map((a) => <option key={a.id} value={a.registryNumber ?? ''}>{a.fullName} · {a.departmentName}</option>)}
        </datalist>
      </Field>
      <button className="btn primary" disabled={busy}>{busy ? 'Atanıyor…' : 'Seçilenleri ata'}</button>
    </form>
  );
}

/** Bilgi işlemden gelen bilgilerle öğrenci hesabı açma (yalnızca sekreterlik). */
function CreateStudent({ departments, onClose, onDone }: { departments: Department[]; onClose: () => void; onDone: () => void }) {
  const [v, setV] = useState({
    fullName: '', email: '', obisPassword: '', phoneNumber: '', nationalId: '', studentNumber: '',
    departmentId: '', registrationYear: String(new Date().getFullYear()),
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof v) => (e: { target: { value: string } }) => setV((x) => ({ ...x, [k]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.post('/api/students', {
        ...v,
        fullName: v.fullName.trim(),
        email: v.email.trim(),
        departmentId: Number(v.departmentId),
        registrationYear: Number(v.registrationYear),
      });
      onDone();
    } catch (err) {
      setError(msg(err, 'Öğrenci hesabı açılamadı.'));
      setBusy(false);
    }
  }

  return (
    <Modal title="Öğrenci ekle" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <p className="muted small">Bilgiler fakültede İŞKUR kapsamında çalışan öğrenciler için bilgi işlemden alınır. Öğrenci, öğrenci e-postası ve OBİS şifresiyle giriş yapar.</p>
        <Field label="Ad soyad"><input value={v.fullName} onChange={set('fullName')} required /></Field>
        <Field label="Öğrenci e-postası"><input type="email" value={v.email} onChange={set('email')} placeholder="ogrencino@ogr.selcuk.edu.tr" required /></Field>
        <Field label="OBİS şifresi"><input type="password" value={v.obisPassword} onChange={set('obisPassword')} autoComplete="new-password" required /></Field>
        <Field label="Telefon"><PhoneInput value={v.phoneNumber} onChange={(x) => setV((y) => ({ ...y, phoneNumber: x }))} required /></Field>
        <Field label="T.C. kimlik no"><input value={v.nationalId} onChange={set('nationalId')} maxLength={11} inputMode="numeric" required /></Field>
        <Field label="Öğrenci numarası"><input value={v.studentNumber} onChange={set('studentNumber')} maxLength={9} required /></Field>
        <Field label="Bölüm">
          <select value={v.departmentId} onChange={set('departmentId')} required>
            <option value="">Seçin</option>
            {departments.filter((d) => d.isActive).map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </Field>
        <Field label="Kayıt yılı"><input type="number" value={v.registrationYear} onChange={set('registrationYear')} required /></Field>
        <button className="btn primary wide" disabled={busy}>{busy ? 'Kaydediliyor…' : 'Öğrenci hesabını aç'}</button>
      </form>
    </Modal>
  );
}
