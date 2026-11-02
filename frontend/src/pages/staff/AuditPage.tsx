import { useState } from 'react';
import { api } from '../../api/client';
import type { AuditLog, Paged } from '../../api/types';
import { Alert, Card, Empty, Field, Pager } from '../../components/ui';
import { actionLabel, auditActions, auditEntities, entityLabel } from '../../auditText';
import { fmtDateTime } from '../../format';
import { useLoad } from '../../useLoad';
import { Filters } from './parts';

export default function AuditPage() {
  const [action, setAction] = useState('');
  const [entity, setEntity] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(1);

  const query = new URLSearchParams({ page: String(page), pageSize: '25' });
  if (action.trim()) query.set('action', action.trim());
  if (entity.trim()) query.set('entityName', entity.trim());
  if (from) query.set('fromUtc', new Date(`${from}T00:00:00`).toISOString());
  if (to) query.set('toUtc', new Date(`${to}T23:59:59`).toISOString());

  const logs = useLoad(() => api.get<Paged<AuditLog>>(`/api/audit-logs?${query}`), [query.toString()]);
  const reset = (set: (v: string) => void) => (v: string) => { set(v); setPage(1); };

  return (
    <Card title="Denetim kayıtları" subtitle="Sistemde kimin, ne zaman, hangi işlemi yaptığı. Kayıtlar silinemez.">
      <Filters>
        <Field label="Ne yapıldı">
          <select value={action} onChange={(e) => reset(setAction)(e.target.value)}>
            <option value="">Tümü</option>
            {Object.entries(auditActions).map(([code, text]) => <option key={code} value={code}>{text}</option>)}
          </select>
        </Field>
        <Field label="Neye yapıldı">
          <select value={entity} onChange={(e) => reset(setEntity)(e.target.value)}>
            <option value="">Tümü</option>
            {Object.entries(auditEntities).map(([code, text]) => <option key={code} value={code}>{text}</option>)}
          </select>
        </Field>
        <Field label="Başlangıç"><input type="date" value={from} onChange={(e) => reset(setFrom)(e.target.value)} /></Field>
        <Field label="Bitiş"><input type="date" value={to} onChange={(e) => reset(setTo)(e.target.value)} /></Field>
      </Filters>
      {logs.error && <Alert>{logs.error}</Alert>}
      {logs.data && logs.data.items.length === 0 && <Empty>Kayıt bulunamadı.</Empty>}
      {logs.data && logs.data.items.length > 0 && (
        <div className="scroll-x">
          <table className="table">
            <thead><tr><th>Zaman</th><th>Kim yaptı</th><th>Ne yapıldı</th><th>Hangi kayıt</th><th>Ayrıntı</th><th>Bilgisayar (IP)</th></tr></thead>
            <tbody>
              {logs.data.items.map((l) => (
                <tr key={l.id}>
                  <td>{fmtDateTime(l.occurredAt)}</td>
                  <td>{l.userEmail ?? '—'}</td>
                  <td>{actionLabel(l.action)}</td>
                  <td>{entityLabel(l.entityName, l.entityId)}</td>
                  <td>{l.details ?? '—'}</td>
                  <td>{l.ipAddress ?? ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {logs.data && <p className="muted small" style={{ marginTop: 8 }}>Toplam {logs.data.totalCount} kayıt</p>}
      {logs.data && <Pager page={page} totalPages={logs.data.totalPages} onChange={setPage} />}
    </Card>
  );
}
