import { useEffect, useState, type FormEvent } from 'react';
import { api, ApiError, firstError } from '../../api/client';
import type { StudentProfileFull } from '../../api/types';
import { Alert, Badge, Card, Field, PhoneInput } from '../../components/ui';
import { fmtDate } from '../../format';
import { useLoad } from '../../useLoad';

export default function ProfilePage() {
  const profile = useLoad(() => api.get<StudentProfileFull>('/api/students/me'), []);
  const p = profile.data;

  return (
    <div className="two-col">
      <div className="stack">
        <Card title="Kişisel bilgilerim">
          {profile.error && <Alert>{profile.error}</Alert>}
          {p && (
            <dl className="kv">
              <dt>Ad soyad</dt><dd>{p.fullName}</dd>
              <dt>E-posta</dt><dd>{p.email}</dd>
              <dt>T.C. kimlik no</dt><dd>{p.nationalId}</dd>
              <dt>Öğrenci numarası</dt><dd>{p.studentNumber}</dd>
              <dt>Bölüm</dt><dd>{p.departmentName}</dd>
              <dt>Kayıt yılı</dt><dd>{p.registrationYear}</dd>
              <dt>Durum</dt><dd><Badge tone={p.isActive ? 'green' : 'gray'}>{p.isActive ? 'Aktif' : 'Pasif'}</Badge></dd>
              <dt>Çalıştığı birim</dt><dd>{p.currentUnit?.workUnitName ?? 'Henüz atanmadı'}</dd>
              <dt>Danışman</dt><dd>{p.currentAdvisor?.advisorName ?? 'Henüz atanmadı'}</dd>
            </dl>
          )}
        </Card>
        {p && <EditContact profile={p} onSaved={profile.reload} />}
        {p && (
          <Card title="Geçmiş" subtitle="Programa katılım, birim ve danışman geçmişiniz.">
            <h3 className="sub">Programa katılım</h3>
            <ul className="plain">
              {p.participations.map((x) => (
                <li key={x.id}>
                  {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}
                  {x.endReason && <span className="muted"> ({x.endReason})</span>}
                </li>
              ))}
            </ul>
            <h3 className="sub">Çalışma birimi</h3>
            <ul className="plain">
              {p.unitHistory.length === 0 && <li className="muted">Kayıt yok</li>}
              {p.unitHistory.map((x) => (
                <li key={x.id}>{x.workUnitName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>
              ))}
            </ul>
            <h3 className="sub">Danışman</h3>
            <ul className="plain">
              {p.advisorHistory.length === 0 && <li className="muted">Kayıt yok</li>}
              {p.advisorHistory.map((x) => (
                <li key={x.id}>{x.advisorName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>
              ))}
            </ul>
          </Card>
        )}
      </div>
      <ChangePassword />
    </div>
  );
}

function EditContact({ profile, onSaved }: { profile: StudentProfileFull; onSaved: () => void }) {
  const [phone, setPhone] = useState(profile.phoneNumber ?? '');
  const [iban, setIban] = useState(profile.iban);
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  useEffect(() => {
    setPhone(profile.phoneNumber ?? '');
    setIban(profile.iban);
  }, [profile]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDone(false);
    try {
      await api.put('/api/students/me', { phoneNumber: phone, iban });
      setDone(true);
      onSaved();
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Kaydedilemedi.'));
    }
  }

  const general = error && Object.keys(error.fieldErrors).length === 0 ? error.message : undefined;
  return (
    <Card title="İletişim ve IBAN" subtitle="Yalnızca telefon ve IBAN bilgisini kendiniz değiştirebilirsiniz.">
      <form onSubmit={submit}>
        {general && <Alert>{general}</Alert>}
        {done && <Alert kind="info">Bilgileriniz güncellendi.</Alert>}
        <div className="grid-2">
          <Field label="Cep telefonu" error={firstError(error, 'phoneNumber')}>
            <PhoneInput value={phone} onChange={setPhone} required />
          </Field>
          <Field label="IBAN" error={firstError(error, 'iban')}>
            <input value={iban} onChange={(e) => setIban(e.target.value.toUpperCase())} required />
          </Field>
        </div>
        <button className="btn primary">Kaydet</button>
      </form>
    </Card>
  );
}

function ChangePassword() {
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDone(false);
    try {
      await api.post('/api/auth/change-password', { currentPassword: current, newPassword: next });
      setDone(true);
      setCurrent('');
      setNext('');
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Parola değiştirilemedi.'));
    }
  }

  const general = error && Object.keys(error.fieldErrors).length === 0 ? error.message : undefined;
  return (
    <Card title="Parolamı değiştir">
      <form onSubmit={submit}>
        {general && <Alert>{general}</Alert>}
        {done && <Alert kind="info">Parolanız değiştirildi.</Alert>}
        <Field label="Mevcut parola" error={firstError(error, 'currentPassword')}>
          <input type="password" value={current} onChange={(e) => setCurrent(e.target.value)} autoComplete="current-password" required />
        </Field>
        <Field label="Yeni parola" error={firstError(error, 'newPassword')}>
          <input type="password" value={next} onChange={(e) => setNext(e.target.value)} autoComplete="new-password" required />
        </Field>
        <button className="btn primary wide">Parolayı Değiştir</button>
      </form>
    </Card>
  );
}
