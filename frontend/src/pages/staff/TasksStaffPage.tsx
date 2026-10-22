import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { StudentSummary, StudentTask, TaskStatus } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import StudentPicker from '../../components/StudentPicker';
import { Alert, Badge, Card, Empty, Field, Modal } from '../../components/ui';
import { fmtDate, fmtDateTime, taskStatusLabels, taskTone, todayIso } from '../../format';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

export default function TasksStaffPage() {
  const { user } = useAuth();
  const isAdvisor = user?.role === 'Advisor';
  const [status, setStatus] = useState('');
  const [overdue, setOverdue] = useState(false);
  const [evaluating, setEvaluating] = useState<StudentTask | null>(null);
  const [openId, setOpenId] = useState<number | null>(null);

  const query = new URLSearchParams();
  if (status) query.set('status', status);
  if (overdue) query.set('overdueOnly', 'true');
  const tasks = useLoad(() => api.get<StudentTask[]>(`/api/tasks?${query}`), [query.toString()]);

  return (
    <div className="two-col">
      <Card title="Görevler ve değerlendirmeler">
        <Filters>
          <Field label="Durum">
            <select value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="">Tümü</option>
              {(Object.keys(taskStatusLabels) as TaskStatus[]).map((s) => <option key={s} value={s}>{taskStatusLabels[s]}</option>)}
            </select>
          </Field>
          <Field label="Süresi geçenler">
            <label className="row"><input type="checkbox" checked={overdue} onChange={(e) => setOverdue(e.target.checked)} /> Yalnızca gecikenler</label>
          </Field>
        </Filters>
        {tasks.error && <Alert>{tasks.error}</Alert>}
        {tasks.data?.length === 0 && <Empty>Görev bulunamadı.</Empty>}
        <div className="stack">
          {tasks.data?.map((t) => (
            <div key={t.id} className="list-item">
              <div className="row between">
                <strong>{t.title}</strong>
                <div className="row">{t.isOverdue && <Badge tone="red">Süresi geçti</Badge>}<Badge tone={taskTone[t.status]}>{taskStatusLabels[t.status]}</Badge></div>
              </div>
              <p className="muted small">{t.studentName} · {t.departmentName} · Veren {t.assignedByName} · Son tarih {fmtDate(t.dueDate)}</p>
              {t.description && <p>{t.description}</p>}
              <div className="row" style={{ marginTop: 8 }}>
                {t.evaluationCount > 0 && (
                  <button className="btn small" onClick={() => setOpenId(openId === t.id ? null : t.id)}>
                    {openId === t.id ? 'Notları gizle' : `Notlar (${t.evaluationCount})`}
                  </button>
                )}
                {isAdvisor && <button className="btn small primary" onClick={() => setEvaluating(t)}>Değerlendir</button>}
              </div>
              {openId === t.id && (
                <div className="stack" style={{ marginTop: 10 }}>
                  {t.evaluations.map((e) => (
                    <div key={e.id} className="list-item">
                      <p className="muted small">{e.authorName} · {fmtDateTime(e.createdAt)}{e.newStatus && ` · Durum: ${taskStatusLabels[e.newStatus]}`}</p>
                      {e.note && <p>{e.note}</p>}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      </Card>
      <AssignTask onDone={tasks.reload} />
      {evaluating && <EvaluateModal task={evaluating} onClose={() => setEvaluating(null)} onDone={tasks.reload} />}
    </div>
  );
}

function AssignTask({ onDone }: { onDone: () => void }) {
  const [student, setStudent] = useState<StudentSummary | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [due, setDue] = useState(todayIso());
  const [error, setError] = useState('');
  const [ok, setOk] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!student) return;
    setError('');
    setOk(false);
    try {
      await api.post('/api/tasks', { studentId: student.id, title: title.trim(), description: description.trim() || null, dueDate: due });
      setTitle('');
      setDescription('');
      setStudent(null);
      setOk(true);
      onDone();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Görev verilemedi.');
    }
  }

  return (
    <Card title="Görev ver" subtitle="Verilen görevin başlığı ve tarihleri sonradan değiştirilemez.">
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        {ok && <Alert kind="info">Görev verildi.</Alert>}
        <Field label="Öğrenci"><StudentPicker value={student} onChange={setStudent} /></Field>
        <Field label="Başlık"><input value={title} onChange={(e) => setTitle(e.target.value)} required minLength={3} maxLength={150} /></Field>
        <Field label="Açıklama"><textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} /></Field>
        <Field label="Son tarih"><input type="date" value={due} min={todayIso()} onChange={(e) => setDue(e.target.value)} required /></Field>
        <button className="btn primary wide" disabled={!student}>Görevi ver</button>
      </form>
    </Card>
  );
}

function EvaluateModal({ task, onClose, onDone }: { task: StudentTask; onClose: () => void; onDone: () => void }) {
  const [note, setNote] = useState('');
  const [status, setStatus] = useState<string>('');
  const [error, setError] = useState('');

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.post(`/api/tasks/${task.id}/evaluations`, { note: note.trim() || null, newStatus: status || null });
      onDone();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kaydedilemedi.');
    }
  }

  return (
    <Modal title={`Değerlendir: ${task.title}`} onClose={onClose}>
      <form onSubmit={submit}>
        {error && <Alert>{error}</Alert>}
        <p className="muted small">Önceki notlar korunur; her değerlendirme yeni bir kayıt olarak eklenir.</p>
        <Field label="Durum">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Değiştirme ({taskStatusLabels[task.status]})</option>
            {(Object.keys(taskStatusLabels) as TaskStatus[]).map((s) => <option key={s} value={s}>{taskStatusLabels[s]}</option>)}
          </select>
        </Field>
        <Field label="Not"><textarea rows={4} value={note} onChange={(e) => setNote(e.target.value)} maxLength={2000} /></Field>
        <button className="btn primary wide" disabled={!note.trim() && !status}>Kaydet</button>
      </form>
    </Modal>
  );
}
