import { useState } from 'react';
import { api } from '../../api/client';
import type { Absence, Attendance, AttendanceComparison, Leave, WorkPlan } from '../../api/types';
import { Alert, Badge, Card, Empty, MonthPicker } from '../../components/ui';
import {
  fmtDate, fmtDateTime, hoursLabel, leaveStatusLabels, leaveTone, monthYear, outcomeLabels, outcomeTone, planStatusLabels, planTone,
} from '../../format';
import { useLoad } from '../../useLoad';

type Tab = 'summary' | 'plans' | 'leaves' | 'absences' | 'attendance';
const tabs: { id: Tab; label: string }[] = [
  { id: 'summary', label: 'Çalışılan günler' },
  { id: 'plans', label: 'Çalışma planları' },
  { id: 'leaves', label: 'İzinler' },
  { id: 'absences', label: 'Devamsızlıklar' },
  { id: 'attendance', label: 'Devam kayıtları' },
];

export default function HistoryPage() {
  const thisYear = new Date().getFullYear();
  const [year, setYear] = useState(thisYear);
  const [tab, setTab] = useState<Tab>('summary');
  const years = Array.from({ length: 6 }, (_, i) => thisYear - i);

  return (
    <Card title="Geçmiş kayıtlarım" subtitle="Önceki ay ve yıllara ait kayıtlarınız.">
      <div className="row between" style={{ marginBottom: 12 }}>
        <div className="tabs">
          {tabs.map((t) => (
            <button key={t.id} className={`tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>
              {t.label}
            </button>
          ))}
        </div>
        {tab !== 'summary' && (
        <select value={year} onChange={(e) => setYear(Number(e.target.value))}>
          {years.map((y) => <option key={y} value={y}>{y}</option>)}
        </select>
        )}
      </div>
      {tab === 'summary' && <Summary />}
      {tab === 'plans' && <Plans year={year} />}
      {tab === 'leaves' && <Leaves year={year} />}
      {tab === 'absences' && <Absences year={year} />}
      {tab === 'attendance' && <Records year={year} />}
    </Card>
  );
}

const weekdayName = (iso: string) => new Date(`${iso}T12:00:00`).toLocaleDateString('tr-TR', { weekday: 'long' });

/** Seçilen ayda kaç saat çalışıldığını ve hangi günlerde çalışıldığını gösterir. */
function Summary() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const data = useLoad(() => api.get<AttendanceComparison>(`/api/attendance/me/comparison?year=${year}&month=${month}`), [year, month]);
  const c = data.data;

  return (
    <div className="stack">
      <MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />
      {data.error && <Alert>{data.error}</Alert>}
      {c && !c.hasApprovedPlan && <Empty>Bu ay için onaylı çalışma planı yok.</Empty>}
      {c && c.hasApprovedPlan && (
        <>
          <div className="tiles">
            <div className="tile-card tone-bg-green">
              <div>
                <span className="tile-label">Çalışılan saat</span>
                <strong className="tile-value">{hoursLabel(c.workedHours)} saat</strong>
                <span className="tile-hint">Günlük {hoursLabel(c.dailyHours)} saat</span>
              </div>
            </div>
            <div className="tile-card">
              <div>
                <span className="tile-label">Çalışılan gün</span>
                <strong className="tile-value">{c.attendedDays} / {c.plannedDays}</strong>
                <span className="tile-hint">Planlanan günler içinde</span>
              </div>
            </div>
            <div className="tile-card tone-bg-amber">
              <div>
                <span className="tile-label">İzinli / devamsız gün</span>
                <strong className="tile-value">{c.leaveDays} / {c.recordedAbsenceDays + c.missingDays}</strong>
                <span className="tile-hint">{monthYear(year, month)}</span>
              </div>
            </div>
          </div>
          <table className="table">
            <thead><tr><th>Tarih</th><th>Gün</th><th>Durum</th><th>Çalışılan saat</th></tr></thead>
            <tbody>
              {c.days.map((d) => (
                <tr key={d.date}>
                  <td>{fmtDate(d.date)}</td>
                  <td>{weekdayName(d.date)}</td>
                  <td><Badge tone={outcomeTone[d.outcome]}>{outcomeLabels[d.outcome]}</Badge></td>
                  <td>{d.outcome === 'Attended' ? `${hoursLabel(c.dailyHours)} saat` : '–'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  );
}

function Plans({ year }: { year: number }) {
  const plans = useLoad(() => api.get<WorkPlan[]>('/api/work-plans/me/history'), []);
  const rows = (plans.data ?? []).filter((p) => p.year === year).sort((a, b) => b.month - a.month);
  if (plans.error) return <Alert>{plans.error}</Alert>;
  if (rows.length === 0) return <Empty>Bu yıl için plan yok.</Empty>;
  return (
    <table className="table">
      <thead><tr><th>Dönem</th><th>Gün</th><th>Saat</th><th>Durum</th><th>Not</th></tr></thead>
      <tbody>
        {rows.map((p) => (
          <tr key={p.id}>
            <td>{monthYear(p.year, p.month)}</td>
            <td>{p.plannedDays}</td>
            <td>{p.plannedHours}</td>
            <td><Badge tone={planTone[p.status]}>{planStatusLabels[p.status]}</Badge></td>
            <td>{p.reviewNote ?? ''}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Leaves({ year }: { year: number }) {
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves/me?year=${year}`), [year]);
  if (leaves.error) return <Alert>{leaves.error}</Alert>;
  if (!leaves.data?.length) return <Empty>Bu yıl için izin kaydı yok.</Empty>;
  return (
    <table className="table">
      <thead><tr><th>Tarih</th><th>Süre</th><th>Gerekçe</th><th>Durum</th></tr></thead>
      <tbody>
        {leaves.data.map((l) => (
          <tr key={l.id}>
            <td>{fmtDate(l.startDate)}{l.startDate !== l.endDate && ` – ${fmtDate(l.endDate)}`}</td>
            <td>{l.hours ? `${hoursLabel(l.hours)} saat` : `${l.daysCounted} gün`}</td>
            <td>{l.reason}</td>
            <td><Badge tone={leaveTone[l.status]}>{leaveStatusLabels[l.status]}</Badge></td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Absences({ year }: { year: number }) {
  const absences = useLoad(() => api.get<Absence[]>(`/api/absences/me?year=${year}`), [year]);
  if (absences.error) return <Alert>{absences.error}</Alert>;
  if (!absences.data?.length) return <Empty>Bu yıl için devamsızlık kaydı yok.</Empty>;
  return (
    <table className="table">
      <thead><tr><th>Tarih</th><th>Not</th><th></th></tr></thead>
      <tbody>
        {absences.data.map((a) => (
          <tr key={a.id}>
            <td>{fmtDate(a.date)}</td>
            <td>{a.note ?? ''}</td>
            <td>{a.isRevoked && <Badge tone="gray">Geri alındı</Badge>}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Records({ year }: { year: number }) {
  const records = useLoad(() => api.get<Attendance[]>(`/api/attendance/me?year=${year}`), [year]);
  if (records.error) return <Alert>{records.error}</Alert>;
  if (!records.data?.length) return <Empty>Bu yıl için devam kaydı yok.</Empty>;
  return (
    <table className="table">
      <thead><tr><th>Tarih</th><th>Kayıt zamanı</th><th></th></tr></thead>
      <tbody>
        {records.data.map((r) => (
          <tr key={r.id}>
            <td>{fmtDate(r.date)}</td>
            <td>{fmtDateTime(r.checkedInAt)}</td>
            <td>{r.isVoided ? <Badge tone="gray">İptal edildi</Badge> : r.isManual ? <Badge tone="blue">Elle girildi</Badge> : <Badge tone="green">Karekod</Badge>}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
