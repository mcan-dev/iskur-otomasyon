import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import type { CourseDay, CourseSession, PlanCalendar, WorkPlan } from '../../api/types';
import { Alert, Badge, Card, MonthPicker } from '../../components/ui';
import { courseSessionLabels, fmtDate, fmtDateTime, hoursLabel, planStatusLabels, planTone, weekdays } from '../../format';
import { useLoad } from '../../useLoad';

const headers = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

export default function PlanPage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const calendar = useLoad(() => api.get<PlanCalendar>(`/api/work-plans/me/calendar?year=${year}&month=${month}`), [year, month]);
  const plan = useLoad(() => api.get<WorkPlan>(`/api/work-plans/me?year=${year}&month=${month}`).catch(() => null), [year, month]);

  const [picked, setPicked] = useState<Set<string>>(new Set());
  const [note, setNote] = useState('');
  const [message, setMessage] = useState<{ kind: 'error' | 'warn' | 'info'; text: string; iban?: boolean } | null>(null);
  const [busy, setBusy] = useState(false);

  // Takvim yüklenince seçimleri sunucudaki duruma getir.
  useEffect(() => {
    const next = new Set<string>();
    calendar.data?.days.forEach((d) => {
      if (d.isSelected) next.add(d.date);
    });
    setPicked(next);
    setNote(plan.data?.studentNote ?? '');
  }, [calendar.data, plan.data]);

  const cal = calendar.data;
  const canEdit = cal?.canEdit ?? false;

  function toggle(date: string) {
    setPicked((p) => {
      const copy = new Set(p);
      if (copy.has(date)) copy.delete(date);
      else copy.add(date);
      return copy;
    });
  }

  async function save() {
    setBusy(true);
    setMessage(null);
    try {
      await api.put('/api/work-plans/me', {
        year,
        month,
        studentNote: note.trim() || null,
        days: [...picked].sort().map((date) => ({ date })),
      });
      calendar.reload();
      plan.reload();
      setMessage({ kind: 'info', text: 'Plan kaydedildi ve onaya gönderildi.' });
    } catch (err) {
      setMessage(failure(err, 'Plan kaydedilemedi.'));
    } finally {
      setBusy(false);
    }
  }

  const offset = cal && cal.days.length ? (new Date(cal.days[0].date + 'T00:00:00').getDay() + 6) % 7 : 0;
  const status = cal?.planStatus ?? plan.data?.status ?? null;

  return (
    <div className="two-col">
      <div className="stack">
        <Card title="Aylık çalışma planı" subtitle="Çalışmak istediğiniz günleri seçin. Haftada en az 1, en fazla 3 gün seçilebilir.">
          <div className="row between" style={{ marginBottom: 12 }}>
            <MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />
            {status && <Badge tone={planTone[status]}>{planStatusLabels[status]}</Badge>}
          </div>

          {calendar.error && <Alert>{calendar.error}</Alert>}
          {message && <Alert kind={message.kind}>{message.text}{message.iban && <> <Link to="/profil">Profilime git</Link></>}</Alert>}
          {cal && !canEdit && <Alert kind="info">Bu ay için plan değiştirilemez (son düzeltme günü: {fmtDate(cal.editDeadline)}).</Alert>}
          {status === 'Rejected' && plan.data?.reviewNote && <Alert kind="warn">Planınız reddedildi: {plan.data.reviewNote}</Alert>}

          <div className="cal" style={{ marginTop: 12 }}>
            {headers.map((h) => (
              <div key={h} className="cal-head">{h}</div>
            ))}
            {Array.from({ length: offset }).map((_, i) => (
              <div key={`o${i}`} className="cal-cell empty-cell" />
            ))}
            {cal?.days.map((d) => {
              const chosen = picked.has(d.date);
              const selectable = canEdit && d.isSelectable;
              return (
                <div
                  key={d.date}
                  className={`cal-cell ${d.color} ${chosen ? 'selected' : ''}`}
                  onClick={() => selectable && toggle(d.date)}
                  title={d.label}
                  role={selectable ? 'button' : undefined}
                >
                  <span className="num">{Number(d.date.slice(8))}</span>
                  <span className="lbl">{d.label}</span>
                  {d.lessonSession && <span className="lbl">Ders: {courseSessionLabels[d.lessonSession].toLowerCase()}</span>}
                  {chosen && <span className="lbl">{hoursLabel(cal?.dailyHours)} saat</span>}
                </div>
              );
            })}
          </div>

          <div className="legend" style={{ marginTop: 12 }}>
            <span><i className="dot" style={{ background: '#bfe7d0' }} />Seçilebilir</span>
            <span><i className="dot" style={{ background: '#fdf1f1' }} />Tatil / kapalı</span>
            <span><i className="dot" style={{ background: '#fff8e6' }} />Ders veya sınav</span>
            <span><i className="dot" style={{ background: '#e4f1ff' }} />Seçtiğiniz gün</span>
          </div>

          <label className="field" style={{ marginTop: 16 }}>
            <span>Not (isteğe bağlı)</span>
            <textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} disabled={!canEdit} maxLength={1000} />
          </label>
          <div className="row">
            <button className="btn primary" disabled={!canEdit || busy || picked.size === 0} onClick={save}>
              {busy ? 'Kaydediliyor…' : 'Planı Kaydet'}
            </button>
            <span className="muted">{picked.size} gün seçildi · {hoursLabel(picked.size * (cal?.dailyHours ?? 0))} saat</span>
          </div>
        </Card>
      </div>

      <div className="stack">
        <Card title="Plan durumu">
          {plan.data ? (
            <dl className="kv">
              <dt>Durum</dt><dd><Badge tone={planTone[plan.data.status]}>{planStatusLabels[plan.data.status]}</Badge></dd>
              <dt>Planlanan gün</dt><dd>{plan.data.plannedDays}</dd>
              <dt>Planlanan saat</dt><dd>{plan.data.plannedHours}</dd>
              <dt>Son kayıt</dt><dd>{fmtDateTime(plan.data.lastSavedAt)}</dd>
              {plan.data.reviewedByName && (<><dt>İnceleyen</dt><dd>{plan.data.reviewedByName}</dd></>)}
              {plan.data.reviewNote && (<><dt>İnceleme notu</dt><dd>{plan.data.reviewNote}</dd></>)}
            </dl>
          ) : (
            <p className="muted">Bu ay için henüz plan oluşturmadınız.</p>
          )}
        </Card>
        <CourseDays onSaved={() => calendar.reload()} />
      </div>
    </div>
  );
}

function failure(err: unknown, fallback: string): { kind: 'error' | 'warn'; text: string; iban?: boolean } {
  if (!(err instanceof ApiError)) return { kind: 'error', text: fallback };
  if (err.fieldErrors.Iban || err.fieldErrors.iban) return { kind: 'warn', text: "IBAN'ı giriniz. Gün seçimini kaydedebilmek için önce IBAN bilginizi kaydetmelisiniz.", iban: true };
  return { kind: 'error', text: errorText(err) };
}

function errorText(err: ApiError): string {
  const details = Object.values(err.fieldErrors).flat();
  return details.length ? details.join(' ') : err.message;
}

function CourseDays({ onSaved }: { onSaved: () => void }) {
  const days = useLoad(() => api.get<CourseDay[]>('/api/students/me/course-days'), []);
  const [selected, setSelected] = useState<Record<string, CourseSession>>({});
  const [message, setMessage] = useState<{ kind: 'error' | 'warn' | 'info'; text: string; iban?: boolean } | null>(null);

  useEffect(() => {
    const next: Record<string, CourseSession> = {};
    days.data?.forEach((d) => { next[d.day] = d.session; });
    setSelected(next);
  }, [days.data]);

  async function save() {
    setMessage(null);
    try {
      await api.put('/api/students/me/course-days', { days: Object.entries(selected).map(([day, session]) => ({ day, session })) });
      setMessage({ kind: 'info', text: 'Ders günleriniz kaydedildi.' });
      days.reload();
      onSaved();
    } catch (err) {
      setMessage(failure(err, 'Kaydedilemedi.'));
    }
  }

  function toggle(day: string) {
    setSelected((s) => {
      const copy = { ...s };
      if (day in copy) delete copy[day];
      else copy[day] = 'FullDay';
      return copy;
    });
  }

  return (
    <Card title="Ders günlerim" subtitle="Derslerinizin olduğu günleri ve zamanını girin; takvimde işaretlenir.">
      {message && <Alert kind={message.kind}>{message.text}{message.iban && <> <Link to="/profil">Profilime git</Link></>}</Alert>}
      <div className="course-days">
        {weekdays.slice(0, 5).map((w) => {
          const on = w.value in selected;
          return (
            <div key={w.value} className={`course-day ${on ? '' : 'off'}`}>
              <label>
                <input type="checkbox" checked={on} onChange={() => toggle(w.value)} />
                {w.label}
              </label>
              <select value={selected[w.value] ?? 'FullDay'} disabled={!on} onChange={(e) => setSelected((s) => ({ ...s, [w.value]: e.target.value as CourseSession }))}>
                {(Object.keys(courseSessionLabels) as CourseSession[]).map((k) => (
                  <option key={k} value={k}>{courseSessionLabels[k]}</option>
                ))}
              </select>
            </div>
          );
        })}
      </div>
      <div className="row" style={{ marginTop: 12 }}>
        <button className="btn primary" onClick={save}>Kaydet</button>
      </div>
    </Card>
  );
}
