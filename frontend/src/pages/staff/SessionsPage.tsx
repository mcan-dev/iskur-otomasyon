import { useEffect, useRef, useState, type FormEvent } from 'react';
import QRCode from 'qrcode';
import { api, ApiError } from '../../api/client';
import type { Attendance, Department, QrCode, Session, StudentSummary } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Badge, Card, Empty, Field, Modal } from '../../components/ui';
import { fmtDate, fmtDateTime, fmtTime, isoDate, todayIso } from '../../format';
import { useLoad } from '../../useLoad';

const addDays = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);

export default function SessionsPage() {
  const { user } = useAuth();
  const isFaculty = user?.role === 'FacultyAdmin';
  const [qrFor, setQrFor] = useState<Session | null>(null);
  const [recordsFor, setRecordsFor] = useState<Session | null>(null);
  const [error, setError] = useState('');

  const from = isoDate(addDays(new Date(), -14));
  const to = isoDate(addDays(new Date(), 30));
  const sessions = useLoad(() => api.get<Session[]>(`/api/attendance/sessions?from=${from}&to=${to}`), [from, to]);

  async function close(s: Session) {
    setError('');
    try {
      await api.post(`/api/attendance/sessions/${s.id}/close`);
      sessions.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Oturum kapatılamadı.');
    }
  }

  return (
    <div className="two-col">
      <Card title="Devam oturumları" subtitle="Çalışma günü oturum açın, karekodu ekranda gösterin; öğrenciler telefonlarıyla okutur.">
        {error && <Alert>{error}</Alert>}
        {sessions.error && <Alert>{sessions.error}</Alert>}
        {sessions.data?.length === 0 && <Empty>Son iki hafta ve gelecek 30 gün için oturum yok.</Empty>}
        {sessions.data && sessions.data.length > 0 && (
          <table className="table">
            <thead><tr><th>Tarih</th><th>Saat</th><th>Kapsam</th><th>Durum</th><th></th></tr></thead>
            <tbody>
              {sessions.data.map((s) => (
                <tr key={s.id}>
                  <td>{fmtDate(s.date)}{s.title && <div className="muted small">{s.title}</div>}</td>
                  <td>{fmtTime(s.startTime)} – {fmtTime(s.endTime)}</td>
                  <td>{s.departmentName ?? 'Tüm fakülte'}</td>
                  <td>{s.isClosed ? <Badge tone="gray">Kapalı</Badge> : s.isActiveNow ? <Badge tone="green">Açık</Badge> : <Badge tone="amber">Bekliyor</Badge>}</td>
                  <td>
                    <div className="row">
                      {!s.isClosed && <button className="btn small primary" onClick={() => setQrFor(s)}>Karekod</button>}
                      <button className="btn small" onClick={() => setRecordsFor(s)}>Katılanlar</button>
                      {!s.isClosed && <button className="btn small danger" onClick={() => close(s)}>Kapat</button>}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
      <NewSession isFaculty={isFaculty} onCreated={sessions.reload} />
      {qrFor && <QrModal session={qrFor} onClose={() => setQrFor(null)} />}
      {recordsFor && <RecordsModal session={recordsFor} canEdit={isFaculty} onClose={() => setRecordsFor(null)} />}
    </div>
  );
}

function NewSession({ isFaculty, onCreated }: { isFaculty: boolean; onCreated: () => void }) {
  const departments = useLoad(() => (isFaculty ? api.get<Department[]>('/api/departments') : Promise.resolve([])), [isFaculty]);
  const [date, setDate] = useState(todayIso());
  const [start, setStart] = useState('09:00');
  const [end, setEnd] = useState('17:00');
  const [title, setTitle] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [error, setError] = useState('');

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/attendance/sessions', {
        date,
        startTime: `${start}:00`,
        endTime: `${end}:00`,
        title: title.trim() || null,
        departmentId: isFaculty && departmentId ? Number(departmentId) : null,
      });
      setTitle('');
      onCreated();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Oturum açılamadı.');
    }
  }

  return (
    <Card title="Yeni oturum">
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <Field label="Tarih"><input type="date" value={date} onChange={(e) => setDate(e.target.value)} required /></Field>
        <div className="grid-2">
          <Field label="Başlangıç"><input type="time" value={start} onChange={(e) => setStart(e.target.value)} required /></Field>
          <Field label="Bitiş"><input type="time" value={end} onChange={(e) => setEnd(e.target.value)} required /></Field>
        </div>
        <Field label="Başlık (isteğe bağlı)"><input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={150} /></Field>
        {isFaculty ? (
          <Field label="Kapsam">
            <select value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
              <option value="">Tüm fakülte</option>
              {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </Field>
        ) : (
          <p className="muted small">Oturum kendi bölümünüz için açılır.</p>
        )}
        <button className="btn primary wide">Oturum aç</button>
      </form>
    </Card>
  );
}

/** Ekranda gösterilen karekod. Süre dolmadan yeni jeton istenir, kod hep güncel kalır. */
function QrModal({ session, onClose }: { session: Session; onClose: () => void }) {
  const canvas = useRef<HTMLCanvasElement>(null);
  const [qr, setQr] = useState<QrCode | null>(null);
  const [error, setError] = useState('');
  const [left, setLeft] = useState(1);

  useEffect(() => {
    let cancelled = false;
    let timer = 0;

    async function load() {
      try {
        const next = await api.get<QrCode>(`/api/attendance/sessions/${session.id}/qr`);
        if (cancelled) return;
        setQr(next);
        setError('');
        const wait = Math.max(1000, new Date(next.refreshAtUtc).getTime() - Date.now());
        timer = window.setTimeout(load, wait);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : 'Karekod alınamadı.');
        timer = window.setTimeout(load, 5000);
      }
    }
    load();
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [session.id]);

  useEffect(() => {
    if (qr && canvas.current) QRCode.toCanvas(canvas.current, qr.token, { width: 320, margin: 1 });
  }, [qr]);

  useEffect(() => {
    if (!qr) return;
    const id = window.setInterval(() => {
      const total = qr.windowSeconds * 1000;
      setLeft(Math.max(0, Math.min(1, (new Date(qr.refreshAtUtc).getTime() - Date.now()) / total)));
    }, 250);
    return () => window.clearInterval(id);
  }, [qr]);

  return (
    <Modal title={`Karekod · ${fmtDate(session.date)} ${fmtTime(session.startTime)}–${fmtTime(session.endTime)}`} onClose={onClose}>
      <div className="qr-box">
        {error && <Alert>{error}</Alert>}
        <canvas ref={canvas} />
        <div className="qr-bar"><i style={{ width: `${left * 100}%` }} /></div>
        <p className="muted small center">Kod {qr?.windowSeconds ?? 30} saniyede bir yenilenir. Öğrenciler telefonlarıyla bu kodu okutur.</p>
      </div>
    </Modal>
  );
}

function RecordsModal({ session, canEdit, onClose }: { session: Session; canEdit: boolean; onClose: () => void }) {
  const records = useLoad(() => api.get<Attendance[]>(`/api/attendance?sessionId=${session.id}&includeVoided=true`), [session.id]);
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [reason, setReason] = useState('');
  const [voiding, setVoiding] = useState<Attendance | null>(null);
  const [voidReason, setVoidReason] = useState('');
  const [error, setError] = useState('');

  async function add(e: FormEvent) {
    e.preventDefault();
    if (!student) return;
    setError('');
    try {
      await api.post('/api/attendance/manual', { studentId: student.id, sessionId: session.id, reason: reason.trim() });
      setStudent(null);
      setReason('');
      records.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kayıt eklenemedi.');
    }
  }

  async function voidRecord(e: FormEvent) {
    e.preventDefault();
    if (!voiding) return;
    setError('');
    try {
      await api.post(`/api/attendance/${voiding.id}/void`, { reason: voidReason.trim() });
      setVoiding(null);
      setVoidReason('');
      records.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kayıt iptal edilemedi.');
    }
  }

  return (
    <Modal title={`Katılanlar · ${fmtDate(session.date)}`} onClose={onClose}>
      {error && <Alert>{error}</Alert>}
      {records.data?.length === 0 && <Empty>Henüz kayıt yok.</Empty>}
      {records.data && records.data.length > 0 && (
        <table className="table">
          <thead><tr><th>Öğrenci</th><th>Zaman</th><th></th></tr></thead>
          <tbody>
            {records.data.map((r) => (
              <tr key={r.id}>
                <td>{r.studentName}<div className="muted small">{r.departmentName}</div></td>
                <td>
                  {fmtDateTime(r.checkedInAt)}
                  {r.isManual && <div className="muted small">Elle: {r.manualReason}</div>}
                  {r.isVoided && <div className="muted small">İptal: {r.voidReason}</div>}
                </td>
                <td>
                  {r.isVoided ? <Badge tone="gray">İptal</Badge> : canEdit && <button className="btn small danger" onClick={() => setVoiding(r)}>İptal et</button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {voiding && (
        <form onSubmit={voidRecord}>
          <Field label={`${voiding.studentName} kaydını iptal et: gerekçe`}>
            <input value={voidReason} onChange={(e) => setVoidReason(e.target.value)} required minLength={3} maxLength={500} />
          </Field>
          <button className="btn primary">İptal et</button>
        </form>
      )}
      {canEdit && !session.isClosed && (
        <form onSubmit={add}>
          <h3 className="sub">Elle devam ekle</h3>
          <Field label="Öğrenci"><StudentPicker value={student} onChange={setStudent} /></Field>
          <Field label="Gerekçe"><input value={reason} onChange={(e) => setReason(e.target.value)} required minLength={3} maxLength={500} /></Field>
          <button className="btn" disabled={!student}>Ekle</button>
        </form>
      )}
    </Modal>
  );
}
