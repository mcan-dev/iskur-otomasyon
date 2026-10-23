import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { Absence, StudentSummary } from '../../api/types';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Card, Field } from '../../components/ui';
import { todayIso } from '../../format';
import { useLoad } from '../../useLoad';
import { AbsenceTable } from './parts';

export default function AbsencesPage() {
  const [year, setYear] = useState(new Date().getFullYear());
  const absences = useLoad(() => api.get<Absence[]>(`/api/absences?year=${year}`), [year]);

  return (
    <div className="two-col">
      <Card title="Devamsızlık kayıtları">
        <div className="row" style={{ marginBottom: 12 }}>
          <label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label>
        </div>
        {absences.error && <Alert>{absences.error}</Alert>}
        {absences.data && <AbsenceTable absences={absences.data} canRevoke onChanged={absences.reload} />}
      </Card>
      <RecordAbsence onDone={absences.reload} />
    </div>
  );
}

function RecordAbsence({ onDone }: { onDone: () => void }) {
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [date, setDate] = useState(todayIso());
  const [note, setNote] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!student) return;
    setError(null);
    setDone(false);
    try {
      await api.post('/api/absences', { studentId: student.id, date, note: note.trim() || null });
      setDone(true);
      setNote('');
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Kayıt yapılamadı.'));
    }
  }

  return (
    <Card title="Devamsızlık gir" subtitle="Devamsızlık yalnızca fakülte yönetimi tarafından kaydedilir ve öğrencinin izin hakkından düşer.">
      <form onSubmit={submit}>
        {error && <Alert>{error.message}</Alert>}
        {done && <Alert kind="info">Devamsızlık kaydedildi.</Alert>}
        <Field label="Öğrenci"><StudentPicker value={student} onChange={setStudent} /></Field>
        <Field label="Tarih"><input type="date" value={date} max={todayIso()} onChange={(e) => setDate(e.target.value)} required /></Field>
        <Field label="Not (isteğe bağlı)"><textarea rows={3} value={note} onChange={(e) => setNote(e.target.value)} maxLength={500} /></Field>
        <button className="btn primary wide" disabled={!student}>Kaydet</button>
      </form>
    </Card>
  );
}
