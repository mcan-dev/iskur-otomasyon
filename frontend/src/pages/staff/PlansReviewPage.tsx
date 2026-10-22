import { useState } from 'react';
import { api } from '../../api/client';
import type { Department, WorkPlan } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Card, Empty, Field, MonthPicker } from '../../components/ui';
import { useLoad } from '../../useLoad';
import { Filters, PlanItem } from './parts';

export default function PlansReviewPage() {
  const { user } = useAuth();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [status, setStatus] = useState('Pending');
  const [departmentId, setDepartmentId] = useState('');

  const departments = useLoad(() => api.get<Department[]>('/api/departments'), []);
  const query = new URLSearchParams({ year: String(year), month: String(month) });
  if (status) query.set('status', status);
  if (departmentId) query.set('departmentId', departmentId);
  const plans = useLoad(() => api.get<WorkPlan[]>(`/api/work-plans?${query}`), [query.toString()]);

  return (
    <Card title="Çalışma planı onayları" subtitle="Öğrencilerin gönderdiği aylık planları onaylayın veya gerekçeyle reddedin.">
      <div className="row between" style={{ marginBottom: 12 }}>
        <MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />
      </div>
      <Filters>
        <Field label="Durum">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Tümü</option>
            <option value="Pending">Onay bekleyen</option>
            <option value="Approved">Onaylanan</option>
            <option value="Rejected">Reddedilen</option>
          </select>
        </Field>
        <Field label="Bölüm">
          <select value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
            <option value="">Tümü</option>
            {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </Field>
      </Filters>
      {plans.error && <Alert>{plans.error}</Alert>}
      {plans.data?.length === 0 && <Empty>Kriterlere uyan plan yok.</Empty>}
      <div className="stack">
        {plans.data?.map((p) => <PlanItem key={p.id} plan={p} canReview={user?.role === 'FacultyAdmin'} onChanged={plans.reload} />)}
      </div>
    </Card>
  );
}
