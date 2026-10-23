import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { Department, ExamPeriod, Holiday, LeavePolicy, Role, User, WorkUnit } from '../../api/types';
import { Alert, Badge, Card, Empty, Field, Modal, PhoneInput } from '../../components/ui';
import { fmtDate } from '../../format';
import { roleLabels } from '../../layout/menu';
import { useLoad } from '../../useLoad';

type Tab = 'departments' | 'units' | 'staff' | 'holidays' | 'exams' | 'policy';

const tabs: { id: Tab; label: string }[] = [
  { id: 'departments', label: 'Bölümler' },
  { id: 'units', label: 'Çalışma birimleri' },
  { id: 'staff', label: 'Personel hesapları' },
  { id: 'holidays', label: 'Resmî tatiller' },
  { id: 'exams', label: 'Sınav dönemleri' },
  { id: 'policy', label: 'İzin ve devamsızlık sınırları' },
];

const msg = (err: unknown, fallback: string) => (err instanceof ApiError ? err.message : fallback);

export default function DefinitionsPage() {
  const [tab, setTab] = useState<Tab>('departments');
  return (
    <>
      <div className="tabs">
        {tabs.map((t) => (
          <button key={t.id} className={`tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>{t.label}</button>
        ))}
      </div>
      {tab === 'departments' && <Departments />}
      {tab === 'units' && <Units />}
      {tab === 'staff' && <Staff />}
      {tab === 'holidays' && <Holidays />}
      {tab === 'exams' && <Exams />}
      {tab === 'policy' && <Policy />}
    </>
  );
}

/* ── Bölümler ─────────────────────────────────────────────── */

function Departments() {
  const list = useLoad(() => api.get<Department[]>('/api/departments?includeInactive=true'), []);
  const [editing, setEditing] = useState<Partial<Department> | null>(null);

  return (
    <Card title="Bölümler" subtitle="Bölümler silinmez; kullanılmayanlar pasif yapılır.">
      <div className="row" style={{ marginBottom: 12 }}>
        <button className="btn primary" onClick={() => setEditing({ isActive: true })}>Yeni bölüm</button>
      </div>
      {list.error && <Alert>{list.error}</Alert>}
      {list.data && (
        <table className="table">
          <thead><tr><th>Kod</th><th>Ad</th><th>Durum</th><th /></tr></thead>
          <tbody>
            {list.data.map((d) => (
              <tr key={d.id}>
                <td>{d.code}</td><td>{d.name}</td>
                <td><Badge tone={d.isActive ? 'green' : 'gray'}>{d.isActive ? 'Aktif' : 'Pasif'}</Badge></td>
                <td className="right-cell"><button className="btn small" onClick={() => setEditing(d)}>Düzenle</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {editing && (
        <NameForm
          title={editing.id ? 'Bölümü düzenle' : 'Yeni bölüm'}
          initial={{ name: editing.name ?? '', code: editing.code ?? '', isActive: editing.isActive ?? true }}
          withCode
          onClose={() => setEditing(null)}
          onSave={async (v) => {
            const body = { name: v.name, code: v.code, isActive: v.isActive };
            if (editing.id) await api.put(`/api/departments/${editing.id}`, body);
            else await api.post('/api/departments', body);
            setEditing(null);
            list.reload();
          }}
        />
      )}
    </Card>
  );
}

/* ── Çalışma birimleri ────────────────────────────────────── */

function Units() {
  const list = useLoad(() => api.get<WorkUnit[]>('/api/work-units?includeInactive=true'), []);
  const [editing, setEditing] = useState<Partial<WorkUnit> | null>(null);

  return (
    <Card title="Çalışma birimleri" subtitle="Öğrencilerin görevlendirildiği birimler (kütüphane, laboratuvar, dekanlık vb.).">
      <div className="row" style={{ marginBottom: 12 }}>
        <button className="btn primary" onClick={() => setEditing({ isActive: true })}>Yeni birim</button>
      </div>
      {list.error && <Alert>{list.error}</Alert>}
      {list.data && (
        <table className="table">
          <thead><tr><th>Ad</th><th>Açıklama</th><th>Durum</th><th /></tr></thead>
          <tbody>
            {list.data.map((u) => (
              <tr key={u.id}>
                <td>{u.name}</td><td>{u.description ?? ''}</td>
                <td><Badge tone={u.isActive ? 'green' : 'gray'}>{u.isActive ? 'Aktif' : 'Pasif'}</Badge></td>
                <td className="right-cell"><button className="btn small" onClick={() => setEditing(u)}>Düzenle</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {editing && (
        <NameForm
          title={editing.id ? 'Birimi düzenle' : 'Yeni birim'}
          initial={{ name: editing.name ?? '', description: editing.description ?? '', isActive: editing.isActive ?? true }}
          withDescription
          onClose={() => setEditing(null)}
          onSave={async (v) => {
            const body = { name: v.name, description: v.description?.trim() || null, isActive: v.isActive };
            if (editing.id) await api.put(`/api/work-units/${editing.id}`, body);
            else await api.post('/api/work-units', body);
            setEditing(null);
            list.reload();
          }}
        />
      )}
    </Card>
  );
}

interface NameValues { name: string; code?: string; description?: string; isActive: boolean }

function NameForm({ title, initial, withCode, withDescription, onClose, onSave }: {
  title: string; initial: NameValues; withCode?: boolean; withDescription?: boolean;
  onClose: () => void; onSave: (v: NameValues) => Promise<void>;
}) {
  const [v, setV] = useState(initial);
  const [error, setError] = useState('');

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await onSave(v);
    } catch (err) {
      setError(msg(err, 'Kayıt yapılamadı.'));
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        {withCode && <Field label="Kod"><input value={v.code ?? ''} onChange={(e) => setV({ ...v, code: e.target.value })} maxLength={20} required /></Field>}
        <Field label="Ad"><input value={v.name} onChange={(e) => setV({ ...v, name: e.target.value })} maxLength={150} required /></Field>
        {withDescription && <Field label="Açıklama (isteğe bağlı)"><textarea rows={3} value={v.description ?? ''} onChange={(e) => setV({ ...v, description: e.target.value })} maxLength={500} /></Field>}
        <label className="row"><input type="checkbox" checked={v.isActive} onChange={(e) => setV({ ...v, isActive: e.target.checked })} /> Aktif</label>
        <button className="btn primary wide" style={{ marginTop: 12 }}>Kaydet</button>
      </form>
    </Modal>
  );
}

/* ── Personel hesapları ───────────────────────────────────── */

const staffRoles: Exclude<Role, 'Student'>[] = ['FacultyAdmin', 'DepartmentHead', 'Advisor'];

function Staff() {
  const [role, setRole] = useState('');
  const users = useLoad(() => api.get<User[]>(`/api/users${role ? `?role=${role}` : ''}`), [role]);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');

  async function toggle(u: User) {
    setError('');
    try {
      await api.put(`/api/users/${u.id}/active`, { isActive: !u.isActive });
      users.reload();
    } catch (err) {
      setError(msg(err, 'Durum değiştirilemedi.'));
    }
  }

  return (
    <Card title="Personel hesapları" subtitle="Hesaplar silinmez; çalışmayanlar pasif yapılır. Öğrenciler kendileri kayıt olur.">
      <div className="row" style={{ marginBottom: 12 }}>
        <button className="btn primary" onClick={() => setCreating(true)}>Yeni personel</button>
        <select value={role} onChange={(e) => setRole(e.target.value)}>
          <option value="">Tüm roller</option>
          {staffRoles.map((r) => <option key={r} value={r}>{roleLabels[r]}</option>)}
        </select>
      </div>
      {(error || users.error) && <Alert>{error || users.error}</Alert>}
      {users.data && (
        <div className="scroll-x">
          <table className="table">
            <thead><tr><th>Ad soyad</th><th>E-posta</th><th>Rol</th><th>Bölüm</th><th>Durum</th><th /></tr></thead>
            <tbody>
              {users.data.map((u) => (
                <tr key={u.id}>
                  <td>{u.fullName}</td><td>{u.email}</td><td>{roleLabels[u.role]}</td><td>{u.departmentName ?? '—'}</td>
                  <td><Badge tone={u.isActive ? 'green' : 'gray'}>{u.isActive ? 'Aktif' : 'Pasif'}</Badge></td>
                  <td className="right-cell"><button className="btn small" onClick={() => toggle(u)}>{u.isActive ? 'Pasif yap' : 'Aktif yap'}</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {creating && <StaffForm onClose={() => setCreating(false)} onDone={() => { setCreating(false); users.reload(); }} />}
    </Card>
  );
}

function StaffForm({ onClose, onDone }: { onClose: () => void; onDone: () => void }) {
  const departments = useLoad(() => api.get<Department[]>('/api/departments'), []);
  const [v, setV] = useState({ fullName: '', email: '', phoneNumber: '', password: '', role: 'Advisor' as Exclude<Role, 'Student'>, departmentId: '' });
  const [error, setError] = useState('');
  const needsDepartment = v.role !== 'FacultyAdmin';

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/users', {
        fullName: v.fullName.trim(),
        email: v.email.trim(),
        phoneNumber: v.phoneNumber.trim() || null,
        password: v.password,
        role: v.role,
        departmentId: needsDepartment ? Number(v.departmentId) : null,
      });
      onDone();
    } catch (err) {
      setError(msg(err, 'Hesap oluşturulamadı.'));
    }
  }

  return (
    <Modal title="Yeni personel hesabı" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <Field label="Ad soyad"><input value={v.fullName} onChange={(e) => setV({ ...v, fullName: e.target.value })} required /></Field>
        <Field label="E-posta"><input type="email" value={v.email} onChange={(e) => setV({ ...v, email: e.target.value })} required /></Field>
        <Field label="Telefon (isteğe bağlı)"><PhoneInput value={v.phoneNumber} onChange={(x) => setV({ ...v, phoneNumber: x })} /></Field>
        <Field label="Rol">
          <select value={v.role} onChange={(e) => setV({ ...v, role: e.target.value as Exclude<Role, 'Student'> })}>
            {staffRoles.map((r) => <option key={r} value={r}>{roleLabels[r]}</option>)}
          </select>
        </Field>
        {needsDepartment && (
          <Field label="Bölüm">
            <select value={v.departmentId} onChange={(e) => setV({ ...v, departmentId: e.target.value })} required>
              <option value="">Seçin</option>
              {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </Field>
        )}
        <Field label="Başlangıç parolası" hint="Kullanıcı daha sonra değiştirebilir."><input type="password" value={v.password} onChange={(e) => setV({ ...v, password: e.target.value })} required autoComplete="new-password" /></Field>
        <button className="btn primary wide">Hesabı oluştur</button>
      </form>
    </Modal>
  );
}

/* ── Resmî tatiller ───────────────────────────────────────── */

function Holidays() {
  const [year, setYear] = useState(new Date().getFullYear());
  const list = useLoad(() => api.get<Holiday[]>(`/api/calendar/holidays?year=${year}`), [year]);
  const [date, setDate] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');

  async function add(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/calendar/holidays', { date, name: name.trim() });
      setDate('');
      setName('');
      list.reload();
    } catch (err) {
      setError(msg(err, 'Tatil eklenemedi.'));
    }
  }

  async function remove(h: Holiday) {
    if (!window.confirm(`${fmtDate(h.date)} ${h.name} tatili listeden kaldırılsın mı?`)) return;
    setError('');
    try {
      await api.del(`/api/calendar/holidays/${h.id}`);
      list.reload();
    } catch (err) {
      setError(msg(err, 'Tatil silinemedi.'));
    }
  }

  return (
    <div className="two-col">
      <Card title="Resmî tatiller" subtitle="Tatil günleri çalışma planında seçilemez ve izin hesabına katılmaz.">
        <div className="row" style={{ marginBottom: 12 }}>
          <label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label>
        </div>
        {(error || list.error) && <Alert>{error || list.error}</Alert>}
        {list.data && list.data.length === 0 && <Empty>Bu yıl için tatil tanımlı değil.</Empty>}
        {list.data && list.data.length > 0 && (
          <table className="table">
            <thead><tr><th>Tarih</th><th>Ad</th><th /></tr></thead>
            <tbody>
              {list.data.map((h) => (
                <tr key={h.id}><td>{fmtDate(h.date)}</td><td>{h.name}</td><td className="right-cell"><button className="btn small danger" onClick={() => remove(h)}>Kaldır</button></td></tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
      <Card title="Tatil ekle">
        <form onSubmit={add}>
          <Field label="Tarih"><input type="date" value={date} onChange={(e) => setDate(e.target.value)} required /></Field>
          <Field label="Ad"><input value={name} onChange={(e) => setName(e.target.value)} maxLength={150} required placeholder="Örn. Cumhuriyet Bayramı" /></Field>
          <button className="btn primary wide">Ekle</button>
        </form>
      </Card>
    </div>
  );
}

/* ── Sınav dönemleri ──────────────────────────────────────── */

function Exams() {
  const [year, setYear] = useState(new Date().getFullYear());
  const list = useLoad(() => api.get<ExamPeriod[]>(`/api/calendar/exam-periods?year=${year}`), [year]);
  const [name, setName] = useState('');
  const [start, setStart] = useState('');
  const [end, setEnd] = useState('');
  const [error, setError] = useState('');

  async function add(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/calendar/exam-periods', { name: name.trim(), startDate: start, endDate: end });
      setName('');
      setStart('');
      setEnd('');
      list.reload();
    } catch (err) {
      setError(msg(err, 'Sınav dönemi eklenemedi.'));
    }
  }

  async function remove(p: ExamPeriod) {
    if (!window.confirm(`${p.name} sınav dönemi kaldırılsın mı?`)) return;
    setError('');
    try {
      await api.del(`/api/calendar/exam-periods/${p.id}`);
      list.reload();
    } catch (err) {
      setError(msg(err, 'Sınav dönemi silinemedi.'));
    }
  }

  return (
    <div className="two-col">
      <Card title="Sınav dönemleri" subtitle="Sınav haftalarında haftalık en az 1 gün çalışma şartı aranmaz.">
        <div className="row" style={{ marginBottom: 12 }}>
          <label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label>
        </div>
        {(error || list.error) && <Alert>{error || list.error}</Alert>}
        {list.data && list.data.length === 0 && <Empty>Bu yıl için sınav dönemi tanımlı değil.</Empty>}
        {list.data && list.data.length > 0 && (
          <table className="table">
            <thead><tr><th>Ad</th><th>Başlangıç</th><th>Bitiş</th><th /></tr></thead>
            <tbody>
              {list.data.map((p) => (
                <tr key={p.id}><td>{p.name}</td><td>{fmtDate(p.startDate)}</td><td>{fmtDate(p.endDate)}</td><td className="right-cell"><button className="btn small danger" onClick={() => remove(p)}>Kaldır</button></td></tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
      <Card title="Sınav dönemi ekle">
        <form onSubmit={add}>
          <Field label="Ad"><input value={name} onChange={(e) => setName(e.target.value)} maxLength={150} required placeholder="Örn. Güz dönemi ara sınavları" /></Field>
          <Field label="Başlangıç"><input type="date" value={start} onChange={(e) => setStart(e.target.value)} required /></Field>
          <Field label="Bitiş"><input type="date" value={end} min={start} onChange={(e) => setEnd(e.target.value)} required /></Field>
          <button className="btn primary wide">Ekle</button>
        </form>
      </Card>
    </div>
  );
}

/* ── İzin ve devamsızlık sınırları ────────────────────────── */

function Policy() {
  const policy = useLoad(() => api.get<LeavePolicy>('/api/settings/leave'), []);
  const [v, setV] = useState<LeavePolicy | null>(null);
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const form = v ?? policy.data;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!form) return;
    setError('');
    setDone(false);
    try {
      setV(await api.put<LeavePolicy>('/api/settings/leave', form));
      setDone(true);
    } catch (err) {
      setError(msg(err, 'Ayarlar kaydedilemedi.'));
    }
  }

  const num = (key: keyof Omit<LeavePolicy, 'healthLeaveCountsTowardLimit'>) => (
    <input type="number" min={0} max={365} value={form?.[key] ?? 0} onChange={(e) => form && setV({ ...form, [key]: Number(e.target.value) })} required />
  );

  return (
    <Card title="İzin ve devamsızlık sınırları" subtitle="Sınırlar kod içinde sabit değildir; fakülte yönetimi buradan belirler. Raporlardaki uyarılar bu değerlere göre hesaplanır.">
      {(error || policy.error) && <Alert>{error || policy.error}</Alert>}
      {done && <Alert kind="info">Ayarlar kaydedildi.</Alert>}
      {form && (
        <form onSubmit={submit} style={{ maxWidth: 460 }}>
          <Field label="Yıllık izin hakkı (gün)">{num('leaveDayLimit')}</Field>
          <Field label="İzin uyarısı (kalan gün)" hint="Kalan izin bu değere indiğinde uyarı verilir.">{num('leaveWarningRemainingDays')}</Field>
          <Field label="Devamsızlık sınırı (gün)">{num('absenceDayLimit')}</Field>
          <Field label="Devamsızlık uyarısı (kalan gün)">{num('absenceWarningRemainingDays')}</Field>
          <label className="row" style={{ margin: '8px 0 14px' }}>
            <input type="checkbox" checked={form.healthLeaveCountsTowardLimit} onChange={(e) => setV({ ...form, healthLeaveCountsTowardLimit: e.target.checked })} />
            Sağlık izinleri izin hakkından düşsün
          </label>
          <button className="btn primary">Kaydet</button>
        </form>
      )}
    </Card>
  );
}
