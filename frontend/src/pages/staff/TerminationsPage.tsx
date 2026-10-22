import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { StudentSummary, Termination, TerminationBasis } from '../../api/types';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Card, Empty, Field } from '../../components/ui';
import { fmtDate, terminationBasisLabels, todayIso } from '../../format';
import { useLoad } from '../../useLoad';

export default function TerminationsPage() {
  const [year, setYear] = useState(new Date().getFullYear());
  const list = useLoad(() => api.get<Termination[]>(`/api/terminations?year=${year}`), [year]);

  return (
    <div className="two-col">
      <Card title="İlişik kesme kararları" subtitle="Karar kaydedilince öğrencinin program katılımı sona erer; kayıtlar silinmez.">
        <div className="row" style={{ marginBottom: 12 }}>
          <label className="row">Yıl <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} style={{ width: 90 }} /></label>
        </div>
        {list.error && <Alert>{list.error}</Alert>}
        {list.data && list.data.length === 0 && <Empty>Bu yıl için ilişik kesme kararı yok.</Empty>}
        {list.data && list.data.length > 0 && (
          <div className="scroll-x">
            <table className="table">
              <thead><tr><th>Tarih</th><th>Öğrenci</th><th>Bölüm</th><th>Dayanak</th><th>İzin (gün)</th><th>Mazeretsiz</th><th>Karar veren</th></tr></thead>
              <tbody>
                {list.data.map((t) => (
                  <tr key={t.id} title={t.note}>
                    <td>{fmtDate(t.decisionDate)}</td>
                    <td>{t.studentName}</td>
                    <td>{t.departmentName}</td>
                    <td>{terminationBasisLabels[t.basis]}</td>
                    <td>{t.leaveDaysUsed}</td>
                    <td>{t.unexcusedAbsenceDays}</td>
                    <td>{t.decidedByName}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      <RecordTermination onDone={list.reload} />
    </div>
  );
}

function RecordTermination({ onDone }: { onDone: () => void }) {
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [basis, setBasis] = useState<TerminationBasis>('LeaveLimitExceeded');
  const [date, setDate] = useState(todayIso());
  const [note, setNote] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!student) return;
    if (!window.confirm(`${student.fullName} için ilişik kesme kararı kaydedilecek ve öğrencinin program katılımı sona erecek. Onaylıyor musunuz?`)) return;
    setError('');
    setDone(false);
    try {
      await api.post('/api/terminations', { studentId: student.id, basis, decisionDate: date, note: note.trim() });
      setDone(true);
      setStudent(null);
      setNote('');
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Karar kaydedilemedi.');
    }
  }

  return (
    <Card title="İlişik kesme kararı gir" subtitle="Yalnızca fakülte sekreterliği karar girer. İzin ve devamsızlık sınırlarını Raporlar bölümündeki inceleme listesinden görebilirsiniz.">
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        {done && <Alert kind="info">Karar kaydedildi.</Alert>}
        <Field label="Öğrenci"><StudentPicker value={student} onChange={setStudent} /></Field>
        <Field label="Dayanak">
          <select value={basis} onChange={(e) => setBasis(e.target.value as TerminationBasis)}>
            {(Object.keys(terminationBasisLabels) as TerminationBasis[]).map((b) => <option key={b} value={b}>{terminationBasisLabels[b]}</option>)}
          </select>
        </Field>
        <Field label="Karar tarihi"><input type="date" value={date} max={todayIso()} onChange={(e) => setDate(e.target.value)} required /></Field>
        <Field label="Gerekçe" hint="En az 10 karakter"><textarea rows={4} value={note} onChange={(e) => setNote(e.target.value)} minLength={10} maxLength={800} required /></Field>
        <button className="btn primary wide" disabled={!student}>Kararı kaydet</button>
      </form>
    </Card>
  );
}
