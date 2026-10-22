import { useState } from 'react';
import { api } from '../../api/client';
import type { Attendance } from '../../api/types';
import { Alert, Badge, Card, Empty, MonthPicker } from '../../components/ui';
import { fmtDate, fmtDateTime } from '../../format';
import { useLoad } from '../../useLoad';

// Danışman: kendi öğrencilerinin aylık devam kayıtları (kapsamı sunucu daraltır).
export default function AttendancePage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const records = useLoad(() => api.get<Attendance[]>(`/api/attendance?year=${year}&month=${month}`), [year, month]);

  return (
    <Card title="Devam durumu" subtitle="Öğrencilerinizin karekodla kaydedilen devamı. Planla karşılaştırma için öğrenci sayfasındaki Devam sekmesine bakın.">
      <div style={{ marginBottom: 12 }}><MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} /></div>
      {records.error && <Alert>{records.error}</Alert>}
      {records.data?.length === 0 && <Empty>Bu ay devam kaydı yok.</Empty>}
      {records.data && records.data.length > 0 && (
        <table className="table">
          <thead><tr><th>Öğrenci</th><th>Tarih</th><th>Kayıt zamanı</th><th></th></tr></thead>
          <tbody>
            {records.data.map((r) => (
              <tr key={r.id}>
                <td>{r.studentName}</td>
                <td>{fmtDate(r.date)}</td>
                <td>{fmtDateTime(r.checkedInAt)}</td>
                <td>{r.isManual ? <Badge tone="blue">Elle girildi</Badge> : <Badge tone="green">Karekod</Badge>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  );
}
