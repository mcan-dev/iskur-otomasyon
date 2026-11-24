import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import type {
  Absence, AttendanceComparison, Department, Leave, LeaveBalance, StudentDetailStaff, StudentTask, User, WorkLifetime, WorkPlan, WorkUnit,
} from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Badge, Card, Empty, Field, Modal, MonthPicker, Stat } from '../../components/ui';
import { fmtDate, outcomeLabels, outcomeTone, taskStatusLabels, taskTone, todayIso } from '../../format';
import { useLoad } from '../../useLoad';
import { AbsenceTable, LeaveTable, PlanItem } from './parts';

type Tab = 'general' | 'plans' | 'leaves' | 'absences' | 'attendance' | 'tasks';
const tabs: { id: Tab; label: string }[] = [
  { id: 'general', label: 'Genel' },
  { id: 'plans', label: 'Çalışma planı' },
  { id: 'leaves', label: 'İzinler' },
  { id: 'absences', label: 'Devamsızlık' },
  { id: 'attendance', label: 'Devam' },
  { id: 'tasks', label: 'Görevler' },
];

export default function StudentDetailPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const [tab, setTab] = useState<Tab>('general');
  const student = useLoad(() => api.get<StudentDetailStaff>(`/api/students/${id}`), [id]);
  const s = student.data;

  return (
    <div className="stack">
      <p><Link to="/ogrenciler">‹ Öğrenci listesi</Link></p>
      {student.error && <Alert>{student.error}</Alert>}
      {s && (
        <Card>
          <div className="row between">
            <div>
              <h2 style={{ fontSize: 20 }}>{s.fullName}</h2>
              <p className="muted">{s.studentNumber} · {s.departmentName} · {s.registrationYear} kaydı</p>
            </div>
            <Badge tone={s.isActive ? 'green' : 'gray'}>{s.isActive ? 'Aktif' : 'Pasif'}</Badge>
          </div>
        </Card>
      )}
      <div className="tabs">
        {tabs.map((t) => (
          <button key={t.id} className={`tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>{t.label}</button>
        ))}
      </div>
      {s && tab === 'general' && <General student={s} role={user?.role} onChanged={student.reload} />}
      {tab === 'plans' && <Plans studentId={id} canReview={user?.role === 'FacultyAdmin'} />}
      {s && tab === 'leaves' && <Leaves studentId={id} canCancel={user?.role === 'FacultyAdmin'} canEditAllowance={user?.role === 'FacultyAdmin'} allowance={s.leaveDayAllowance ?? null} onAllowanceChanged={student.reload} />}
      {tab === 'absences' && <Absences studentId={id} canRevoke={user?.role === 'FacultyAdmin'} />}
      {tab === 'attendance' && <Attendance studentId={id} />}
      {tab === 'tasks' && <Tasks studentId={id} />}
    </div>
  );
}

// ─── Genel ─────────────────────────────────────────────────────────────────

function General({ student: s, role, onChanged }: { student: StudentDetailStaff; role?: string; onChanged: () => void }) {
  const [dialog, setDialog] = useState<'unit' | 'advisor' | 'end' | 'start' | 'edit' | null>(null);
  const lifetime = useLoad(() => api.get<WorkLifetime>(`/api/work-plans/students/${s.id}/lifetime`), [s.id]);
  const isFaculty = role === 'FacultyAdmin';
  const canAdvisor = isFaculty || role === 'DepartmentHead';
  const close = () => setDialog(null);

  return (
    <div className="two-col">
      <div className="stack">
        <Card title="Kişisel bilgiler">
          <dl className="kv">
            <dt>E-posta</dt><dd>{s.email}</dd>
            <dt>Telefon</dt><dd>{s.phoneNumber}</dd>
            <dt>T.C. kimlik no</dt><dd>{s.nationalId}</dd>
            <dt>IBAN</dt><dd>{s.iban}</dd>
          </dl>
          {s.isPersonalDataMasked && <p className="muted small" style={{ marginTop: 8 }}>T.C. ve IBAN bilgisi yetkiniz gereği maskelidir.</p>}
        </Card>
        <Card title="Geçmiş">
          <h3 className="sub">Programa katılım</h3>
          <ul className="plain">
            {s.participations.map((x) => (
              <li key={x.id}>{fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}{x.endReason && <span className="muted"> ({x.endReason})</span>}</li>
            ))}
          </ul>
          <h3 className="sub">Çalışma birimi</h3>
          <ul className="plain">
            {s.unitHistory.length === 0 && <li className="muted">Kayıt yok</li>}
            {s.unitHistory.map((x) => <li key={x.id}>{x.workUnitName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>)}
          </ul>
          <h3 className="sub">Sorumlu personel</h3>
          <ul className="plain">
            {s.advisorHistory.length === 0 && <li className="muted">Kayıt yok</li>}
            {s.advisorHistory.map((x) => <li key={x.id}>{x.advisorName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>)}
          </ul>
        </Card>
      </div>
      <div className="stack">
      <Card title="Çalışma günü hakkı" subtitle={`Üniversite hayatı boyunca en fazla ${lifetime.data?.limit ?? 140} iş günü çalışılabilir.`}>
        {lifetime.error && <Alert>{lifetime.error}</Alert>}
        {lifetime.data && (
          <div className="stats">
            <Stat label="Çalışılan" value={`${lifetime.data.workedDays} gün`} />
            <Stat label="Kalan" value={`${lifetime.data.remainingDays} gün`} tone="green"
              hint={lifetime.data.upcomingPlannedDays > 0 ? `${lifetime.data.upcomingPlannedDays} gün planlarda bekliyor` : undefined} />
          </div>
        )}
      </Card>
      <Card title="Atamalar ve durum">
        <dl className="kv" style={{ gridTemplateColumns: '130px 1fr' }}>
          <dt>Birim</dt><dd>{s.currentUnit?.workUnitName ?? 'Atanmadı'}</dd>
          <dt>Sorumlu personel</dt><dd>{s.currentAdvisor?.advisorName ?? 'Atanmadı'}</dd>
        </dl>
        <div className="stack" style={{ marginTop: 14 }}>
          {isFaculty && <button className="btn" onClick={() => setDialog('edit')}>Öğrenci bilgilerini düzenle</button>}
          {isFaculty && s.isActive && <button className="btn" onClick={() => setDialog('unit')}>Birim ata / değiştir</button>}
          {canAdvisor && s.isActive && <button className="btn" onClick={() => setDialog('advisor')}>Sorumlu personel ata / değiştir</button>}
          {isFaculty && s.isActive && <button className="btn danger" onClick={() => setDialog('end')}>Programdan çıkar</button>}
          {isFaculty && !s.isActive && <button className="btn primary" onClick={() => setDialog('start')}>Programa yeniden dahil et</button>}
        </div>
      </Card>
      </div>
      {dialog === 'edit' && <EditStudent student={s} onClose={close} onDone={onChanged} />}
      {dialog === 'unit' && <AssignUnit student={s} onClose={close} onDone={onChanged} />}
      {dialog === 'advisor' && <AssignAdvisor student={s} onClose={close} onDone={onChanged} />}
      {dialog === 'end' && <EndParticipation student={s} onClose={close} onDone={onChanged} />}
      {dialog === 'start' && <StartParticipation student={s} onClose={close} onDone={onChanged} />}
    </div>
  );
}

interface DialogProps { student: StudentDetailStaff; onClose: () => void; onDone: () => void }

function useSubmit(action: () => Promise<unknown>, { onClose, onDone }: Pick<DialogProps, 'onClose' | 'onDone'>) {
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      await action();
      onDone();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İşlem yapılamadı.');
      setBusy(false);
    }
  }
  return { error, busy, submit };
}

function AssignUnit({ student, onClose, onDone }: DialogProps) {
  const units = useLoad(() => api.get<WorkUnit[]>('/api/work-units'), []);
  const [unitId, setUnitId] = useState('');
  const { error, busy, submit } = useSubmit(() => api.put(`/api/students/${student.id}/unit`, { workUnitId: Number(unitId) }), { onClose, onDone });
  return (
    <Modal title="Çalışma birimi ata" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <Field label="Birim">
          <select value={unitId} onChange={(e) => setUnitId(e.target.value)} required>
            <option value="">Seçin</option>
            {units.data?.map((u) => <option key={u.id} value={u.id}>{u.name}</option>)}
          </select>
        </Field>
        <p className="muted small">Önceki birim geçmişe taşınır.</p>
        <button className="btn primary wide" disabled={busy || !unitId}>Ata</button>
      </form>
    </Modal>
  );
}

function AssignAdvisor({ student, onClose, onDone }: DialogProps) {
  const advisors = useLoad(() => api.get<User[]>(`/api/users?role=Advisor&isActive=true&departmentId=${student.departmentId}`), []);
  const [registry, setRegistry] = useState('');
  const { error, busy, submit } = useSubmit(
    () => api.put(`/api/students/${student.id}/advisor`, { advisorRegistryNumber: registry.trim() }),
    { onClose, onDone },
  );
  return (
    <Modal title="Sorumlu personel ata" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <Field label="Sorumlu personelin sicil numarası">
          <input value={registry} onChange={(e) => setRegistry(e.target.value)} list="advisor-registry" required inputMode="numeric" />
          <datalist id="advisor-registry">
            {advisors.data?.map((a) => <option key={a.id} value={a.registryNumber ?? ''}>{a.fullName}</option>)}
          </datalist>
        </Field>
        <ul className="plain small muted">
          {advisors.data?.map((a) => (
            <li key={a.id}>
              <button type="button" className="link" onClick={() => setRegistry(a.registryNumber ?? '')}>{a.registryNumber}</button> · {a.fullName}
            </li>
          ))}
        </ul>
        <p className="muted small">Öğrencinin bölümündeki sorumlu personel atanabilir. Önceki sorumlu geçmişe taşınır.</p>
        <button className="btn primary wide" disabled={busy || !registry.trim()}>Ata</button>
      </form>
    </Modal>
  );
}

/** Sekreterlik öğrencinin bütün bilgilerini düzeltebilir (ad, e-posta, TC, numara, bölüm, IBAN, OBİS şifresi). */
function EditStudent({ student, onClose, onDone }: DialogProps) {
  const departments = useLoad(() => api.get<Department[]>('/api/departments'), []);
  const [form, setForm] = useState({
    fullName: student.fullName,
    email: student.email,
    phoneNumber: student.phoneNumber ?? '',
    nationalId: student.nationalId,
    studentNumber: student.studentNumber,
    departmentId: String(student.departmentId),
    registrationYear: String(student.registrationYear),
    iban: '',
    newObisPassword: '',
  });
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm((f) => ({ ...f, [k]: e.target.value }));
  const { error, busy, submit } = useSubmit(
    () => api.put(`/api/students/${student.id}`, {
      ...form,
      departmentId: Number(form.departmentId),
      registrationYear: Number(form.registrationYear),
      iban: form.iban.trim() || null,
      newObisPassword: form.newObisPassword || null,
    }),
    { onClose, onDone },
  );
  return (
    <Modal title="Öğrenci bilgilerini düzenle" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <Field label="Ad soyad"><input value={form.fullName} onChange={set('fullName')} required /></Field>
        <Field label="Öğrenci e-postası"><input type="email" value={form.email} onChange={set('email')} required /></Field>
        <Field label="Telefon"><input value={form.phoneNumber} onChange={set('phoneNumber')} required /></Field>
        <Field label="T.C. kimlik no"><input value={form.nationalId} onChange={set('nationalId')} maxLength={11} required /></Field>
        <Field label="Öğrenci numarası"><input value={form.studentNumber} onChange={set('studentNumber')} maxLength={9} required /></Field>
        <Field label="Bölüm">
          <select value={form.departmentId} onChange={set('departmentId')}>
            {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </Field>
        <Field label="Kayıt yılı"><input type="number" value={form.registrationYear} onChange={set('registrationYear')} required /></Field>
        <Field label="Yeni IBAN (boş bırakılırsa değişmez)"><input value={form.iban} onChange={set('iban')} placeholder="TR.." /></Field>
        <Field label="Yeni OBİS şifresi (boş bırakılırsa değişmez)"><input type="password" value={form.newObisPassword} onChange={set('newObisPassword')} autoComplete="new-password" /></Field>
        <p className="muted small">Bölüm değişirse mevcut sorumlu personel ataması kapanır.</p>
        <button className="btn primary wide" disabled={busy}>Kaydet</button>
      </form>
    </Modal>
  );
}

function EndParticipation({ student, onClose, onDone }: DialogProps) {
  const [date, setDate] = useState(todayIso());
  const [reason, setReason] = useState('');
  const { error, busy, submit } = useSubmit(() => api.post(`/api/students/${student.id}/end-participation`, { endDate: date, reason: reason.trim() }), { onClose, onDone });
  return (
    <Modal title="Programdan çıkar" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <p className="muted small">Kayıt silinmez; öğrenci pasif olur ve geçmişi korunur. Resmî ilişik kesme kararı için “İlişik Kesme” ekranını kullanın.</p>
        <Field label="Bitiş tarihi"><input type="date" value={date} onChange={(e) => setDate(e.target.value)} required /></Field>
        <Field label="Gerekçe"><textarea rows={3} value={reason} onChange={(e) => setReason(e.target.value)} required minLength={3} maxLength={1000} /></Field>
        <button className="btn primary wide" disabled={busy}>Programdan çıkar</button>
      </form>
    </Modal>
  );
}

function StartParticipation({ student, onClose, onDone }: DialogProps) {
  const [date, setDate] = useState(todayIso());
  const { error, busy, submit } = useSubmit(() => api.post(`/api/students/${student.id}/start-participation`, { startDate: date }), { onClose, onDone });
  return (
    <Modal title="Programa yeniden dahil et" onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <p className="muted small">Yeni bir katılım dönemi açılır; önceki dönemler geçmişte kalır.</p>
        <Field label="Başlangıç tarihi"><input type="date" value={date} onChange={(e) => setDate(e.target.value)} required /></Field>
        <button className="btn primary wide" disabled={busy}>Dahil et</button>
      </form>
    </Modal>
  );
}

// ─── Sekmeler ──────────────────────────────────────────────────────────────

function Plans({ studentId, canReview }: { studentId: number; canReview: boolean }) {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const plans = useLoad(() => api.get<WorkPlan[]>(`/api/work-plans?year=${year}&month=${month}&studentId=${studentId}`), [studentId, year, month]);
  return (
    <Card title="Aylık çalışma planı">
      <div style={{ marginBottom: 12 }}><MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} /></div>
      {plans.error && <Alert>{plans.error}</Alert>}
      {plans.data?.length === 0 && <Empty>Bu ay için plan yok.</Empty>}
      <div className="stack">{plans.data?.map((p) => <PlanItem key={p.id} plan={p} canReview={canReview} showStudent={false} onChanged={plans.reload} />)}</div>
    </Card>
  );
}

function AllowanceCard({ studentId, allowance, policyLimit, onChanged }: { studentId: number; allowance: number | null; policyLimit: number | null; onChanged: () => void }) {
  const [value, setValue] = useState(allowance === null ? '' : String(allowance));
  const [message, setMessage] = useState<{ kind: 'error' | 'info'; text: string } | null>(null);

  async function save(days: number | null) {
    setMessage(null);
    try {
      await api.put(`/api/students/${studentId}/leave-allowance`, { days });
      setValue(days === null ? '' : String(days));
      setMessage({ kind: 'info', text: days === null ? 'Genel izin hakkına dönüldü.' : 'Öğrencinin izin hakkı güncellendi.' });
      onChanged();
    } catch (err) {
      setMessage({ kind: 'error', text: err instanceof ApiError ? err.message : 'Kaydedilemedi.' });
    }
  }

  return (
    <Card title="İzin hakkını düzenle" subtitle={allowance === null ? `Bu öğrenci genel izin hakkını kullanıyor (${policyLimit ?? ''} gün).` : `Bu öğrenciye özel ${allowance} gün tanımlı.`}>
      {message && <Alert kind={message.kind}>{message.text}</Alert>}
      <div className="row">
        <input type="number" min={0} max={365} value={value} onChange={(e) => setValue(e.target.value)} placeholder="Gün" style={{ width: 110 }} />
        <button className="btn primary" disabled={value === ''} onClick={() => save(Number(value))}>Kaydet</button>
        {allowance !== null && <button className="btn" onClick={() => save(null)}>Genel hakka dön</button>}
      </div>
    </Card>
  );
}

function Leaves({ studentId, canCancel, canEditAllowance = false, allowance, onAllowanceChanged }: { studentId: number; canCancel: boolean; canEditAllowance?: boolean; allowance: number | null; onAllowanceChanged: () => void }) {
  const [year, setYear] = useState(new Date().getFullYear());
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves?studentId=${studentId}&year=${year}`), [studentId, year]);
  const balance = useLoad(() => api.get<LeaveBalance>(`/api/leaves/students/${studentId}/balance?year=${year}`), [studentId, year]);
  const b = balance.data;
  return (
    <div className="stack">
      <div className="row"><label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label></div>
      {b && (
        <div className="stats">
          <Stat label="İzin hakkı" value={`${b.limit} gün`} />
          <Stat label="Kullanılan" value={`${b.used} gün`} tone="amber" />
          <Stat label="Kalan" value={`${b.remaining} gün`} tone="green" />
        </div>
      )}
      {b?.warnings.map((w) => <Alert key={w.code} kind="warn">{w.message}</Alert>)}
      {canEditAllowance && b && <AllowanceCard studentId={studentId} allowance={allowance} policyLimit={allowance === null ? b.limit : null} onChanged={() => { onAllowanceChanged(); balance.reload(); }} />}
      <Card title="İzinler" subtitle="Öğrenci izinleri onay beklemez; sekreterlik gerekirse iptal eder.">
        {leaves.error && <Alert>{leaves.error}</Alert>}
        {leaves.data && <LeaveTable leaves={leaves.data} canCancel={canCancel} showStudent={false} onChanged={() => { leaves.reload(); balance.reload(); }} />}
      </Card>
    </div>
  );
}

function Absences({ studentId, canRevoke }: { studentId: number; canRevoke: boolean }) {
  const [year, setYear] = useState(new Date().getFullYear());
  const absences = useLoad(() => api.get<Absence[]>(`/api/absences?studentId=${studentId}&year=${year}`), [studentId, year]);
  return (
    <Card title="Devamsızlık kayıtları">
      <div className="row" style={{ marginBottom: 12 }}><label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label></div>
      {absences.error && <Alert>{absences.error}</Alert>}
      {absences.data && <AbsenceTable absences={absences.data} canRevoke={canRevoke} showStudent={false} onChanged={absences.reload} />}
    </Card>
  );
}

function Attendance({ studentId }: { studentId: number }) {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const cmp = useLoad(() => api.get<AttendanceComparison>(`/api/attendance/students/${studentId}/comparison?year=${year}&month=${month}`), [studentId, year, month]);
  const c = cmp.data;
  return (
    <Card title="Planlanan ve gerçekleşen devam">
      <div style={{ marginBottom: 12 }}><MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} /></div>
      {cmp.error && <Alert>{cmp.error}</Alert>}
      {c && !c.hasApprovedPlan && <Alert kind="info">Bu ay için onaylı plan yok.</Alert>}
      {c && c.hasApprovedPlan && (
        <>
          <div className="stats">
            <Stat label="Planlanan gün" value={c.plannedDays} />
            <Stat label="Gelinen gün" value={c.attendedDays} tone="green" />
            <Stat label="İzinli" value={c.leaveDays} tone="blue" />
            <Stat label="Devamsızlık" value={c.recordedAbsenceDays} tone="red" />
            <Stat label="Kayıt yok" value={c.missingDays} tone="amber" />
          </div>
          <table className="table" style={{ marginTop: 14 }}>
            <thead><tr><th>Tarih</th><th>Durum</th></tr></thead>
            <tbody>
              {c.days.map((d) => (
                <tr key={d.date}><td>{fmtDate(d.date)}</td><td><Badge tone={outcomeTone[d.outcome]}>{outcomeLabels[d.outcome]}</Badge></td></tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </Card>
  );
}

function Tasks({ studentId }: { studentId: number }) {
  const tasks = useLoad(() => api.get<StudentTask[]>(`/api/tasks?studentId=${studentId}`), [studentId]);
  return (
    <Card title="Görevler ve değerlendirmeler">
      {tasks.error && <Alert>{tasks.error}</Alert>}
      {tasks.data?.length === 0 && <Empty>Görev yok.</Empty>}
      <div className="stack">
        {tasks.data?.map((t) => (
          <div key={t.id} className="list-item">
            <div className="row between">
              <strong>{t.title}</strong>
              <div className="row">{t.isOverdue && <Badge tone="red">Süresi geçti</Badge>}<Badge tone={taskTone[t.status]}>{taskStatusLabels[t.status]}</Badge></div>
            </div>
            <p className="muted small">{t.assignedByName} · Son tarih {fmtDate(t.dueDate)} · {t.evaluationCount} not</p>
          </div>
        ))}
      </div>
    </Card>
  );
}
