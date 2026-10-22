import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { AbsenceNotice, StudentSummary } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Badge, Card, Empty, Field } from '../../components/ui';
import { fmtDate, fmtDateTime, todayIso } from '../../format';
import { useLoad } from '../../useLoad';

export default function NoticesPage() {
  const { user } = useAuth();
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [error, setError] = useState('');
  const notices = useLoad(() => api.get<AbsenceNotice[]>(`/api/absence-notices${unreadOnly ? '?unreadOnly=true' : ''}`), [unreadOnly]);
  const isHead = user?.role === 'DepartmentHead';

  async function markRead(n: AbsenceNotice) {
    setError('');
    try {
      await api.post(`/api/absence-notices/${n.id}/read`);
      notices.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İşaretlenemedi.');
    }
  }

  return (
    <div className={user?.role === 'Advisor' ? 'two-col' : undefined}>
      <Card title="Devamsızlık bildirimleri" subtitle={isHead ? 'Danışmanlardan gelen bildirimleri inceleyip okundu olarak işaretleyin.' : 'Bölüm başkanına gönderilen bildirimler.'}>
        <label className="row" style={{ marginBottom: 12 }}>
          <input type="checkbox" checked={unreadOnly} onChange={(e) => setUnreadOnly(e.target.checked)} /> Yalnızca okunmamışlar
        </label>
        {error && <Alert>{error}</Alert>}
        {notices.error && <Alert>{notices.error}</Alert>}
        {notices.data?.length === 0 && <Empty>Bildirim yok.</Empty>}
        <div className="stack">
          {notices.data?.map((n) => (
            <div key={n.id} className="list-item">
              <div className="row between">
                <strong>{n.studentName} · {fmtDate(n.date)}</strong>
                {n.isRead ? <Badge tone="gray">Okundu</Badge> : <Badge tone="amber">Yeni</Badge>}
              </div>
              <p className="muted small">{n.departmentName} · Danışman {n.advisorName} · {fmtDateTime(n.createdAt)}</p>
              <p>{n.message}</p>
              {n.isRead && n.readByName && <p className="muted small">{n.readByName} · {fmtDateTime(n.readAt)}</p>}
              {isHead && !n.isRead && <button className="btn small primary" onClick={() => markRead(n)}>Okundu işaretle</button>}
            </div>
          ))}
        </div>
      </Card>
      {user?.role === 'Advisor' && <SendNotice onDone={notices.reload} />}
    </div>
  );
}

function SendNotice({ onDone }: { onDone: () => void }) {
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [date, setDate] = useState(todayIso());
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [ok, setOk] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!student) return;
    setError('');
    setOk(false);
    try {
      await api.post('/api/absence-notices', { studentId: student.id, date, message: message.trim() });
      setOk(true);
      setMessage('');
      setStudent(null);
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Bildirim gönderilemedi.');
    }
  }

  return (
    <Card title="Bildirim gönder" subtitle="Öğrencinin gelmediği veya devamda sorun yaşandığı günü bölüm başkanına bildirin.">
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        {ok && <Alert kind="info">Bildirim gönderildi.</Alert>}
        <Field label="Öğrenci"><StudentPicker value={student} onChange={setStudent} /></Field>
        <Field label="Gün"><input type="date" value={date} max={todayIso()} onChange={(e) => setDate(e.target.value)} required /></Field>
        <Field label="Açıklama"><textarea rows={4} value={message} onChange={(e) => setMessage(e.target.value)} required minLength={3} maxLength={1000} /></Field>
        <button className="btn primary wide" disabled={!student}>Gönder</button>
      </form>
    </Card>
  );
}
