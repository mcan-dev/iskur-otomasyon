import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { Leave, LeaveBalance, LeaveResult, LeaveType } from '../../api/types';
import { Alert, Badge, Card, Empty, Field, Stat } from '../../components/ui';
import { fmtDate, leaveStatusLabels, leaveTone, leaveTypeLabels, todayIso } from '../../format';
import { useLoad } from '../../useLoad';

export default function LeavesPage() {
  const year = new Date().getFullYear();
  const balance = useLoad(() => api.get<LeaveBalance>(`/api/leaves/me/balance?year=${year}`), [year]);
  const leaves = useLoad(() => api.get<Leave[]>(`/api/leaves/me?year=${year}`), [year]);
  const [warnings, setWarnings] = useState<string[]>([]);

  const b = balance.data;

  return (
    <div className="stack">
      <div className="stats">
        <Stat label={`${year} izin hakkı`} value={b ? `${b.limit} gün` : '–'} tone="blue" />
        <Stat label="Kullanılan" value={b ? `${b.used} gün` : '–'} hint={b ? `${b.leaveDays} gün izin, ${b.absenceDays} gün devamsızlık${b.pending > 0 ? `, ${b.pending} gün onay bekliyor` : ''}` : undefined} tone="amber" />
        <Stat label="Kalan" value={b ? `${b.remaining} gün` : '–'} tone="green" />
      </div>
      {[...(b?.warnings.map((w) => w.message) ?? []), ...warnings].map((w, i) => (
        <Alert key={i} kind="warn">{w}</Alert>
      ))}

      <div className="two-col">
        <Card title="İzin taleplerim">
          {leaves.error && <Alert>{leaves.error}</Alert>}
          {leaves.data?.length === 0 && <Empty>Bu yıl için izin talebiniz yok.</Empty>}
          <div className="stack">
            {leaves.data?.map((l) => (
              <LeaveItem key={l.id} leave={l} onChanged={() => { leaves.reload(); balance.reload(); }} />
            ))}
          </div>
        </Card>
        <NewLeave
          onCreated={(result) => {
            setWarnings(result.balance.warnings.map((w) => w.message));
            leaves.reload();
            balance.reload();
          }}
        />
      </div>
    </div>
  );
}

function LeaveItem({ leave, onChanged }: { leave: Leave; onChanged: () => void }) {
  const [error, setError] = useState('');

  async function run(action: () => Promise<unknown>) {
    setError('');
    try {
      await action();
      onChanged();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İşlem yapılamadı.');
    }
  }

  function upload(file: File | undefined) {
    if (!file) return;
    const form = new FormData();
    form.append('file', file);
    run(() => api.upload(`/api/leaves/me/${leave.id}/document`, form));
  }

  const same = leave.startDate === leave.endDate;
  return (
    <div className="list-item">
      <div className="row between">
        <strong>
          {fmtDate(leave.startDate)}
          {!same && ` – ${fmtDate(leave.endDate)}`}
        </strong>
        <Badge tone={leaveTone[leave.status]}>{leaveStatusLabels[leave.status]}</Badge>
      </div>
      <p className="muted small">
        {leaveTypeLabels[leave.type]} · {leave.daysCounted} gün · Başvuru {fmtDate(leave.appliedOn)}
      </p>
      <p>{leave.reason}</p>
      {leave.reviewNote && <p className="muted small">İnceleme notu: {leave.reviewNote}</p>}
      {error && <Alert>{error}</Alert>}
      <div className="row" style={{ marginTop: 8 }}>
        {leave.hasDocument && (
          <button className="btn small" onClick={() => run(() => api.download(`/api/leaves/${leave.id}/document`, leave.documentFileName ?? 'belge'))}>
            Belgeyi indir
          </button>
        )}
        {(leave.status === 'Pending' || leave.status === 'Approved') && (
          <label className="btn small">
            {leave.hasDocument ? 'Belgeyi değiştir' : 'Belge ekle'}
            <input type="file" accept=".pdf,.jpg,.jpeg,.png" hidden onChange={(e) => upload(e.target.files?.[0])} />
          </label>
        )}
        {leave.status === 'Pending' && (
          <button className="btn small danger" onClick={() => run(() => api.post(`/api/leaves/me/${leave.id}/cancel`))}>
            İptal et
          </button>
        )}
      </div>
    </div>
  );
}

function NewLeave({ onCreated }: { onCreated: (r: LeaveResult) => void }) {
  const today = todayIso();
  const [type, setType] = useState<LeaveType>('Excuse');
  const [start, setStart] = useState(today);
  const [end, setEnd] = useState(today);
  const [reason, setReason] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [localError, setLocalError] = useState('');
  const [done, setDone] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDone('');
    setLocalError('');
    if (!start || !end) return setLocalError('Başlangıç ve bitiş tarihini seçin.');
    if (end < start) return setLocalError('Bitiş tarihi başlangıçtan önce olamaz.');
    if (reason.trim().length < 3) return setLocalError('Gerekçe en az 3 karakter olmalıdır.');
    setBusy(true);
    try {
      const result = await api.post<LeaveResult>('/api/leaves/me', {
        type,
        startDate: start,
        endDate: end,
        reason: reason.trim(),
        description: description.trim() || null,
      });
      setReason('');
      setDescription('');
      setDone('İzin talebiniz gönderildi; onay bekliyor.');
      onCreated(result);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Talep oluşturulamadı.'));
    } finally {
      setBusy(false);
    }
  }

  const fieldMsg = (name: string) => {
    const key = error && Object.keys(error.fieldErrors).find((k) => k.toLowerCase() === name.toLowerCase());
    return key && error ? error.fieldErrors[key][0] : undefined;
  };
  // Alan altında gösterilemeyen her hata (ör. izin türü) üstte uyarı olarak da listelenir.
  const shown = ['startdate', 'enddate', 'reason'];
  const otherErrors = error
    ? Object.entries(error.fieldErrors).filter(([k]) => !shown.includes(k.toLowerCase())).flatMap(([, v]) => v)
    : [];

  return (
    <Card title="Yeni izin talebi" subtitle="İzinler gün bazında alınır.">
      <form onSubmit={submit} noValidate>
        {done && <Alert kind="info">{done}</Alert>}
        {localError && <Alert>{localError}</Alert>}
        {error && Object.keys(error.fieldErrors).length === 0 && <Alert>{error.message}</Alert>}
        {otherErrors.length > 0 && <Alert>{otherErrors.join(' ')}</Alert>}
        <Field label="İzin türü">
          <select value={type} onChange={(e) => setType(e.target.value as LeaveType)}>
            {(Object.keys(leaveTypeLabels) as LeaveType[]).map((k) => (
              <option key={k} value={k}>{leaveTypeLabels[k]}</option>
            ))}
          </select>
        </Field>
        <Field label="Başlangıç" error={fieldMsg('startDate')}>
          <input type="date" value={start} onChange={(e) => { setStart(e.target.value); if (e.target.value > end) setEnd(e.target.value); }} />
        </Field>
        <Field label="Bitiş" error={fieldMsg('endDate')}>
          <input type="date" value={end} min={start} onChange={(e) => setEnd(e.target.value)} />
        </Field>
        <Field label="Gerekçe" error={fieldMsg('reason')}>
          <input value={reason} onChange={(e) => setReason(e.target.value)} maxLength={300} />
        </Field>
        <Field label="Açıklama (isteğe bağlı)">
          <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} />
        </Field>
        <button className="btn primary wide" disabled={busy}>{busy ? 'Gönderiliyor…' : 'Talep Gönder'}</button>
        <p className="muted small" style={{ marginTop: 8 }}>Belgeyi (PDF, JPG, PNG; en fazla 5 MB) talep oluştuktan sonra listeden ekleyebilirsiniz.</p>
      </form>
    </Card>
  );
}
