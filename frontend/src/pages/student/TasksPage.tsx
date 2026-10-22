import { useState } from 'react';
import { api } from '../../api/client';
import type { StudentTask, TaskStatus } from '../../api/types';
import { Alert, Badge, Card, Empty } from '../../components/ui';
import { fmtDate, fmtDateTime, taskStatusLabels, taskTone } from '../../format';
import { useLoad } from '../../useLoad';

const filters: { value: TaskStatus | ''; label: string }[] = [
  { value: '', label: 'Tümü' },
  { value: 'Open', label: 'Açık' },
  { value: 'Completed', label: 'Tamamlandı' },
  { value: 'NotCompleted', label: 'Tamamlanmadı' },
];

export default function TasksPage() {
  const [status, setStatus] = useState<TaskStatus | ''>('');
  const tasks = useLoad(() => api.get<StudentTask[]>(`/api/tasks/me${status ? `?status=${status}` : ''}`), [status]);
  const [openId, setOpenId] = useState<number | null>(null);

  return (
    <Card title="Görevlerim" subtitle="Danışmanınızın veya bölümünüzün verdiği görevler ve değerlendirme notları.">
      <div className="tabs" style={{ marginBottom: 14 }}>
        {filters.map((f) => (
          <button key={f.value} className={`tab ${status === f.value ? 'active' : ''}`} onClick={() => setStatus(f.value)}>
            {f.label}
          </button>
        ))}
      </div>
      {tasks.error && <Alert>{tasks.error}</Alert>}
      {tasks.data?.length === 0 && <Empty>Görev bulunmuyor.</Empty>}
      <div className="stack">
        {tasks.data?.map((t) => (
          <div key={t.id} className="list-item">
            <div className="row between">
              <strong>{t.title}</strong>
              <div className="row">
                {t.isOverdue && <Badge tone="red">Süresi geçti</Badge>}
                <Badge tone={taskTone[t.status]}>{taskStatusLabels[t.status]}</Badge>
              </div>
            </div>
            <p className="muted small">
              Veren: {t.assignedByName} · Verilme {fmtDate(t.assignedDate)} · Son tarih {fmtDate(t.dueDate)}
            </p>
            {t.description && <p>{t.description}</p>}
            {t.evaluationCount > 0 && (
              <>
                <button className="btn small" style={{ marginTop: 8 }} onClick={() => setOpenId(openId === t.id ? null : t.id)}>
                  {openId === t.id ? 'Notları gizle' : `Değerlendirme notları (${t.evaluationCount})`}
                </button>
                {openId === t.id && (
                  <div className="stack" style={{ marginTop: 10 }}>
                    {t.evaluations.map((e) => (
                      <div key={e.id} className="list-item">
                        <p className="muted small">
                          {e.authorName} · {fmtDateTime(e.createdAt)}
                          {e.newStatus && ` · Durum: ${taskStatusLabels[e.newStatus]}`}
                        </p>
                        {e.note && <p>{e.note}</p>}
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}
