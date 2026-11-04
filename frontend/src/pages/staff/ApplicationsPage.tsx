import { useState } from 'react';
import { api } from '../../api/client';
import type { User } from '../../api/types';
import { Alert, Badge, Card, Empty } from '../../components/ui';
import { roleLabels } from '../../layout/menu';
import { useLoad } from '../../useLoad';

function message(err: unknown, fallback: string) {
  return err instanceof Error && err.message ? err.message : fallback;
}

/** Danışman ve bölüm başkanlarının kendi yaptığı kayıt başvuruları: fakülte sekreterliği onaylar veya reddeder. */
export default function ApplicationsPage() {
  const list = useLoad(() => api.get<User[]>('/api/users?isPendingApproval=true'), []);
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [busyId, setBusyId] = useState<number | null>(null);

  async function decide(u: User, approve: boolean) {
    setError('');
    setInfo('');
    setBusyId(u.id);
    try {
      await api.put(`/api/users/${u.id}/active`, { isActive: approve });
      setInfo(approve ? `${u.fullName} için hesap açıldı; artık giriş yapabilir.` : `${u.fullName} başvurusu reddedildi.`);
      list.reload();
    } catch (err) {
      setError(message(err, 'İşlem tamamlanamadı.'));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <Card
      title="Hesap başvuruları"
      subtitle="Danışman ve bölüm başkanları kayıt formundan başvurur; hesap siz onaylayana kadar pasif kalır ve giriş yapılamaz."
    >
      {error && <Alert>{error}</Alert>}
      {info && <Alert kind="info">{info}</Alert>}
      {list.error && <Alert>{list.error}</Alert>}
      {list.data && list.data.length === 0 && <Empty>Onay bekleyen başvuru yok.</Empty>}
      {list.data && list.data.length > 0 && (
        <div className="scroll-x">
          <table className="table">
            <thead>
              <tr>
                <th>Ad soyad</th>
                <th>E-posta</th>
                <th>Telefon</th>
                <th>Rol</th>
                <th>Bölüm</th>
                <th>Durum</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {list.data.map((u) => (
                <tr key={u.id}>
                  <td>{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>{u.phoneNumber ?? '—'}</td>
                  <td>{roleLabels[u.role]}</td>
                  <td>{u.departmentName ?? '—'}</td>
                  <td><Badge tone="amber">Onay bekliyor</Badge></td>
                  <td className="right-cell">
                    <div className="row" style={{ justifyContent: 'flex-end', flexWrap: 'nowrap' }}>
                      <button className="btn primary small" disabled={busyId === u.id} onClick={() => decide(u, true)}>Onayla</button>
                      <button className="btn small" disabled={busyId === u.id} onClick={() => decide(u, false)}>Reddet</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
