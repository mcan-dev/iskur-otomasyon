import { useEffect, useState } from 'react';
import { api, ApiError } from '../../api/client';
import type { Leave, LeaveCalendar } from '../../api/types';
import { Alert, Badge, Card, Empty, Field, MonthPicker, Stat } from '../../components/ui';
import { fmtDate, leaveStatusLabels, leaveTone, todayIso } from '../../format';
import { useLoad } from '../../useLoad';

const headers = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

/**
 * İzin: öğrencinin önüne o ayın çalışma takvimi açılır, yalnızca onaylı plandaki çalışma günleri seçilebilir.
 * Tür ve gerekçe yoktur, açıklama isteğe bağlıdır. İzin onay beklemez; sekreterlik bilgilendirilir.
 * Ayın bütün çalışma günleri için izin alınamaz (ayda en az bir gün çalışılmalıdır).
 */
export default function LeavesPage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const calendar = useLoad(() => api.get<LeaveCalendar>(`/api/leaves/me/calendar?year=${year}&month=${month}`), [year, month]);
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves/me?year=${year}`), [year]);

  const [picked, setPicked] = useState<Set<string>>(new Set());
  const [description, setDescription] = useState('');
  const [message, setMessage] = useState<{ kind: 'error' | 'warn' | 'info'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => setPicked(new Set()), [calendar.data]);

  const cal = calendar.data;
  const b = cal?.balance;
  const offset = cal && cal.days.length ? (new Date(cal.days[0].date + 'T00:00:00').getDay() + 6) % 7 : 0;

  const workDays = cal?.days.filter((d) => d.isWorkDay) ?? [];
  const freeWorkDays = workDays.filter((d) => !d.isOnLeave).length;

  function toggle(date: string) {
    setMessage(null);
    setPicked((p) => {
      const copy = new Set(p);
      if (copy.has(date)) {
        copy.delete(date);
        return copy;
      }
      if (copy.size + 1 >= freeWorkDays) {
        setMessage({ kind: 'warn', text: 'Bir ay içinde en az bir gün çalışmanız gerekir; ayın tüm çalışma günleri için izin alamazsınız.' });
        return copy;
      }
      copy.add(date);
      return copy;
    });
  }

  async function submit() {
    setBusy(true);
    setMessage(null);
    try {
      const created = await api.post<Leave[]>('/api/leaves/me', {
        days: [...picked].sort(),
        description: description.trim() || null,
      });
      setDescription('');
      setMessage({ kind: 'info', text: `${created.length} gün için izniniz kaydedildi. Fakülte sekreterliği bilgilendirildi.` });
      calendar.reload();
      leaves.reload();
    } catch (err) {
      setMessage({ kind: 'error', text: errorText(err, 'İzin alınamadı.') });
    } finally {
      setBusy(false);
    }
  }

  async function cancel(leave: Leave) {
    setMessage(null);
    try {
      await api.post(`/api/leaves/me/${leave.id}/cancel`);
      calendar.reload();
      leaves.reload();
    } catch (err) {
      setMessage({ kind: 'error', text: errorText(err, 'İzin iptal edilemedi.') });
    }
  }

  const today = todayIso();

  return (
    <div className="stack">
      <div className="stats">
        <Stat label={`${year} izin hakkı`} value={b ? `${b.limit} gün` : '–'} tone="blue" />
        <Stat label="Kullanılan" value={b ? `${b.used} gün` : '–'} hint={b ? `${b.leaveDays} gün izin, ${b.absenceDays} gün devamsızlık` : undefined} tone="amber" />
        <Stat label="Kalan" value={b ? `${b.remaining} gün` : '–'} tone="green" />
      </div>
      {b?.warnings.map((w, i) => (
        <Alert key={i} kind="warn">{w.message}</Alert>
      ))}

      <div className="two-col">
        <Card title="İzin al" subtitle="Takvimden izin almak istediğiniz çalışma günlerini seçin. Çalışma günü olmayan günler seçilemez.">
          <div className="row between" style={{ marginBottom: 12 }}>
            <MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />
          </div>
          {calendar.error && <Alert>{calendar.error}</Alert>}
          {message && <Alert kind={message.kind}>{message.text}</Alert>}
          {cal && !cal.hasApprovedPlan && (
            <Alert kind="info">Bu ay için onaylanmış bir çalışma planınız yok; izin yalnızca onaylı plandaki çalışma günleri için alınabilir.</Alert>
          )}

          <div className="cal" style={{ marginTop: 12 }}>
            {headers.map((h) => (
              <div key={h} className="cal-head">{h}</div>
            ))}
            {Array.from({ length: offset }).map((_, i) => (
              <div key={`o${i}`} className="cal-cell empty-cell" />
            ))}
            {cal?.days.map((d) => {
              const chosen = picked.has(d.date);
              const cls = d.isOnLeave ? 'leaveOn' : chosen ? 'leavePick' : d.isWorkDay ? 'pickGreen' : 'Gray';
              const label = d.isOnLeave ? 'İzinli' : chosen ? 'İzin için seçildi' : d.isWorkDay ? (d.canSelect ? 'Çalışma günü' : 'Çalışma günü (geçti)') : '';
              return (
                <div
                  key={d.date}
                  className={`cal-cell ${cls} ${d.canSelect ? 'clickable' : ''}`}
                  onClick={() => d.canSelect && toggle(d.date)}
                  role={d.canSelect ? 'button' : undefined}
                  title={label}
                >
                  <span className="num">{Number(d.date.slice(8))}</span>
                  {label && <span className="lbl">{label}</span>}
                </div>
              );
            })}
          </div>

          <div className="legend" style={{ marginTop: 12 }}>
            <span><i className="dot" style={{ background: '#c9efd8' }} />Çalışma günü</span>
            <span><i className="dot" style={{ background: '#fff1c7' }} />İzin için seçtiğiniz gün</span>
            <span><i className="dot" style={{ background: '#e4f1ff' }} />İzinli gün</span>
          </div>

          <Field label="Açıklama (isteğe bağlı)">
            <textarea rows={2} value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} />
          </Field>
          <div className="row">
            <button className="btn primary" disabled={busy || picked.size === 0} onClick={submit}>
              {busy ? 'Kaydediliyor…' : 'İzin Al'}
            </button>
            <span className="muted">{picked.size} gün seçildi</span>
          </div>
        </Card>

        <Card title={`${year} izinlerim`}>
          {leaves.error && <Alert>{leaves.error}</Alert>}
          {leaves.data?.length === 0 && <Empty>Bu yıl için izniniz yok.</Empty>}
          <div className="stack">
            {leaves.data?.map((l) => (
              <div key={l.id} className="list-item">
                <div className="row between">
                  <strong>
                    {fmtDate(l.startDate)}
                    {l.startDate !== l.endDate && ` – ${fmtDate(l.endDate)}`}
                  </strong>
                  <Badge tone={leaveTone[l.status]}>{leaveStatusLabels[l.status]}</Badge>
                </div>
                <p className="muted small">{l.daysCounted} gün · Alındığı gün {fmtDate(l.appliedOn)}</p>
                {l.description && <p>{l.description}</p>}
                {l.status === 'Cancelled' && (
                  <p className="muted small">
                    İptal eden: {l.cancelledByName ?? '—'}{l.cancelNote ? ` · ${l.cancelNote}` : ''}
                  </p>
                )}
                {l.status === 'Approved' && l.startDate >= today && (
                  <div className="row" style={{ marginTop: 8 }}>
                    <button className="btn small danger" onClick={() => cancel(l)}>İzni iptal et</button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}

function errorText(err: unknown, fallback: string): string {
  if (!(err instanceof ApiError)) return fallback;
  const details = Object.values(err.fieldErrors).flat();
  return details.length ? details.join(' ') : err.message;
}
