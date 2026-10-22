import { useState, type FormEvent, type ReactNode } from 'react';
import { api, ApiError } from '../../api/client';
import type { Absence, Leave, WorkPlan } from '../../api/types';
import { Alert, Badge, Empty, Field, Modal } from '../../components/ui';
import {
  absenceTypeLabels, fmtDate, fmtDateTime, leaveStatusLabels, leaveTone, leaveTypeLabels, monthYear, planStatusLabels, planTone,
} from '../../format';

const msg = (err: unknown, fallback: string) => (err instanceof ApiError ? err.message : fallback);

/** Onay / ret penceresi: gerekçe notu ile birlikte. */
function ReviewModal({ title, onClose, onDone, url }: { title: string; onClose: () => void; onDone: () => void; url: string }) {
  const [approve, setApprove] = useState(true);
  const [note, setNote] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api.post(url, { approve, note: note.trim() || null });
      onDone();
      onClose();
    } catch (err) {
      setError(msg(err, 'İşlem yapılamadı.'));
      setBusy(false);
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <div className="checks" style={{ marginBottom: 12 }}>
          <label><input type="radio" checked={approve} onChange={() => setApprove(true)} /> Onayla</label>
          <label><input type="radio" checked={!approve} onChange={() => setApprove(false)} /> Reddet</label>
        </div>
        <Field label={approve ? 'Not (isteğe bağlı)' : 'Ret gerekçesi'}>
          <textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} required={!approve} maxLength={1000} />
        </Field>
        <button className="btn primary wide" disabled={busy}>Kaydet</button>
      </form>
    </Modal>
  );
}

export function PlanItem({ plan, canReview, onChanged, showStudent = true }: { plan: WorkPlan; canReview: boolean; onChanged: () => void; showStudent?: boolean }) {
  const [reviewing, setReviewing] = useState(false);
  return (
    <div className="list-item">
      <div className="row between">
        <strong>
          {showStudent && plan.studentName ? `${plan.studentName} · ` : ''}
          {monthYear(plan.year, plan.month)}
        </strong>
        <Badge tone={planTone[plan.status]}>{planStatusLabels[plan.status]}</Badge>
      </div>
      <p className="muted small">
        {showStudent && plan.departmentName ? `${plan.departmentName} · ` : ''}
        {plan.plannedDays} gün · {plan.plannedHours} saat · Son kayıt {fmtDateTime(plan.lastSavedAt)}
      </p>
      <p>
        {plan.days.map((d) => fmtDate(d.date).slice(0, 5)).join(', ')}
      </p>
      {plan.studentNote && <p className="muted small">Öğrenci notu: {plan.studentNote}</p>}
      {plan.reviewedByName && (
        <p className="muted small">
          {plan.reviewedByName} · {fmtDateTime(plan.reviewedAt)}
          {plan.reviewNote && ` · ${plan.reviewNote}`}
        </p>
      )}
      {canReview && (
        <button className="btn small" style={{ marginTop: 8 }} onClick={() => setReviewing(true)}>
          {plan.status === 'Pending' ? 'Onayla / Reddet' : 'Kararı değiştir'}
        </button>
      )}
      {reviewing && (
        <ReviewModal title="Çalışma planını incele" url={`/api/work-plans/${plan.id}/review`} onClose={() => setReviewing(false)} onDone={onChanged} />
      )}
    </div>
  );
}

export function LeaveTable({ leaves, canReview, onChanged, showStudent = true }: { leaves: Leave[]; canReview: boolean; onChanged: () => void; showStudent?: boolean }) {
  const [reviewing, setReviewing] = useState<Leave | null>(null);
  const [error, setError] = useState('');
  if (leaves.length === 0) return <Empty>Kayıt bulunamadı.</Empty>;

  async function download(l: Leave) {
    setError('');
    try {
      await api.download(`/api/leaves/${l.id}/document`, l.documentFileName ?? 'belge');
    } catch (err) {
      setError(msg(err, 'Belge indirilemedi.'));
    }
  }

  return (
    <>
      {error && <Alert>{error}</Alert>}
      <table className="table">
        <thead>
          <tr>
            {showStudent && <th>Öğrenci</th>}
            <th>Tarih</th><th>Tür</th><th>Süre</th><th>Gerekçe</th><th>Durum</th><th></th>
          </tr>
        </thead>
        <tbody>
          {leaves.map((l) => (
            <tr key={l.id}>
              {showStudent && <td>{l.studentName}<div className="muted small">{l.departmentName}</div></td>}
              <td>{fmtDate(l.startDate)}{l.startDate !== l.endDate && ` – ${fmtDate(l.endDate)}`}</td>
              <td>{leaveTypeLabels[l.type]}</td>
              <td>{l.hours ? `${l.hours} saat` : `${l.daysCounted} gün`}</td>
              <td>
                {l.reason}
                {l.reviewNote && <div className="muted small">Not: {l.reviewNote}</div>}
              </td>
              <td><Badge tone={leaveTone[l.status]}>{leaveStatusLabels[l.status]}</Badge></td>
              <td>
                <div className="row">
                  {l.hasDocument && <button className="btn small" onClick={() => download(l)}>Belge</button>}
                  {canReview && l.status === 'Pending' && <button className="btn small primary" onClick={() => setReviewing(l)}>İncele</button>}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {reviewing && (
        <ReviewModal title="İzin talebini incele" url={`/api/leaves/${reviewing.id}/review`} onClose={() => setReviewing(null)} onDone={onChanged} />
      )}
    </>
  );
}

export function AbsenceTable({ absences, canRevoke, onChanged, showStudent = true }: { absences: Absence[]; canRevoke: boolean; onChanged: () => void; showStudent?: boolean }) {
  const [revoking, setRevoking] = useState<Absence | null>(null);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  if (absences.length === 0) return <Empty>Kayıt bulunamadı.</Empty>;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!revoking) return;
    setError('');
    try {
      await api.post(`/api/absences/${revoking.id}/revoke`, { reason: reason.trim() });
      setRevoking(null);
      setReason('');
      onChanged();
    } catch (err) {
      setError(msg(err, 'Geri alınamadı.'));
    }
  }

  return (
    <>
      <table className="table">
        <thead>
          <tr>{showStudent && <th>Öğrenci</th>}<th>Tarih</th><th>Tür</th><th>Not</th><th>Kaydeden</th><th></th></tr>
        </thead>
        <tbody>
          {absences.map((a) => (
            <tr key={a.id}>
              {showStudent && <td>{a.studentName}<div className="muted small">{a.departmentName}</div></td>}
              <td>{fmtDate(a.date)}</td>
              <td>{absenceTypeLabels[a.type]}</td>
              <td>{a.note}</td>
              <td>{a.recordedByName}</td>
              <td>
                {a.isRevoked ? (
                  <Badge tone="gray">Geri alındı</Badge>
                ) : (
                  canRevoke && <button className="btn small danger" onClick={() => setRevoking(a)}>Geri al</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {revoking && (
        <Modal title="Devamsızlık kaydını geri al" onClose={() => setRevoking(null)}>
          <form onSubmit={submit}>
            {error && <Alert>{error}</Alert>}
            <p className="muted small">Kayıt silinmez; geri alındı olarak saklanır.</p>
            <Field label="Gerekçe">
              <textarea rows={3} value={reason} onChange={(e) => setReason(e.target.value)} required minLength={3} maxLength={500} />
            </Field>
            <button className="btn primary wide">Geri al</button>
          </form>
        </Modal>
      )}
    </>
  );
}

export function Filters({ children }: { children: ReactNode }) {
  return <div className="filters">{children}</div>;
}
